using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System.Diagnostics;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Client_System_C_
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class RemoveUserPage : Page
    {
        public RemoveUserPage()
        {
            this.InitializeComponent();
        }

        private async void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            string maskedCpf = string.Empty;

            if (idTypeFindRadioButtons.SelectedIndex == 0)
            {
                maskedCpf = removeCPF.Text.Trim();
            }
            if (idTypeFindRadioButtons.SelectedIndex == 1)
            {
                maskedCpf = removeCNPJ.Text.Trim();
            }

            var user = DataAcess.GetUserByCpf(maskedCpf);
            if (user == null)
            {
                var dialog = new ContentDialog
                {
                    Title = "Cliente não encontrado",
                    Content = $"Nenhum cliente encontrado com o CPF/CNPJ {maskedCpf}.",
                    CloseButtonText = "OK",
                    XamlRoot = this.Content.XamlRoot,
                    Background = (Brush)App.Current.Resources["SystemFillColorCriticalBackgroundBrush"]
                };
                await dialog.ShowAsync();
                return;
            }

            removeCPF.Text = string.Empty;
            DataAcess.RemoveUser(user.InternalId);

            var successDialog = new ContentDialog
            {
                Title = "Cliente removido",
                Content = $"Cliente com CPF/CNPJ {maskedCpf} foi removido.",
                CloseButtonText = "OK",
                XamlRoot = this.Content.XamlRoot,
                Background = (Brush)App.Current.Resources["SystemFillColorSuccessBackgroundBrush"]
            };
            await successDialog.ShowAsync();
        }

        private void IdTypeFindRadioButtons_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (idTypeFindRadioButtons.SelectedIndex == 0)
            {
                removeCPF.Visibility = Visibility.Visible;
                removeCNPJ.Visibility = Visibility.Collapsed;
                removeCNPJ.Text = string.Empty;
            }
            else
            {
                removeCPF.Visibility = Visibility.Collapsed;
                removeCPF.Text = string.Empty;
                removeCNPJ.Visibility = Visibility.Visible;
            }
        }
    }
}
