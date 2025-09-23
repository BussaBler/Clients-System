using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Printing;
using Windows.Graphics.Printing;
using Windows.Storage;
using static Client_System_C_.DataAcess;

namespace Client_System_C_
{
    public sealed partial class MachineProfilePage : Page
    {
        private int currentInternalId;
        private PrintDocument printDoc;
        private IPrintDocumentSource printDocSource;
        private StackPanel printContent;
        private ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
        
        public MachineProfilePage()
        {
            this.InitializeComponent();
            RegisterForPrinting();
        }

        private void RegisterForPrinting()
        {
            printDoc = new PrintDocument();
            printDocSource = printDoc.DocumentSource;
            printDoc.Paginate += PrintDocument_Paginate;
            printDoc.GetPreviewPage += PrintDocument_GetPreviewPage;
            printDoc.AddPages += PrintDocument_AddPages;
        }

        private void PrintDocument_GetPreviewPage(object sender, GetPreviewPageEventArgs e)
        {
            printDoc.SetPreviewPage(e.PageNumber, printContent);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter is string machineId)
            {
                LoadMachineProfile(machineId);
            }
        }

        private void LoadMachineProfile(int internalId)
        {
            var machine = DataAcess.GetMachine(internalId);
            if (machine == null) return;
            Debug.WriteLine("Machine loaded successfully!");

            currentInternalId = machine.InternalId;

            machineHeader.Text = $"{machine.MachineModel} (ID: {machine.MachineId})";

            var machineInfoText = new List<string>();
            if (!string.IsNullOrWhiteSpace(machine.OwnerName))
                machineInfoText.Add($"Propriet�rio: {machine.OwnerName}");
            if (!string.IsNullOrWhiteSpace(machine.OwnerPhone))
                machineInfoText.Add($"Celular/Telefone: {machine.OwnerPhone}");
            if (!string.IsNullOrWhiteSpace(machine.OwnerPhone2))
                machineInfoText.Add($"Celular/Telefone: {machine.OwnerPhone2}");

            machineInfo.Text = machineInfoText.Count > 0 ? string.Join("  |  ", machineInfoText) : "Sem informa��es adicionais";

            LoadRepairHistory(internalId);
        }

        private void LoadMachineProfile(string machineId)
        {
            var machine = DataAcess.GetMachineById(machineId);
            if (machine == null) return;
            Debug.WriteLine("Machine loaded successfully!");

            currentInternalId = machine.InternalId;

            machineHeader.Text = $"{machine.MachineModel} (ID: {machine.MachineId})";

            var machineInfoText = new List<string>();
            if (!string.IsNullOrWhiteSpace(machine.OwnerName))
                machineInfoText.Add($"Propriet�rio: {machine.OwnerName}");
            if (!string.IsNullOrWhiteSpace(machine.OwnerPhone))
                machineInfoText.Add($"Celular/Telefone: {machine.OwnerPhone}");
            if (!string.IsNullOrWhiteSpace(machine.OwnerPhone2))
                machineInfoText.Add($"Celular/Telefone: {machine.OwnerPhone2}");

            machineInfo.Text = machineInfoText.Count > 0 ? string.Join("  |  ", machineInfoText) : "Sem informa��es adicionais";

            LoadRepairHistory(currentInternalId);
        }

        private void LoadRepairHistory(int internalId)
        {
            repairListPanel.Children.Clear();
            var repairs = DataAcess.GetRepairsFromMachine(internalId);

            if (repairs.Count == 0)
            {
                repairListPanel.Children.Add(new TextBlock
                {
                    Text = "Nenhum reparo encontrado.",
                    FontSize = 18,
                    TextAlignment = TextAlignment.Center,
                    Margin = new Thickness(10)
                });
                return;
            }

            foreach (var repair in repairs)
            {
                var repairPanel = new Grid
                {
                    Margin = new Thickness(10),
                    CornerRadius = new CornerRadius(8)
                };

                repairPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                repairPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var repairInfoPanel = new StackPanel { Padding = new Thickness(10) };

                string done = repair.Done ? "Sim" : "N�o";
                var repairHeader = new TextBlock
                {
                    Text = $"Reparo em {repair.Date}, Custo: R${repair.Price:F2}, Conclu�do: {done}",
                    FontSize = 20,
                    FontWeight = Microsoft.UI.Text.FontWeights.Bold
                };
                repairInfoPanel.Children.Add(repairHeader);

                var repairDesc = new TextBlock
                {
                    Text = repair.Description,
                    FontSize = 16,
                    Margin = new Thickness(0, 5, 0, 0)
                };
                repairInfoPanel.Children.Add(repairDesc);

                Grid.SetColumn(repairInfoPanel, 0);
                repairPanel.Children.Add(repairInfoPanel);

                var editButton = new AppBarButton
                {
                    Icon = new SymbolIcon(Symbol.Edit),
                    Width = 50,
                    Height = 50,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Center
                };
                editButton.Click += (s, e) => EditRepairButton_Click(repair.RepairId, repair.Description, repair.Price);

                Grid.SetColumn(editButton, 1);
                repairPanel.Children.Add(editButton);

                repairListPanel.Children.Add(repairPanel);
            }
        }

