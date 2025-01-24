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
using Microsoft.UI.Text;
using Microsoft.UI;
using Windows.UI;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Client_System_C_
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ListUsersPage : Page
    {
        public ListUsersPage()
        {
            this.InitializeComponent();
            LoadUsers();
        }

        private void LoadUsers()
        {
            var users = DataAcess.GetAllUsers();
            if (users.Count == 0)
            {
                clientListPanel.Children.Add(new TextBlock
                {
                    Text = "Nenhum cliente cadastrado.",
                    FontSize = 18,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(10)
                });
                return;
            }

            var sortedUsers = users.OrderBy(u => u.FirstName).ThenBy(u => u.LastName);
            foreach (var user in sortedUsers)
            {
                var userPanel = new StackPanel
                {
                    Margin = new Thickness(10),
                    Padding = new Thickness(10),
                    Background = new SolidColorBrush((Color)App.Current.Resources["SystemBaseMediumColor"]),
                    CornerRadius = new CornerRadius(8)
                };

                userPanel.Children.Add(new TextBlock
                {
                    Text = $"{user.FirstName} {user.LastName}",
                    FontSize = 20,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Colors.Black)
                });

                var details = new List<string> { $"CPF/CNPJ: {user.CPF}" };
                if (!string.IsNullOrEmpty(user.Email)) details.Add($"Email: {user.Email}");
                if (!string.IsNullOrEmpty(user.Phone)) details.Add($"Celular/Telefone: {user.Phone}");

                var detailsText = new TextBlock
                {
                    Text = string.Join("\n", details),
                    FontSize = 16,
                    Foreground = new SolidColorBrush(Colors.Black),
                    Margin = new Thickness(0, 5, 0, 0)
                };
                userPanel.Children.Add(detailsText);

                clientListPanel.Children.Add(userPanel);
            }
        }
    }
}
