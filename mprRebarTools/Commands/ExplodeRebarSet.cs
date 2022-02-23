namespace mprRebarTools.Commands;

using System;
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
/// Взорвать набор
/// </summary>
[Transaction(TransactionMode.Manual)]
[Regeneration(RegenerationOption.Manual)]
//// ReSharper disable once UnusedMember.Global
public class ExplodeRebarSet : IExternalCommand
{
    /// <inheritdoc />
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        return CommandExecutor.Execute(() =>
        {
#if !DEBUG
                ModPlusAPI.Statistic.SendCommandStarting($"mpr{nameof(ExplodeRebarSet)}", ModPlusConnector.Instance.AvailProductExternalVersion);
#endif
            var doc = commandData.Application.ActiveUIDocument.Document;

            // "Выберите арматурные наборы"
            var rebarSets = commandData.Application.ActiveUIDocument.Selection
                .PickObjects(ObjectType.Element, new RebarSetSelectionFilter(), Language.GetItem("m4"))
                .Select(r => doc.GetElement(r))
                .Cast<Rebar>()
                .ToList();

            if (!rebarSets.Any())
                throw new OperationCanceledException();

            using (var trGroup = new TransactionGroup(doc, Language.GetItem("n3")))
            {
                trGroup.Start();

                foreach (var rebar in rebarSets)
                {
                    Explode(rebar);
                }

                trGroup.Assimilate();
            }
        });
    }

    private void Explode(Rebar rebar)
    {
        using (var tr = new Transaction(rebar.Document, "Explode rebar set"))
        {
            tr.Start();
                
#if R2017
                var distributionPath = rebar.GetDistributionPath();
#else
            var distributionPath = rebar.GetShapeDrivenAccessor().GetDistributionPath();
#endif
            var arrayLength = distributionPath.Length;
            var realStep = arrayLength / (rebar.NumberOfBarPositions - 1);
                
            var rebarConstraintsManager = rebar.GetRebarConstraintsManager();
            var sourceRebarConstraints = rebarConstraintsManager
                .GetAllConstrainedHandles()
                .Where(h => h.GetHandleType() != RebarHandleType.RebarPlane)
                .Select(rebarConstrainedHandle =>
                    rebarConstraintsManager.GetCurrentConstraintOnHandle(rebarConstrainedHandle))
                .ToList();

            for (int i = 0; i < rebar.NumberOfBarPositions; i++)
            {
                if (!rebar.IncludeFirstBar && i == 0)
                    continue;
                if (!rebar.IncludeLastBar && i == rebar.NumberOfBarPositions - 1)
                    continue;

#if !R2017 && !R2018 && !R2019 && !R2020 && !R2021
                if (!rebar.DoesBarExistAtPosition(i))
                    continue;
#endif
                var translation = distributionPath.Direction * realStep * i;

                if (ElementTransformUtils.CopyElement(rebar.Document, rebar.Id, translation).FirstOrDefault() is { } newRebarId &&
                    rebar.Document.GetElement(newRebarId) is Rebar newRebar)
                {
                    newRebar.SetLayoutAsSingle();

                    if (rebar.DistributionType == DistributionType.VaryingLength)
                    {
                        var newRebarConstraintsManager = newRebar.GetRebarConstraintsManager();

                        var index = -1;
                        foreach (var rebarConstrainedHandle in newRebarConstraintsManager.GetAllConstrainedHandles())
                        {
                            var rebarHandleType = rebarConstrainedHandle.GetHandleType();
                            if (rebarHandleType == RebarHandleType.RebarPlane)
                                continue;

                            index++;

                            var sourceRebarConstraint = sourceRebarConstraints[index];
                            newRebarConstraintsManager.SetPreferredConstraintForHandle(rebarConstrainedHandle, sourceRebarConstraint);
                        }
                    }
                }
            }

            rebar.Document.Delete(rebar.Id);

            tr.Commit();
        }
    }
}