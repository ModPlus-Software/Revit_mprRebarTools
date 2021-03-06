namespace mprRebarTools.Commands;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using ModPlus_Revit.Utils;
using ModPlusAPI;
using SelectionFilters;

/// <summary>
/// Копировать арматуру между основами
/// </summary>
[Transaction(TransactionMode.Manual)]
[Regeneration(RegenerationOption.Manual)]
//// ReSharper disable once UnusedMember.Global
public class CopyRebarBetweenHosts : IExternalCommand
{
    /// <inheritdoc />
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        return CommandExecutor.Execute(() =>
        {
#if !DEBUG
            ModPlusAPI.Statistic.SendCommandStarting($"mpr{nameof(CopyRebarBetweenHosts)}", ModPlusConnector.Instance.AvailProductExternalVersion);
#endif
            var doc = commandData.Application.ActiveUIDocument.Document;

            // "Выберите исходный элемент-основу"
            var sourceHost = doc.GetElement(commandData.Application.ActiveUIDocument.Selection.PickObject(
                ObjectType.Element, new HostSelectionFilter(), Language.GetItem("m2")));

            var sourceColumnRotation = double.NaN;
            if (sourceHost is FamilyInstance { IsSlantedColumn: false, Location: LocationPoint sourceFamilyLocationPoint })
            {
                sourceColumnRotation = sourceFamilyLocationPoint.Rotation;
            }

            var sourceReinforcement = Utils.GetReinforcement(sourceHost, true, false, false, false).ToList();

            if (!sourceReinforcement.Any())
                return;

            var sourceGroupsAndAssemblies = new Dictionary<int, Element>();
            for (var i = sourceReinforcement.Count - 1; i >= 0; i--)
            {
                if (doc.GetElement(sourceReinforcement[i].GroupId ?? ElementId.InvalidElementId) is Group group)
                {
                    if (!sourceGroupsAndAssemblies.ContainsKey(group.Id.IntegerValue))
                        sourceGroupsAndAssemblies.Add(group.Id.IntegerValue, group);
                    sourceReinforcement.RemoveAt(i);
                }
                else if (doc.GetElement(sourceReinforcement[i].AssemblyInstanceId ?? ElementId.InvalidElementId) is AssemblyInstance assemblyInstance)
                {
                    if (!sourceGroupsAndAssemblies.ContainsKey(assemblyInstance.Id.IntegerValue))
                        sourceGroupsAndAssemblies.Add(assemblyInstance.Id.IntegerValue, assemblyInstance);
                    sourceReinforcement.RemoveAt(i);
                }
            }

            var sourceSolids = GetSolids(sourceHost).ToList();
            var sourceCentroid = GetCentroid(sourceSolids);

            var skipIfNoMatchSolids = false;
            var copyIfNoMatchSolids = false;
            using (var trGroup = new TransactionGroup(doc, Language.GetItem("n2")))
            {
                trGroup.Start();

                while (true)
                {
                    try
                    {
                        // "Выберите целевые элементы-основы"
                        var targetHost = doc.GetElement(commandData.Application.ActiveUIDocument.Selection.PickObject(
                            ObjectType.Element, new HostSelectionFilter(), Language.GetItem("m3")));

                        var targetSolids = GetSolids(targetHost).ToList();

                        if (!IsMatchVolume(sourceSolids, targetSolids))
                        {
                            if (skipIfNoMatchSolids)
                                continue;
                            if (!copyIfNoMatchSolids)
                            {
                                // "Неподходящий элемент"
                                var dialog = new TaskDialog(Language.GetItem("h1"))
                                {
                                    // "Выбранный элемент имеет твердое тело, которое не совпадает с исходным
                                    // элементам по объему. Копируемая арматура расположится некорректно! Выберите
                                    // дальнейшее действие:"
                                    MainInstruction = Language.GetItem("h2"),

                                    // "Применить выбранное действие к последующим случаям"
                                    ExtraCheckBoxText = Language.GetItem("h3")
                                };

                                // "Пропустить копирование"
                                dialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, Language.GetItem("h4"));

                                // "Выполнить копирование"
                                dialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink2, Language.GetItem("h5"));

                                // "Отмена"
                                dialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink3, Language.GetItem("h6"));

                                var result = dialog.Show();

                                if (result == TaskDialogResult.CommandLink1)
                                {
                                    if (dialog.WasExtraCheckBoxChecked())
                                        skipIfNoMatchSolids = true;
                                    continue;
                                }

                                if (result == TaskDialogResult.CommandLink2)
                                {
                                    if (dialog.WasExtraCheckBoxChecked())
                                        copyIfNoMatchSolids = true;
                                }

                                if (result == TaskDialogResult.CommandLink3)
                                    break;
                            }
                        }

                        var targetCentroid = GetCentroid(targetSolids);

                        using (var tr = new Transaction(doc, "Copy between hosts"))
                        {
                            tr.Start();
                            WarningSwallower.Set(tr);

                            var translation = targetCentroid - sourceCentroid;

                            var copiedElements = new List<Element>();

                            if (sourceReinforcement.Any())
                            {
                                copiedElements.AddRange(ElementTransformUtils.CopyElements(
                                       doc,
                                       sourceReinforcement.Select(e => e.Id).ToList(),
                                       translation)
                                   .Select(id => doc.GetElement(id)));
                            }

                            if (sourceGroupsAndAssemblies.Any())
                            {
                                copiedElements.AddRange(
                                    ElementTransformUtils.CopyElements(
                                        doc,
                                        sourceGroupsAndAssemblies.Select(p => p.Value.Id).ToList(),
                                        translation)
                                        .Select(id => doc.GetElement(id)));
                            }

                            if (!double.IsNaN(sourceColumnRotation) &&
                                targetHost is FamilyInstance { IsSlantedColumn: false, Location: LocationPoint targetFamilyLocationPoint })
                            {
                                var targetColumnRotation = targetFamilyLocationPoint.Rotation;

                                foreach (var element in copiedElements)
                                {
                                    ElementTransformUtils.RotateElement(
                                        doc,
                                        element.Id,
                                        Line.CreateUnbound(targetCentroid, XYZ.BasisZ),
                                        targetColumnRotation - sourceColumnRotation);
                                }
                            }

                            foreach (var element in copiedElements)
                            {
                                if (element is Rebar rebar)
                                    rebar.SetHostId(doc, targetHost.Id);
                            }

                            tr.Commit();
                        }
                    }
                    catch (Autodesk.Revit.Exceptions.OperationCanceledException)
                    {
                        break;
                    }
                }