        private async void EditProfileButton_Click(object sender, RoutedEventArgs e)
        {
            var machine = DataAcess.GetMachine(currentInternalId);
            if (machine == null) return;

            var dialog = new ContentDialog
            {
                Title = "Editar M�quina",
                PrimaryButtonText = "Salvar",
                SecondaryButtonText = "Cancelar",
                XamlRoot = this.Content.XamlRoot
            };

            var panel = new StackPanel { Spacing = 8 };

            var machineModelBox = new TextBox { Text = machine.MachineModel, Header = "Modelo da M�quina" };
            var ownerNameBox = new TextBox { Text = machine.OwnerName, Header = "Propriet�rio" };
            var ownerPhoneBox = new TextBox { Text = machine.OwnerPhone, Header = "Celular/Telefone" };
            var ownerPhoneBox2 = new TextBox { Text = machine.OwnerPhone2, Header = "Celular/Telefone" };

            panel.Children.Add(machineModelBox);
            panel.Children.Add(ownerNameBox);
            panel.Children.Add(ownerPhoneBox);
            panel.Children.Add(ownerPhoneBox2);
            dialog.Content = panel;

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                DataAcess.UpdateMachine(
                    currentInternalId,
                    machineModelBox.Text,
                    ownerNameBox.Text,
                    ownerPhoneBox.Text,
                    ownerPhoneBox2.Text
                );
                LoadMachineProfile(currentInternalId);
            }
        }

        private async void EditRepairButton_Click(int repairId, string description, double price)
        {
            var dialog = new ContentDialog
            {
                Title = "Editar Reparo",
                PrimaryButtonText = "Salvar",
                SecondaryButtonText = "Cancelar",
                XamlRoot = this.Content.XamlRoot
            };

            var panel = new StackPanel { Spacing = 8 };

            var descriptionBox = new TextBox { Text = description, Header = "Descri��o do Reparo" };
            var priceBox = new NumberBox { Value = price, Header = "Pre�o" };
            var doneCheckBox = new CheckBox { Content = "Conclu�do", IsChecked = false };

            panel.Children.Add(descriptionBox);
            panel.Children.Add(priceBox);
            panel.Children.Add(doneCheckBox);
            dialog.Content = panel;

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                DataAcess.UpdateRepair(repairId, descriptionBox.Text, (bool)doneCheckBox.IsChecked);
                LoadRepairHistory(currentInternalId);
            }
        }

        private async void NewRepairButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ContentDialog
            {
                Title = "Adicionar Novo Reparo",
                PrimaryButtonText = "Salvar",
                SecondaryButtonText = "Cancelar",
                XamlRoot = this.Content.XamlRoot
            };

            var panel = new StackPanel { Spacing = 8 };

            var descriptionBox = new TextBox { PlaceholderText = "Descri��o do Reparo" };
            var priceBox = new NumberBox { PlaceholderText = "Pre�o"};

            panel.Children.Add(descriptionBox);
            panel.Children.Add(priceBox);
            dialog.Content = panel;

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                DataAcess.InsertRepair(currentInternalId, descriptionBox.Text, priceBox.Value, DateTime.Now.ToString("dd/MM/yyyy"));
                LoadRepairHistory(currentInternalId);
            }
        }

        private async void PrintProfileButton_Click(object sender, RoutedEventArgs e)
        {
            var repairs = DataAcess.GetRepairsFromMachine(currentInternalId);
            Repair? selectedRepair = null;

            if (repairs.Count > 0)
            {
                var repairDialog = new ContentDialog
                {
                    Title = "Selecione um Reparo para Imprimir",
                    PrimaryButtonText = "Selecionar",
                    SecondaryButtonText = "Cancelar",
                    XamlRoot = this.Content.XamlRoot
                };

                var repairComboBox = new ComboBox { ItemsSource = repairs, DisplayMemberPath = "Description" };
                repairDialog.Content = repairComboBox;

                var result = await repairDialog.ShowAsync();
                if (result == ContentDialogResult.Primary && repairComboBox.SelectedItem is Repair repair)
                {
                    selectedRepair = repair;
                }
            }

            printContent = GeneratePrintPage(selectedRepair);

            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(MainWindow.Instance);
            var printManager = PrintManagerInterop.GetForWindow(hWnd);
            printManager.PrintTaskRequested += PrintManager_PrintTaskRequested;
            await PrintManagerInterop.ShowPrintUIForWindowAsync(hWnd);
            printManager.PrintTaskRequested -= PrintManager_PrintTaskRequested;
        }

        private void PrintManager_PrintTaskRequested(PrintManager sender, PrintTaskRequestedEventArgs args)
        {
            PrintTask printTask = args.Request.CreatePrintTask("Impress�o Perfil da M�quina", (PrintTaskSourceRequestedArgs taskArgs) =>
            {
                taskArgs.SetSource(printDocSource);
            });

            printTask.Completed += PrintTaskCompleted;
        }

        private void PrintDocument_Paginate(object sender, PaginateEventArgs e)
        {
            printDoc.SetPreviewPageCount(1, PreviewPageCountType.Final);
        }

        private void PrintDocument_AddPages(object sender, AddPagesEventArgs e)
        {
            printDoc.AddPage(printContent);
            printDoc.AddPagesComplete();
        }

        private StackPanel GeneratePrintPage(Repair? repair)
        {
            var panel = new StackPanel { Margin = new Thickness(20) };

            panel.Children.Add(new TextBlock
            {
                Text = "Relat�rio de M�quina",
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center,
                Foreground = new SolidColorBrush(Colors.Black),
                Margin = new Thickness(0, 0, 0, 10)
            });

            panel.Children.Add(new TextBlock { Text = $"Modelo: {machineHeader.Text}", FontSize = 18, Foreground = new SolidColorBrush(Colors.Black) });
            panel.Children.Add(new TextBlock { Text = $"ID: {currentInternalId}", FontSize = 18, Foreground = new SolidColorBrush(Colors.Black) });

            panel.Children.Add(new TextBlock
            {
                Text = "Informa��es do Propriet�rio",
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 10, 0, 5),
                Foreground = new SolidColorBrush(Colors.Black)
            });

            panel.Children.Add(new TextBlock
            {
                Text = $"{machineInfo.Text}",
                FontSize = 16,
                Foreground = new SolidColorBrush(Colors.Black)
            });

            panel.Children.Add(new TextBlock
            {
                Text = "Informa��es para Contato",
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 10, 0, 5),
                Foreground = new SolidColorBrush(Colors.Black)
            });

            panel.Children.Add(new TextBlock { Text = $"Celular: {localSettings.Values["CellPhone"] as string ?? "N�o definido"}", FontSize = 16, Foreground = new SolidColorBrush(Colors.Black) });
            panel.Children.Add(new TextBlock { Text = $"Telefone: {localSettings.Values["Phone"] as string ?? "N�o definido"}", FontSize = 16, Foreground = new SolidColorBrush(Colors.Black) });

            if (repair != null)
            {
                panel.Children.Add(new TextBlock
                {
                    Text = "Informa��es Sobre o Reparo",
                    FontSize = 20,
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(0, 10, 0, 5),
                    Foreground = new SolidColorBrush(Colors.Black)
                });

                panel.Children.Add(new TextBlock { Text = $"Data: {repair.Date}", FontSize = 16, Foreground = new SolidColorBrush(Colors.Black) });
                panel.Children.Add(new TextBlock { Text = $"Descri��o: {repair.Description}", FontSize = 16, Foreground = new SolidColorBrush(Colors.Black) });
                panel.Children.Add(new TextBlock { Text = $"Pre�o: R${repair.Price:F2}", FontSize = 16, Foreground = new SolidColorBrush(Colors.Black) });
                panel.Children.Add(new TextBlock { Text = $"Conclu�do: {(repair.Done ? "Sim" : "N�o")}", FontSize = 16, Foreground = new SolidColorBrush(Colors.Black) });
                panel.Children.Add(new TextBlock { Text = $"Prazo de Entrega: {localSettings.Values["BudgetDeadline"] as string ?? "N�o definido"}", FontSize = 16, Foreground = new SolidColorBrush(Colors.Black) });
            }

            return panel;
        }

        private void PrintTaskCompleted(PrintTask sender, PrintTaskCompletedEventArgs args)
        {
            if (args.Completion == PrintTaskCompletion.Failed)
            {
                this.DispatcherQueue.TryEnqueue(async () =>
                {
                    ContentDialog noPrintingDialog = new()
                    {
                        XamlRoot = this.Content.XamlRoot,
                        Title = "Erro na Impress�o",
                        Content = "N�o foi poss�vel imprimir.",
                        PrimaryButtonText = "OK"
                    };
                    await noPrintingDialog.ShowAsync();
                });
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