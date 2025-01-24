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

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Client_System_C_
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class FindUserPage : Page
    {
        public FindUserPage()
        {
            this.InitializeComponent();
        }

        private async void FindButton_Click(object sender, RoutedEventArgs e)
        {
            string maskedId = string.Empty;

            if (idTypeFindRadioButtons.SelectedIndex == 0)
            {
                maskedId = userCPF.Text.Trim();
            }
            if (idTypeFindRadioButtons.SelectedIndex == 1)
            {
                maskedId = userCNPJ.Text.Trim();
            }

            string id = maskedId.Replace(".", "").Replace("-", "").Replace("_", "").Replace("/", "").Trim();
            DataAcess.User? user = null;

            if (!string.IsNullOrEmpty(id))
            {
                user = DataAcess.GetUser(maskedId);
            }
            else if (!string.IsNullOrEmpty(userLastName.Text.Trim()))
            {
                user = DataAcess.GetUserByLastName(userLastName.Text.Trim());
            } else if (!string.IsNullOrEmpty(userPhone.Text.Replace("(", "").Replace(")", "").Replace("-", "").Replace("_", "").Trim()))
            {
                user = DataAcess.GetUserByPhone(userPhone.Text.Replace("(", "").Replace(")", "").Replace("-", "").Replace("_", "").Trim());
            }

            if (user == null)
            {
                var dialog = new ContentDialog
                {
                    Title = "Cliente não encontrado",
                    Content = "Nenhum cliente encontrado.",
                    CloseButtonText = "OK",
                    XamlRoot = this.Content.XamlRoot,
                    Background = (Brush)App.Current.Resources["SystemFillColorCriticalBackgroundBrush"]
                };
                await dialog.ShowAsync();
                return;
            }

            string idToLoad = string.IsNullOrEmpty(user.CPF.Replace("_", "").Replace("-", "")) ? user.Phone : user.CPF;
            MainPage.Current?.ContentFrame.Navigate(typeof(UserProfilePage), idToLoad);
        }

        private void idTypeFindRadioButtons_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (idTypeFindRadioButtons.SelectedIndex == 0)
            {
                userCPF.Visibility = Visibility.Visible;
                userCNPJ.Visibility = Visibility.Collapsed;
                userCNPJ.Text = string.Empty;
            }
            else
            {
                userCPF.Visibility = Visibility.Collapsed;
                userCPF.Text = string.Empty;
                userCNPJ.Visibility = Visibility.Visible;
            }
        }
    }
}