                trGroup.Assimilate();
            }
        });
    }

    private XYZ GetCentroid(IEnumerable<Solid> solids)
    {
        return GetCenterPoint(solids.Select(solid => solid.ComputeCentroid()).ToList());
    }

    private IEnumerable<Solid> GetSolids(Element host)
    {
        var geometry = host.get_Geometry(new Options());
        if (geometry != null)
            geometry = geometry.GetTransformed(Transform.Identity);

        Debug.Assert(geometry != null, nameof(geometry) + " != null");
        foreach (var geometryObject in geometry)
        {
            if (geometryObject is Solid { Volume: > 0.0 } solid)
                yield return solid;
        }
    }

    private bool IsMatchVolume(IEnumerable<Solid> sourceSolids, IEnumerable<Solid> targetSolids)
    {
        var sourceVolume = sourceSolids.Select(s => s.Volume).Sum();
        var targetVolume = targetSolids.Select(s => s.Volume).Sum();
        return Math.Abs(sourceVolume - targetVolume) < 0.001;
    }

    private XYZ GetCenterPoint(IReadOnlyCollection<XYZ> points)
    {
        var allX = points.Select(p => p.X).ToArray();
        var allY = points.Select(p => p.Y).ToArray();
        var allZ = points.Select(p => p.Z).ToArray();
        return new XYZ(
            allX.Sum() / allX.Length, allY.Sum() / allY.Length, allZ.Sum() / allZ.Length);
    }
}