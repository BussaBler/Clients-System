using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Client_System_C_
{
    public sealed partial class UserProfilePage : Page
    {
        private int currentInternalId;
        private CalendarDatePicker calendarDate;
        private readonly List<ItemEntry> currentItems = [];

        public UserProfilePage()
        {
            this.InitializeComponent();
            calendarDate = new CalendarDatePicker();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter is string cpf)
            {
                LoadUserProfile(cpf);
            }
        }

        private void LoadUserProfile(int internalId)
        {
            var user = DataAcess.GetUser(internalId);
            if (user == null) return;

            currentInternalId = internalId;
            userHeader.Text = $"{user.IdName} (CPF/CNPJ: {user.CPF})";

            var userInfoText = new List<string> {$"Nome para Contato: {user.ContactName}"};
            if (!string.IsNullOrEmpty(user.Email)) userInfoText.Add($"Email: {user.Email}");
            if (!string.IsNullOrEmpty(user.Phone)) userInfoText.Add($"Celular/Telefone: {user.Phone}");

            var addressInfo = new List<string>();
            if (!string.IsNullOrEmpty(user.Street)) addressInfo.Add($"Rua: {user.Street}");
            if (!string.IsNullOrEmpty(user.AdressNumber)) addressInfo.Add($"Número: {user.AdressNumber}");
            if (!string.IsNullOrEmpty(user.Neighboorhood)) addressInfo.Add($"Bairro: {user.Neighboorhood}");
            if (!string.IsNullOrEmpty(user.City)) addressInfo.Add($"Cidade: {user.City}");

            if (addressInfo.Count > 0) userInfoText.Add(string.Join(" | ", addressInfo));

            userInfo.Text = userInfoText.Count > 0 ? string.Join("\n", userInfoText) : "Sem informações adicionais.";
            userInfo.LineHeight += 1;

            LoadPurchaseHistory(currentInternalId);
        }

        private static DataAcess.User? TryGetUser(string phoneOrCpf)
        {
            var user = DataAcess.GetUserByPhone(phoneOrCpf);
            if (user == null)
            {
                user = DataAcess.GetUserByCpf(phoneOrCpf);
            }
            return user;
        }

        private void LoadUserProfile(string phoneOrCpf)
        {
            var user = TryGetUser(phoneOrCpf);
            if (user == null) return;

            currentInternalId = user.InternalId;
            userHeader.Text = $"{user.IdName} (CPF/CNPJ: {user.CPF})";

            var userInfoText = new List<string>() {$"Nome para Contato: {user.ContactName}"};
            if (!string.IsNullOrEmpty(user.Email)) userInfoText.Add($"Email: {user.Email}");
            if (!string.IsNullOrEmpty(user.Phone)) userInfoText.Add($"Celular/Telefone: {user.Phone}");

            var addressInfo = new List<string>();
            if (!string.IsNullOrEmpty(user.Street)) addressInfo.Add($"Rua: {user.Street}");
            if (!string.IsNullOrEmpty(user.AdressNumber)) addressInfo.Add($"Número: {user.AdressNumber}");
            if (!string.IsNullOrEmpty(user.Neighboorhood)) addressInfo.Add($"Bairro: {user.Neighboorhood}");
            if (!string.IsNullOrEmpty(user.City)) addressInfo.Add($"Cidade: {user.City}");

            if (addressInfo.Count > 0) userInfoText.Add(string.Join(" | ", addressInfo));

            userInfo.Text = userInfoText.Count > 0 ? string.Join("\n", userInfoText) : "Sem informações adicionais.";
            userInfo.LineHeight += 1;

            LoadPurchaseHistory(currentInternalId);
        }

        private void LoadPurchaseHistory(int internalId)
        {
            purchaseListPanel.Children.Clear();
            var history = DataAcess.GetPurchaseHistory(internalId);

            if (history.Count == 0)
            {
                purchaseListPanel.Children.Add(new TextBlock
                {
                    Text = "Nenhuma compra encontrada.",
                    FontSize = 20,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 10, 0, 10)
                });
                return;
            }

            var groupedPurchases = history.GroupBy(p => new { p.PurchaseId, p.Date, p.TotalPrice }).OrderByDescending(g => g.Key.Date);

            foreach (var group in groupedPurchases)
            {
                var purchaseHeader = new TextBlock
                {
                    Text = $"Compra em {group.Key.Date}, Total R${group.Key.TotalPrice:F2}",
                    FontSize = 20,
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(12, 8, 0, 4)
                };
                purchaseListPanel.Children.Add(purchaseHeader);

                foreach (var item in group)
                {
                    string discountText = !string.IsNullOrEmpty(item.Discount) ? $" com desconto de {item.Discount}" : string.Empty;
                    var itemBlock = new TextBlock
                    {
                        Text = $"  • {item.ItemName} x {item.Quantity}un R${item.Price:F2}{discountText}",
                        Margin = new Thickness(10, 0, 0, 5)
                    };
                    purchaseListPanel.Children.Add(itemBlock);
                }
            }
        }

        private async void NewPurchaseButton_Click(object sender, RoutedEventArgs e)
        {
            currentItems.Clear();
            var newPurchaseDialog = new ContentDialog
            {
                Title = "Nova Compra",
                PrimaryButtonText = "Salvar",
                SecondaryButtonText = "Cancelar",
                XamlRoot = this.Content.XamlRoot,
                Content = BuildNewPurchaseDialogContent()
            };

            var result = await newPurchaseDialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                DateTimeOffset dateTimeOffset = calendarDate.Date.GetValueOrDefault(DateTimeOffset.MinValue);
                int purchaseId = DataAcess.InsertPurchase(currentInternalId, dateTimeOffset.Date.ToShortDateString());

                foreach (var item in currentItems)
                {
                    string itemName = item.itemNameBox.Text;
                    int quantity = (int)item.quantityBox.Value;
                    double price = item.priceBox.Value;
                    DataAcess.InsertPurchaseItem(purchaseId, itemName, quantity, price, item.discountBox.Text);
                }

                DataAcess.UpdateTotalPrice(purchaseId);
                LoadPurchaseHistory(currentInternalId);
            }
        }

        private StackPanel BuildNewPurchaseDialogContent()
        {
            var rootPanel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 8 };
            calendarDate = new CalendarDatePicker { PlaceholderText = "Data", Date = DateTimeOffset.Now };
            rootPanel.Children.Add(calendarDate);

            var itemsPanel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 5 };
            rootPanel.Children.Add(itemsPanel);

            var addItemButton = new Button { Content = "Adicionar Item", HorizontalAlignment = HorizontalAlignment.Left };
            addItemButton.Click += (s, e) => AddItem(itemsPanel);
            rootPanel.Children.Add(addItemButton);

            return rootPanel;
        }

        private void AddItem(StackPanel itemsPanel)
        {
            var rowPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 5, Margin = new Thickness(0, 4, 0, 0) };
            var itemNameBox = new TextBox { PlaceholderText = "Nome do Item", Width = 110 };
            var quantityBox = new NumberBox { PlaceholderText = "Qtd", SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Compact, SmallChange = 1, LargeChange = 10, Value = 1 };
            var priceBox = new NumberBox { PlaceholderText = "Preço", Width = 70, Value = 0 };
            var discountTextBox = new TextBox { PlaceholderText = "Desconto", Width = 100 };

            rowPanel.Children.Add(itemNameBox);
            rowPanel.Children.Add(quantityBox);
            rowPanel.Children.Add(priceBox);
            rowPanel.Children.Add(discountTextBox);

            var removeButton = new Button { Content = "Remover", Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red), Width = 100 };
            var entry = new ItemEntry(itemNameBox, quantityBox, priceBox, discountTextBox);

            removeButton.Click += (s, e) =>
            {
                itemsPanel.Children.Remove(rowPanel);
                currentItems.Remove(entry);
            };

            rowPanel.Children.Add(removeButton);
            currentItems.Add(entry);
            itemsPanel.Children.Add(rowPanel);
        }

        private class ItemEntry
        {
            public TextBox itemNameBox;
            public NumberBox quantityBox;
            public NumberBox priceBox;
            public TextBox discountBox;

            public ItemEntry(TextBox item, NumberBox quantity, NumberBox price, TextBox discount)
            {
                itemNameBox = item;
                quantityBox = quantity;
                priceBox = price;
                discountBox = discount;
            }
        }

        private async void EditProfileButton_Click(object sender, RoutedEventArgs e)
        {
            var user = DataAcess.GetUser(currentInternalId);
            if (user == null) return;

            var firstNameBox = new TextBox { PlaceholderText = "Nome", Text = user.IdName };
            var lastNameBox = new TextBox { PlaceholderText = "Sobrenome", Text = user.ContactName };
            var emailBox = new TextBox { PlaceholderText = "Email", Text = user.Email };
            var phoneBox = new TextBox { PlaceholderText = "Celular/Telefone", Text = user.Phone };

            var contentPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Spacing = 8
            };
            contentPanel.Children.Add(firstNameBox);
            contentPanel.Children.Add(lastNameBox);
            contentPanel.Children.Add(emailBox);
            contentPanel.Children.Add(phoneBox);

            var editDialog = new ContentDialog
            {
                Title = "Editar Perfil",
                Content = contentPanel,
                PrimaryButtonText = "Salvar",
                CloseButtonText = "Cancelar",
                XamlRoot = this.Content.XamlRoot
            };

            var result = await editDialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                DataAcess.UpdateUser(currentInternalId, firstNameBox.Text, lastNameBox.Text, emailBox.Text, phoneBox.Text);
                LoadUserProfile(currentInternalId);
            }
        }
        private void ReturnButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.Frame.CanGoBack)
            {
                this.Frame.GoBack();
            }
        }
    }
}