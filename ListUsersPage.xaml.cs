using System.Collections.Generic;
using System.Linq;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
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
            clientListPanel.Children.Clear(); // optional: clear old entries

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

            var sortedUsers = users.OrderBy(u => u.IdName).ThenBy(u => u.ContactName);

            foreach (var user in sortedUsers)
            {
                var userButton = new Button
                {
                    Margin = new Thickness(10),
                    Padding = new Thickness(0),
                    Background = new SolidColorBrush((Color)App.Current.Resources["SystemBaseMediumColor"]),
                    BorderThickness = new Thickness(0),
                    CornerRadius = new CornerRadius(8),
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    HorizontalContentAlignment = HorizontalAlignment.Left,
                    Tag = user
                };
                userButton.Click += UserButton_Clicked;

                var contentPanel = new StackPanel
                {
                    Margin = new Thickness(10),
                };

                contentPanel.Children.Add(new TextBlock
                {
                    Text = $"{user.IdName}",
                    FontSize = 20,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Colors.Black),
                });

                var details = new List<string> { $"CPF/CNPJ: {user.CPF}", $"Nome para Contato: {user.ContactName}" };
                if (!string.IsNullOrEmpty(user.Email)) details.Add($"Email: {user.Email}");
                if (!string.IsNullOrEmpty(user.Phone)) details.Add($"Celular/Telefone: {user.Phone}");

                contentPanel.Children.Add(new TextBlock
                {
                    Text = string.Join("\n", details),
                    FontSize = 16,
                    Foreground = new SolidColorBrush(Colors.Black),
                    Margin = new Thickness(0, 5, 0, 0)
                });

                userButton.Content = contentPanel;
                clientListPanel.Children.Add(userButton);
            }
        }

        private void UserButton_Clicked(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag is DataAcess.User user)
            {
                string idToLoad = string.IsNullOrEmpty(user.CPF.Replace(".", "").Replace("-", "").Replace("_", "").Replace("/", "").Trim()) ? user.Phone : user.CPF;
                Frame.Navigate(typeof(UserProfilePage), idToLoad);
            }
        }
    }
}
