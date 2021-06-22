namespace mprRebarTools
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using Autodesk.Revit.UI;
    using ModPlusAPI;
    using ModPlusAPI.Windows;

    /// <inheritdoc/>
    public class App : IExternalApplication
    {
        /// <inheritdoc/>
        public Result OnStartup(UIControlledApplication application)
        {
            try
            {
                // create ribbon tab
                CreateRibbonTab(application);

                ////ModPlus_Revit.App.RibbonBuilder.HideTextOfSmallButtons(
                ////    "ModPlus",
                ////    new List<string> { "Grids mode", "Grids bubbles", "Rebars outside host", "Pick Annotations" });

                return Result.Succeeded;
            }
            catch (Exception exception)
            {
                ExceptionBox.Show(exception);
                return Result.Failed;
            }
        }

        /// <inheritdoc/>
        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }

        private void CreateRibbonTab(UIControlledApplication application)
        {
            var panel = ModPlus_Revit.App.RibbonBuilder.GetOrCreateRibbonPanel(
                application,
                "ModPlus",
                Language.TryGetCuiLocalGroupName("Конструкции"));
            
            if (panel == null)
                return;
            
            // interface of current ModPlus function
            var intF = new ModPlusConnector();
            var assembly = Assembly.GetExecutingAssembly().Location;
            var contextualHelp = new ContextualHelp(ContextualHelpType.Url, ModPlus_Revit.App.RibbonBuilder.GetHelpUrl(intF.Name));
        }
    }
}
