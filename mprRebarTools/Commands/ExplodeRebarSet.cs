namespace mprRebarTools.Commands
{
    using System;
    using System.Linq;
    using Autodesk.Revit.Attributes;
    using Autodesk.Revit.DB;
    using Autodesk.Revit.DB.Structure;
    using Autodesk.Revit.UI;
    using Autodesk.Revit.UI.Selection;
    using SelectionFilters;

    /// <summary>
    /// Взорвать набор
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class ExplodeRebarSet : IExternalCommand
    {
        /// <inheritdoc />
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            return CommandExecutor.Execute(() =>
            {
                var doc = commandData.Application.ActiveUIDocument.Document;

                var rebarSets = commandData.Application.ActiveUIDocument.Selection
                    .PickObjects(ObjectType.Element, new RebarSetSelectionFilter(), "Pick rebar sets")
                    .Select(r => doc.GetElement(r))
                    .Cast<Rebar>()
                    .ToList();

                if (!rebarSets.Any())
                    throw new OperationCanceledException();

                using (var trGroup = new TransactionGroup(doc, "Explode rebar sets"))
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

                var step = rebar.MaxSpacing;
                var distributionPath = rebar.GetShapeDrivenAccessor().GetDistributionPath();
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
#if R2022
                    if (!rebar.DoesBarExistAtPosition(i))
                        continue;
#endif
                    var translation = distributionPath.Direction * step * i;

                    if (ElementTransformUtils.CopyElement(
                                rebar.Document, rebar.Id, translation).FirstOrDefault() is ElementId newRebarId &&
                        rebar.Document.GetElement(newRebarId) is Rebar newRebar)
                    {
#if R2017
                        newRebar.SetLayoutAsSingle();
#else
                        newRebar.GetShapeDrivenAccessor().SetLayoutAsSingle();
#endif

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
}
