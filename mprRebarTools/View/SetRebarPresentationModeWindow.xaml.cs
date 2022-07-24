namespace mprRebarTools.View;

using System;
using System.Windows;
using ModPlusAPI;

/// <summary>
/// Логика взаимодействия для SetRebarPresentationModeWindow.xaml
/// </summary>
public partial class SetRebarPresentationModeWindow
{
    public SetRebarPresentationModeWindow(bool showSelectionModes)
    {
        InitializeComponent();
        Title = ModPlusAPI.Language.GetItem("h8");

        GridSelectionMode.Visibility = showSelectionModes ? Visibility.Visible : Visibility.Collapsed;
        
        Closed += OnClosed;

        var presentationMode = UserConfigFile.GetValue(ModPlusConnector.Instance.Name, "PresentationMode");
        if ($"Rb{presentationMode}" == nameof(RbFirstLast))
            RbFirstLast.IsChecked = true;
        else if ($"Rb{presentationMode}" == nameof(RbMiddle))
            RbMiddle.IsChecked = true;
        else 
            RbAll.IsChecked = true;

        CbProcessVariant.SelectedIndex = int.TryParse(UserConfigFile.GetValue(ModPlusConnector.Instance.Name, nameof(CbProcessVariant)), out var i) ? i : 0;
    }

    private void OnClosed(object sender, EventArgs e)
    {
        var presentationMode = "All";
        if (RbFirstLast.IsChecked == true)
            presentationMode = "FirstLast";
        else if (RbMiddle.IsChecked == true)
            presentationMode = "Middle";
        UserConfigFile.SetValue(ModPlusConnector.Instance.Name, "PresentationMode", presentationMode, true);

        UserConfigFile.SetValue(ModPlusConnector.Instance.Name, nameof(CbProcessVariant), CbProcessVariant.SelectedIndex.ToString(), true);
    }
    
    private void BtContinue_OnClick(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }
}