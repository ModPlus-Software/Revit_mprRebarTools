namespace mprRebarTools.Commands
{
    using System;
    using Autodesk.Revit.Attributes;
    using Autodesk.Revit.DB;
    using Autodesk.Revit.DB.Structure;
    using Autodesk.Revit.UI;
    using Autodesk.Revit.UI.Selection;
    using SelectionFilters;

    /// <summary>
    /// Разделить арматурный набор на два по указанному стержню в наборе
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class SplitRebarSet : IExternalCommand
    {
        /// <inheritdoc />
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            return CommandExecutor.Execute(() =>
            {
                var doc = commandData.Application.ActiveUIDocument.Document;

                var rebarSetReference = commandData.Application.ActiveUIDocument.Selection
                    .PickObject(ObjectType.Element, new RebarSetSelectionFilter(), "Pick rebar set");

                if (!(doc.GetElement(rebarSetReference) is Rebar rebarSet))
                    throw new OperationCanceledException();

                var globalPoint = commandData.Application.ActiveUIDocument.Selection.PickObject(
                    ObjectType.PointOnElement, "Pick rebar in set")
                    .GlobalPoint;

                using (var trGroup = new TransactionGroup(doc, "Split rebar set"))
                {
                    trGroup.Start();

                    Split(rebarSet, globalPoint);

                    trGroup.Assimilate();
                }
            });
        }

        private void Split(Rebar rebar, XYZ point)
        {

        }
    }
}
