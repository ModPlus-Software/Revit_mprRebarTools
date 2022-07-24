namespace mprRebarTools.Commands;

using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using Enums;
using ModPlus_Revit;
using ModPlusAPI;
using ModPlusAPI.Enums;
using ModPlusAPI.Services;
using SelectionFilters;
using View;

/// <summary>
/// Изменить представление арматуры
/// </summary>
[Regeneration(RegenerationOption.Manual)]
[Transaction(TransactionMode.Manual)]
public class SetRebarPresentationMode : IExternalCommand
{
    /// <inheritdoc />
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        return CommandExecutor.Execute(() =>
        {
#if !DEBUG
            ModPlusAPI.Statistic.SendCommandStarting($"mpr{nameof(SetRebarPresentationMode)}", ModPlusConnector.Instance.AvailProductExternalVersion);
#endif
            var uiApplication = commandData.Application;
            var view = uiApplication.ActiveUIDocument.ActiveGraphicalView;
            var doc = uiApplication.ActiveUIDocument.Document;
            var preSelected = GetForPresentationMode(
                    uiApplication.ActiveUIDocument.Selection.GetElementIds().Select(id => doc.GetElement(id)), 
                    doc)
                .ToList();

            var win = new SetRebarPresentationModeWindow(!preSelected.Any());
            if (ModPlus.ShowModal(win) != true)
                return;

            var presentationMode = RebarPresentationMode.All;
            if (win.RbFirstLast.IsChecked == true)
                presentationMode = RebarPresentationMode.FirstLast;
            else if (win.RbMiddle.IsChecked == true)
                presentationMode = RebarPresentationMode.Middle;

            var objects = new List<object>();
            objects.AddRange(preSelected.Any()
                ? preSelected
                : GetForPresentationMode(GetElements((ElementsProcessVariant)win.CbProcessVariant.SelectedIndex, uiApplication), doc));

            if (objects.Any())
            {
                var resultService = new ResultService();

                // Изменить представление арматуры
                using (var tr = new Transaction(doc, Language.GetItem("h12")))
                {
                    tr.Start();
                    foreach (var o in objects)
                    {
                        var id = string.Empty;
                        try
                        {
                            if (o is Rebar rebar)
                            {
                                id = rebar.Id.ToString();
                                if (rebar.LayoutRule == RebarLayoutRule.Single)
                                {
                                    //// Нельзя изменить представление для арматуры с компоновкой "Один"
                                    resultService.Add(Language.GetItem("h14"), id, ResultItemType.Warning);
                                    continue;
                                }

                                if (rebar.CanApplyPresentationMode(view))
                                    rebar.SetPresentationMode(view, presentationMode);
                                else
                                    //// На текущем виде нельзя изменить представление арматуры
                                    resultService.Add(Language.GetItem("h13"), id, ResultItemType.Warning);
                            }
                            else if (o is RebarInSystem rebarInSystem)
                            {
                                id = rebarInSystem.Id.ToString();
                                if (rebarInSystem.CanApplyPresentationMode(view))
                                    rebarInSystem.SetPresentationMode(view, presentationMode);
                                else
                                    //// На текущем виде нельзя изменить представление арматуры
                                    resultService.Add(Language.GetItem("h13"), id, ResultItemType.Warning);
                            }
                            else if (o is RebarContainerItem rebarContainerItem)
                            {
                                if (rebarContainerItem.CanApplyPresentationMode(view))
                                    rebarContainerItem.SetPresentationMode(view, presentationMode);
                                else
                                    //// На текущем виде нельзя изменить представление арматуры
                                    resultService.Add(Language.GetItem("h13"), ResultItemType.Warning);
                            }
                        }
                        catch (Exception exception)
                        {
                            resultService.Add(exception.Message, id, ResultItemType.Error);
                        }
                    }

                    tr.Commit();
                }

                resultService.Show(ModPlus.GetRevitWindowHandle());
            }
        });
    }

    private IEnumerable<Element> GetElements(ElementsProcessVariant elementsProcessVariant, UIApplication uiApplication)
    {
        var view = uiApplication.ActiveUIDocument.ActiveGraphicalView;
        var doc = uiApplication.ActiveUIDocument.Document;
        switch (elementsProcessVariant)
        {
            case ElementsProcessVariant.AllElementsOnView:
                {
                    return new FilteredElementCollector(doc, view.Id)
                        .WhereElementIsNotElementType()
                        .Where(e => e.IsValidObject && e.Category != null)
                        .Where(ReinforcementSelectionFilter.IsAllowableElement)
                        .ToList();
                }

            case ElementsProcessVariant.SelectedElements:
                {
                    var pickedRefs = uiApplication.ActiveUIDocument.Selection.PickObjects(
                        ObjectType.Element, new ReinforcementSelectionFilter(), Language.GetItem("msg2"));

                    return pickedRefs.Select(reference => doc.GetElement(reference)).ToList();
                }

            case ElementsProcessVariant.AllButSelectedElementsOnView:
                {
                    var selected =
                        uiApplication.ActiveUIDocument.Selection.GetElementIds().Select(i => i.IntegerValue).ToList();
                    return new FilteredElementCollector(doc, view.Id)
                        .WhereElementIsNotElementType()
                        .Where(e => e.IsValidObject && e.Category != null && !selected.Contains(e.Id.IntegerValue))
                        .Where(e => IsValidBySelectedHost(e, selected))
                        .Where(ReinforcementSelectionFilter.IsAllowableElement)
                        .ToList();
                }

            default:
                {
                    var pickedRef = uiApplication.ActiveUIDocument.Selection.PickObject(
                        ObjectType.Element, new ReinforcementSelectionFilter(), Language.GetItem("msg1"));
                    return new List<Element>
                {
                    doc.GetElement(pickedRef)
                };
                }
        }
    }

    private IEnumerable<object> GetForPresentationMode(IEnumerable<Element> elements, Document doc)
    {
        foreach (var element in elements)
        {
            if (element is AreaReinforcement areaReinforcement)
            {
                foreach (var rebarInSystemId in areaReinforcement.GetRebarInSystemIds())
                {
                    if (doc.GetElement(rebarInSystemId) is RebarInSystem rebarInSystem)
                        yield return rebarInSystem;
                }
            }
            else if (element is PathReinforcement pathReinforcement)
            {
                foreach (var rebarInSystemId in pathReinforcement.GetRebarInSystemIds())
                {
                    if (doc.GetElement(rebarInSystemId) is RebarInSystem rebarInSystem)
                        yield return rebarInSystem;
                }
            }
            else if (element is RebarContainer rebarContainer)
            {
                for (var i = 0; i < rebarContainer.ItemsCount; i++)
                {
                    yield return rebarContainer.GetItem(i);
                }
            }
            else if (element is Rebar rebar)
            {
                yield return rebar;
            }
            else
            {
                foreach (var o in GetForPresentationMode(Utils.GetReinforcement(element), doc))
                {
                    yield return o;
                }
            }
        }
    }

    private bool IsValidBySelectedHost(Element element, ICollection<int> excludeIds)
    {
        var hostId = GetHostId(element);
        if (hostId != null && hostId != ElementId.InvalidElementId && excludeIds.Contains(hostId.IntegerValue))
            return false;
        return true;
    }

    private ElementId GetHostId(Element element)
    {
        switch (element)
        {
            case Rebar rebar:
                return rebar.GetHostId();
            case AreaReinforcement areaReinforcement:
                return areaReinforcement.GetHostId();
            case PathReinforcement pathReinforcement:
                return pathReinforcement.GetHostId();
            case RebarContainer rebarContainer:
                return rebarContainer.GetHostId();
#if !R2017 && !R2018 && !R2019 && !R2020
            case FabricSheet fabricSheet:
                return fabricSheet.HostId;
#endif
            default:
                return ElementId.InvalidElementId;
        }
    }
}