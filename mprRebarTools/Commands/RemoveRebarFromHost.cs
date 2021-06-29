namespace mprRebarTools.Commands
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Autodesk.Revit.Attributes;
    using Autodesk.Revit.DB;
    using Autodesk.Revit.UI;
    using Autodesk.Revit.UI.Selection;
    using ModPlusAPI;
    using SelectionFilters;

    /// <summary>
    /// Удалить арматуру из основы
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    //// ReSharper disable once UnusedMember.Global
    public class RemoveRebarFromHost : IExternalCommand
    {
        /// <inheritdoc/>
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            return CommandExecutor.Execute(() =>
            {
#if !DEBUG
                ModPlusAPI.Statistic.SendCommandStarting($"mpr{nameof(RemoveRebarFromHost)}", ModPlusConnector.Instance.AvailProductExternalVersion);
#endif
                var doc = commandData.Application.ActiveUIDocument.Document;
                
                // "Выберите элементы-основы"
                var hosts = commandData.Application.ActiveUIDocument.Selection.PickObjects(
                        ObjectType.Element, new HostSelectionFilter(), Language.GetItem("m1"))
                    .Select(r => doc.GetElement(r))
                    .ToList();

                if (!hosts.Any())
                    throw new OperationCanceledException();

                var idsToDelete = new List<ElementId>();

                foreach (var hostObject in hosts)
                {
                    idsToDelete.AddRange(Utils.GetReinforcement(hostObject).Select(e => e.Id));
                }

                if (!idsToDelete.Any())
                    return;
                
                using (var tr = new Transaction(doc, Language.GetItem("n1")))
                {
                    tr.Start();

                    doc.Delete(idsToDelete);
                        
                    tr.Commit();
                }
            });
        }
    }
}
