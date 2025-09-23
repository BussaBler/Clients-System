using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Client_System_C_
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class FindMachinePage : Page
    {
        public FindMachinePage()
        {
            this.InitializeComponent();
        }

        private async void FindButton_Click(object sender, RoutedEventArgs e)
        {
            string machineId = machineIdBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(machineId))
            {
                machineIdBox.Focus(FocusState.Programmatic);
                return;
            }

            var machine = DataAcess.GetMachineById(machineId);

            if (machine == null)
            {
                var dialog = new ContentDialog
                {
                    Title = "Máquina não encontrada",
                    Content = $"Nenhuma máquina encontrada com o ID {machineId}.",
                    CloseButtonText = "OK",
                    XamlRoot = this.Content.XamlRoot,
                    Background = (Brush)App.Current.Resources["SystemFillColorCriticalBackgroundBrush"]
                };
                await dialog.ShowAsync();
                return;
            }

            Frame.Navigate(typeof(MachineProfilePage), machineId);
        }

    }
}
