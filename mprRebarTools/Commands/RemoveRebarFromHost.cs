namespace mprRebarTools.Commands
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Autodesk.Revit.Attributes;
    using Autodesk.Revit.DB;
    using Autodesk.Revit.DB.Structure;
    using Autodesk.Revit.UI;
    using Autodesk.Revit.UI.Selection;
    using SelectionFilters;

    /// <summary>
    /// Удалить арматуру из основы
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class RemoveRebarFromHost : IExternalCommand
    {
        /// <inheritdoc/>
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            return CommandExecutor.Execute(() =>
            {
                var doc = commandData.Application.ActiveUIDocument.Document;
                
                var hosts = commandData.Application.ActiveUIDocument.Selection.PickObjects(
                        ObjectType.Element, new HostSelectionFilter(), "Pick host objects")
                    .Select(r => doc.GetElement(r))
                    .ToList();

                if (!hosts.Any())
                    throw new OperationCanceledException();

                var idsToDelete = new List<ElementId>();

                foreach (var hostObject in hosts)
                {
                    var rebarHostData = RebarHostData.GetRebarHostData(hostObject);
                    idsToDelete.AddRange(rebarHostData.GetAreaReinforcementsInHost().Select(r => r.Id));
                    idsToDelete.AddRange(rebarHostData.GetPathReinforcementsInHost().Select(r => r.Id));
                    idsToDelete.AddRange(rebarHostData.GetRebarContainersInHost().Select(r => r.Id));
                    idsToDelete.AddRange(rebarHostData.GetRebarsInHost().Select(r => r.Id));
                }

                if (idsToDelete.Any())
                {
                    using (var tr = new Transaction(doc, "Remove rebar from host"))
                    {
                        tr.Start();

                        doc.Delete(idsToDelete);
                        
                        tr.Commit();
                    }
                }
            });
        }
    }
}
