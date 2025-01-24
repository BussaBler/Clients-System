using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Client_System_C_
{
    public sealed partial class RemoveMachinePage : Page
    {
        public RemoveMachinePage()
        {
            this.InitializeComponent();
        }

        private async void RemoveMachineButton_Click(object sender, RoutedEventArgs e)
        {
            string machineId = machineIdBox.Text.Trim();

            if (string.IsNullOrEmpty(machineId))
            {
                machineIdBox.Focus(FocusState.Keyboard);
                return;
            }

            var machine = DataAcess.GetMachine(machineId);
            if (machine == null)
            {
                var dialog = new ContentDialog
                {
                    Title = "Máquina não encontrada",
                    Content = $"Nenhuma máquina encontrada com o ID {machineId}.",
                    CloseButtonText = "OK",
                    XamlRoot = this.Content.XamlRoot,
                    Background = (Microsoft.UI.Xaml.Media.Brush)App.Current.Resources["SystemFillColorCriticalBackgroundBrush"]
                };
                await dialog.ShowAsync();
                return;
            }

            DataAcess.RemoveMachine(machineId);

            var successDialog = new ContentDialog
            {
                Title = "Máquina Removida",
                Content = $"Máquina com o ID {machineId} foi removida.",
                CloseButtonText = "OK",
                XamlRoot = this.Content.XamlRoot,
                Background = (Microsoft.UI.Xaml.Media.Brush)App.Current.Resources["SystemFillColorSuccessBackgroundBrush"]
            };
            await successDialog.ShowAsync();

            machineIdBox.Text = string.Empty;
        }
    }
}