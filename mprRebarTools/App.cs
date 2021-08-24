namespace mprRebarTools
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Windows.Media.Imaging;
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
            var contextualHelp = new ContextualHelp(ContextualHelpType.Url, ModPlus_Revit.App.RibbonBuilder.GetHelpUrl(intF.Name));
            
            var splitButtonData = new SplitButtonData(
                "RemoveRebarFromHost",
                "Remove rebar from host");

            var firstButton = GetButton("RemoveRebarFromHost", Language.GetItem("n1"), Language.GetItem("d1"));
            firstButton.SetContextualHelp(contextualHelp);
            var help = firstButton.GetContextualHelp();
            var sb = (SplitButton)panel.AddItem(splitButtonData);
            sb.AddPushButton(firstButton);
            sb.SetContextualHelp(help);
            
            foreach (var t in new List<Tuple<string, string, string>>
            {
                new Tuple<string, string, string>("CopyRebarBetweenHosts", Language.GetItem("n2"), Language.GetItem("d2")),
                new Tuple<string, string, string>("ExplodeRebarSet", Language.GetItem("n3"), Language.GetItem("d3")),
                new Tuple<string, string, string>("SplitRebarSet", Language.GetItem("n4"), Language.GetItem("d4")),
            })
            {
                sb.AddPushButton(GetButton(t.Item1, t.Item2, t.Item3));
            }
        }

        private PushButtonData GetButton(string name, string lName, string description)
        {
            return new PushButtonData(
                name,
                ModPlus_Revit.App.RibbonBuilder.ConvertLName(lName),
                Assembly.GetExecutingAssembly().Location,
                $"mprRebarTools.Commands.{name}")
            {
                ToolTip = description,
                LargeImage = GetBitmapImage(
                    $"pack://application:,,,/mprRebarTools_{ModPlusConnector.Instance.AvailProductExternalVersion};component/Resources/{name}_32x32.png"),
                Image = GetBitmapImage(
                    $"pack://application:,,,/mprRebarTools_{ModPlusConnector.Instance.AvailProductExternalVersion};component/Resources/{name}_16x16.png")
            };
        }
        
        private static BitmapImage GetBitmapImage(string uri)
        {
            // https://stackoverflow.com/a/65111729/4944499
            // https://stackoverflow.com/a/65111729/4944499
            var bi = new BitmapImage();
            bi.BeginInit();
            bi.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
            bi.UriSource = new Uri(uri, UriKind.RelativeOrAbsolute);
            bi.EndInit();
            return bi;
        }
    }
}
