using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage;

namespace Client_System_C_
{
    public sealed partial class SettingsPage : Page
    {
        private readonly ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

        public SettingsPage()
        {
            this.InitializeComponent();
            LoadSettings();
        }

        private void LoadSettings()
        {
            cellPhoneBox.Text = localSettings.Values["CellPhone"] as string ?? "";
            phoneBox.Text = localSettings.Values["Phone"] as string ?? "";
            budgetDeadlineBox.Text = localSettings.Values["BudgetDeadline"] as string ?? "";
        }

        private void SaveSettings_Click(object sender, RoutedEventArgs e)
        {
            localSettings.Values["CellPhone"] = cellPhoneBox.Text.Trim();
            localSettings.Values["Phone"] = phoneBox.Text.Trim();
            localSettings.Values["BudgetDeadline"] = budgetDeadlineBox.Text.Trim();

            var successDialog = new ContentDialog
            {
                Title = "Configurações Salvas",
                Content = "As configurações foram salvas com sucesso.",
                CloseButtonText = "OK",
                XamlRoot = this.Content.XamlRoot
            };
            _ = successDialog.ShowAsync();
        }
    }
}