using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
        private enum PrintType
        {
            MachineProfile,
            RepairReceipt
        }
        private enum PaymentMethod
        {
            Dinheiro,
            Credito,
            Debito,
            Pix,
            Outro
        }
        private int currentInternalId;
        private PrintType currentPrintType;
        private PaymentMethod currentPaymentMethod;
        private PrintDocument? printDoc;
        private IPrintDocumentSource? printDocSource;
        private readonly ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
        private readonly List<UIElement> printPages = [];
        private List<Repair> repairsToPrint = [];
        private readonly List<Repair> receiptsToPrint = [];

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
            if (e.PageNumber >= 1 && e.PageNumber <= printPages.Count)
            {
                printDoc?.SetPreviewPage(e.PageNumber, printPages[e.PageNumber - 1]);
            }
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
                machineInfoText.Add($"Proprietário: {machine.OwnerName}");
            if (!string.IsNullOrWhiteSpace(machine.OwnerPhone))
                machineInfoText.Add($"Celular/Telefone: {machine.OwnerPhone}");
            if (!string.IsNullOrWhiteSpace(machine.OwnerPhone2))
                machineInfoText.Add($"Celular/Telefone: {machine.OwnerPhone2}");

            machineInfo.Text = machineInfoText.Count > 0 ? string.Join("  |  ", machineInfoText) : "Sem informações adicionais";

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
                machineInfoText.Add($"Proprietário: {machine.OwnerName}");
            if (!string.IsNullOrWhiteSpace(machine.OwnerPhone))
                machineInfoText.Add($"Celular/Telefone: {machine.OwnerPhone}");
            if (!string.IsNullOrWhiteSpace(machine.OwnerPhone2))
                machineInfoText.Add($"Celular/Telefone: {machine.OwnerPhone2}");

            machineInfo.Text = machineInfoText.Count > 0 ? string.Join("  |  ", machineInfoText) : "Sem informações adicionais";

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

                string done = repair.Done ? "Sim" : "Não";
                var repairHeader = new TextBlock
                {
                    Text = $"Reparo em {repair.Date}, Custo: R${repair.Price:F2}, Concluído: {done}",
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
                editButton.Click += (s, e) => EditRepairButton_Click(repair.RepairId, repair.Description, repair.Price, repair.ServiceOrder);

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
                Title = "Editar Máquina",
                PrimaryButtonText = "Salvar",
                SecondaryButtonText = "Cancelar",
                XamlRoot = this.Content.XamlRoot
            };

            var panel = new StackPanel { Spacing = 8 };

            var machineModelBox = new TextBox { Text = machine.MachineModel, Header = "Modelo da Máquina" };
            var ownerNameBox = new TextBox { Text = machine.OwnerName, Header = "Proprietário" };
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

        private async void EditRepairButton_Click(int repairId, string description, double price, string serviceOrder)
        {
            var dialog = new ContentDialog
            {
                Title = "Editar Reparo",
                PrimaryButtonText = "Salvar",
                SecondaryButtonText = "Cancelar",
                XamlRoot = this.Content.XamlRoot
            };

            var panel = new StackPanel { Spacing = 8 };

            var descriptionBox = new TextBox { Text = description, Header = "Descrição do Reparo" };
            var priceBox = new NumberBox { Value = price, Header = "Preço" };
            var serviceOrderBox = new TextBox { Text = serviceOrder, Header = "Ordem de Serviço (opcional)" };
            var doneCheckBox = new CheckBox { Content = "Concluído", IsChecked = false };

            panel.Children.Add(descriptionBox);
            panel.Children.Add(priceBox);
            panel.Children.Add(serviceOrderBox);
            panel.Children.Add(doneCheckBox);
            dialog.Content = panel;

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                DataAcess.UpdateRepair(repairId, descriptionBox.Text, serviceOrderBox.Text.Trim(), (bool)doneCheckBox.IsChecked);
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

            var descriptionBox = new TextBox
            {
                PlaceholderText = "Descrição do Reparo",
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                Height = 100
            };
            var priceBox = new NumberBox { PlaceholderText = "Preço" };
            var serviceOrderBox = new TextBox { PlaceholderText = "Ordem de Serviço (opcional)" };

            panel.Children.Add(descriptionBox);
            panel.Children.Add(priceBox);
            panel.Children.Add(serviceOrderBox);
            dialog.Content = panel;

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                DataAcess.InsertRepair(currentInternalId, descriptionBox.Text, priceBox.Value, DateTime.Now.ToString("dd/MM/yyyy"), serviceOrderBox.Text.Trim());
                LoadRepairHistory(currentInternalId);
            }
        }

        private async void PrintProfileButton_Click(object sender, RoutedEventArgs e)
        {
            var typeOfPrintDialog = new ContentDialog
            {
                Title = "Escolha o Tipo de Impressão",
                PrimaryButtonText = "Perfil da Máquina",
                SecondaryButtonText = "Recibo do Reparo",
                CloseButtonText = "Cancelar",
                XamlRoot = this.Content.XamlRoot
            };

            var type = await typeOfPrintDialog.ShowAsync();

            if (type == ContentDialogResult.Primary)
            {
                var repairs = DataAcess.GetRepairsFromMachine(currentInternalId);
                if (repairs.Count == 0) return;

                var wrap = new StackPanel { Spacing = 8, Padding = new Thickness(4) };
                wrap.Children.Add(new TextBlock
                {
                    Text = "Selecione um ou mais reparos para incluir no relatório do perfil:",
                    FontSize = 14,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 0, 0, 4)
                });

                var listView = new ListView
                {
                    ItemsSource = repairs,
                    SelectionMode = ListViewSelectionMode.Multiple,
                    IsMultiSelectCheckBoxEnabled = true,
                    Height = 320
                };

                wrap.Children.Add(listView);
                wrap.Children.Add(new TextBlock
                {
                    Text = "Dica: você pode selecionar múltiplos reparos para aparecerem na tabela.",
                    Opacity = 0.7,
                    FontSize = 12
                });

                var repairDialog = new ContentDialog
                {
                    Title = "Imprimir – Perfil da Máquina",
                    PrimaryButtonText = "Selecionar",
                    SecondaryButtonText = "Cancelar",
                    XamlRoot = this.Content.XamlRoot,
                    Content = wrap
                };

                var result = await repairDialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    var selectedRepairs = listView.SelectedItems.Cast<Repair>().ToList();
                    selectedRepairs.Sort((a, b) => a.RepairId.CompareTo(b.RepairId));
                    if (selectedRepairs.Count == 0) return;

                    repairsToPrint = selectedRepairs;
                    currentPrintType = PrintType.MachineProfile;

                    var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(MainWindow.Instance);
                    var printManager = PrintManagerInterop.GetForWindow(hWnd);
                    printManager.PrintTaskRequested += PrintManager_PrintTaskRequested;
                    await PrintManagerInterop.ShowPrintUIForWindowAsync(hWnd);
                    printManager.PrintTaskRequested -= PrintManager_PrintTaskRequested;
                }
            }

            else if (type == ContentDialogResult.Secondary)
            {
                var repairs = DataAcess.GetRepairsFromMachine(currentInternalId);
                if (repairs.Count == 0) return;

                var root = new StackPanel { Spacing = 12, Padding = new Thickness(4) };

                root.Children.Add(new TextBlock
                {
                    Text = "Selecione um reparo para imprimir o recibo:",
                    FontSize = 14,
                    TextWrapping = TextWrapping.Wrap
                });

                var listView = new ListView
                {
                    ItemsSource = repairs,
                    SelectionMode = ListViewSelectionMode.Single,
                    Height = 240
                };
                root.Children.Add(listView);

                root.Children.Add(new TextBlock
                {
                    Text = "Forma de pagamento:",
                    FontSize = 14
                });

                var paymentCombo = new ComboBox
                {
                    ItemsSource = Enum.GetValues(typeof(PaymentMethod)),
                    SelectedItem = currentPaymentMethod,
                    MinWidth = 240
                };
                root.Children.Add(paymentCombo);

                var paidCheck = new CheckBox
                {
                    Content = "Marcar como pago",
                    IsChecked = true
                };
                //root.Children.Add(paidCheck);

                var repairDialog = new ContentDialog
                {
                    Title = "Imprimir – Recibo do Reparo",
                    PrimaryButtonText = "Selecionar",
                    SecondaryButtonText = "Cancelar",
                    XamlRoot = this.Content.XamlRoot,
                    Content = root
                };

                var result = await repairDialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    if (listView.SelectedItem is not Repair selected)
                        return;

                    currentPaymentMethod = paymentCombo.SelectedItem is PaymentMethod pmSel
                        ? pmSel
                        : PaymentMethod.Dinheiro;

                    if (paidCheck.IsChecked == true)
                    {
                        //selected.Done = true;
                    }

                    receiptsToPrint.Clear();
                    receiptsToPrint.Add(selected);
                    currentPrintType = PrintType.RepairReceipt;

                    var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(MainWindow.Instance);
                    var printManager = PrintManagerInterop.GetForWindow(hWnd);
                    printManager.PrintTaskRequested += PrintManager_PrintTaskRequested;
                    await PrintManagerInterop.ShowPrintUIForWindowAsync(hWnd);
                    printManager.PrintTaskRequested -= PrintManager_PrintTaskRequested;
                }
            }
        }

        private void PrintManager_PrintTaskRequested(PrintManager sender, PrintTaskRequestedEventArgs args)
        {
            string tittle = currentPrintType switch
            {
                PrintType.MachineProfile => "Impressão Perfil da Máquina",
                PrintType.RepairReceipt => "Impressão Recibo do Reparo",
                _ => "Impressão",
            };
            PrintTask printTask = args.Request.CreatePrintTask(tittle, (PrintTaskSourceRequestedArgs taskArgs) =>
            {
                taskArgs.SetSource(printDocSource);
            });

            printTask.Completed += PrintTaskCompleted;
        }

        private void PrintDocument_Paginate(object sender, PaginateEventArgs e)
        {
            printPages.Clear();

            var desc = e.PrintTaskOptions.GetPageDescription(0);
            double availWidth = desc.ImageableRect.Width;
            double availHeight = desc.ImageableRect.Height;

            if (currentPrintType == PrintType.MachineProfile)
            {
                var probe = BuildSingleReport(repairsToPrint);
                probe.Measure(new Windows.Foundation.Size(availWidth, double.PositiveInfinity));
                var reportHeight = probe.DesiredSize.Height;

                if (reportHeight <= (availHeight / 2.0))
                {
                    var page = new Grid { RowSpacing = 24 };
                    page.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                    page.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

                    var top = new Border { Child = BuildSingleReport(repairsToPrint) };
                    Grid.SetRow(top, 0);
                    page.Children.Add(top);

                    var bottom = new Border { Child = BuildSingleReport(repairsToPrint) };
                    Grid.SetRow(bottom, 1);
                    page.Children.Add(bottom);

                    page.Measure(new Windows.Foundation.Size(availWidth, availHeight));
                    page.Arrange(new Windows.Foundation.Rect(0, 0, availWidth, availHeight));
                    printPages.Add(page);
                }
                else
                {
                    for (int i = 0; i < 2; i++)
                    {
                        var page = new Grid();
                        page.Children.Add(new Border { Child = BuildSingleReport(repairsToPrint) });
                        page.Measure(new Windows.Foundation.Size(availWidth, availHeight));
                        page.Arrange(new Windows.Foundation.Rect(0, 0, availWidth, availHeight));
                        printPages.Add(page);
                    }
                }
            }
            else 
            {
                foreach (var r in receiptsToPrint)
                {
                    var page = new Grid();
                    var content = new Border { Child = BuildSingleRecipt(r) };
                    page.Children.Add(content);
                    page.Measure(new Windows.Foundation.Size(availWidth, availHeight));
                    page.Arrange(new Windows.Foundation.Rect(0, 0, availWidth, availHeight));
                    printPages.Add(page);
                }
            }

            printDoc?.SetPreviewPageCount(printPages.Count, PreviewPageCountType.Final);
        }

        private void PrintDocument_AddPages(object sender, AddPagesEventArgs e)
        {
            foreach (var page in printPages)
            {
                printDoc?.AddPage(page);
            }
            printDoc?.AddPagesComplete();
        }

        private StackPanel BuildSingleReport(List<Repair> repairs)
        {
            var baseHigh = new SolidColorBrush(Colors.Black);
            var baseMed = new SolidColorBrush(Windows.UI.Color.FromArgb(200, 0, 0, 0));
            var accent = new SolidColorBrush((Windows.UI.Color)Application.Current.Resources["SystemAccentColor"]);
            var subtleLine = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 0, 0));

            Grid MakeKVGrid()
            {
                var g = new Grid { Margin = new Thickness(0, 8, 0, 0) };
                g.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(165) });
                g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                return g;
            }
            void AddRow(Grid g, string label, string value)
            {
                int r = g.RowDefinitions.Count - 1;
                var tbLabel = new TextBlock
                {
                    Text = label,
                    FontSize = 16,
                    Foreground = baseMed,
                    Margin = new Thickness(0, 4, 12, 4),
                    TextWrapping = TextWrapping.Wrap
                };
                var tbValue = new TextBlock
                {
                    Text = value,
                    FontSize = 16,
                    Foreground = baseHigh,
                    Margin = new Thickness(0, 4, 0, 4),
                    TextWrapping = TextWrapping.Wrap
                };
                Grid.SetRow(tbLabel, r); Grid.SetColumn(tbLabel, 0);
                Grid.SetRow(tbValue, r); Grid.SetColumn(tbValue, 1);
                g.Children.Add(tbLabel); g.Children.Add(tbValue);
                g.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            }
            string PhoneOr(string key) => (localSettings.Values[key] as string) ?? "Não definido";
            var culture = new System.Globalization.CultureInfo("pt-BR");
            string Money(decimal v) => string.Format(culture, "{0:C}", v);
            string DateFmt(object dt) => dt is System.DateTime d ? d.ToString("dd/MM/yyyy") : (dt?.ToString() ?? "Não definido");
            Grid BuildRepairTable(List<Repair> repairs)
            {
                // Grid da tabela
                var table = new Grid { Margin = new Thickness(0, 8, 0, 0) };

                // COLUNAS: ajuste se quiser
                table.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(110) });                // Data
                table.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });// Descrição
                table.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });                // Preço
                table.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(140) });               // Prazo de Entrega
                table.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(60) });                 // Ordem de Serviço
                table.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });                 // Concluído

                // Helper: adiciona célula de texto
                void AddCell(int row, int col, string text, bool header = false, TextAlignment align = TextAlignment.Left)
                {
                    var tb = new TextBlock
                    {
                        Text = text,
                        FontSize = header ? 16 : 14,
                        FontWeight = header ? FontWeights.SemiBold : FontWeights.Normal,
                        Foreground = header ? baseHigh : baseHigh,
                        TextWrapping = TextWrapping.NoWrap,
                        TextTrimming = TextTrimming.CharacterEllipsis,
                        Margin = new Thickness(8, header ? 8 : 6, 8, header ? 8 : 6),
                        HorizontalAlignment = HorizontalAlignment.Stretch,
                        TextAlignment = align
                    };
                    Grid.SetRow(tb, row);
                    Grid.SetColumn(tb, col);
                    table.Children.Add(tb);
                }

                // Helper: adiciona linha (RowDefinition) e separador inferior opcional
                void AddRowDef(bool withSeparator)
                {
                    table.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                    if (withSeparator)
                    {
                        var sep = new Border
                        {
                            Height = 1,
                            Background = subtleLine,
                            HorizontalAlignment = HorizontalAlignment.Stretch,
                            Margin = new Thickness(0, 0, 0, 0)
                        };
                        Grid.SetRow(sep, table.RowDefinitions.Count - 1);
                        Grid.SetColumnSpan(sep, table.ColumnDefinitions.Count);
                        // Dica: coloque o separador ANTES das células para ficar “atrás”
                        table.Children.Add(sep);
                    }
                }

                // CABEÇALHO
                AddRowDef(withSeparator: false); // linha 0 – header
                                                 // Fundo do header
                var headerBg = new Border
                {
                    Background = new SolidColorBrush(Windows.UI.Color.FromArgb(16, 0, 0, 0)),
                    CornerRadius = new CornerRadius(6),
                    Margin = new Thickness(0, 0, 0, 0)
                };
                Grid.SetRow(headerBg, 0);
                Grid.SetColumnSpan(headerBg, table.ColumnDefinitions.Count);
                table.Children.Add(headerBg);

                AddCell(0, 0, "Data", header: true);
                AddCell(0, 1, "Descrição", header: true);
                AddCell(0, 2, "Preço", header: true);
                AddCell(0, 3, "Prazo de Entrega", header: true);
                AddCell(0, 4, "O.S.", header: true);
                AddCell(0, 5, "Concluído", header: true);

                // LINHAS
                for (int i = 0; i < repairs.Count; i++)
                {
                    Repair r = repairs[i];

                    // nova linha + separador inferior
                    AddRowDef(withSeparator: false);
                    int row = table.RowDefinitions.Count - 1;

                    // (Opcional) zebra: leve fundo em linhas pares
                    if (i % 2 == 1)
                    {
                        var zebra = new Border
                        {
                            Background = new SolidColorBrush(Windows.UI.Color.FromArgb(8, 0, 0, 0)),
                            CornerRadius = new CornerRadius(4),
                            Margin = new Thickness(0, 0, 0, 0)
                        };
                        Grid.SetRow(zebra, row);
                        Grid.SetColumnSpan(zebra, table.ColumnDefinitions.Count);
                        table.Children.Insert(0, zebra); // inserir atrás das células
                    }

                    AddCell(row, 0, DateFmt(r.Date));

                    // Descrição: permite wrap, mas limita altura para não estourar
                    var descTb = new TextBlock
                    {
                        Text = string.IsNullOrWhiteSpace(r?.Description) ? "—" : r.Description,
                        FontSize = 14,
                        Foreground = baseHigh,
                        TextWrapping = TextWrapping.Wrap,
                        TextTrimming = TextTrimming.WordEllipsis,
                        //MaxLines = 2,
                        Margin = new Thickness(8, 6, 8, 6)
                    };
                    Grid.SetRow(descTb, row);
                    Grid.SetColumn(descTb, 1);
                    table.Children.Add(descTb);

                    AddCell(row, 2, Money(Convert.ToDecimal(r.Price)), header: false);
                    AddCell(row, 3, PhoneOr("BudgetDeadline"));
                    AddCell(row, 4, string.IsNullOrEmpty(r.ServiceOrder) ? "—" : r.ServiceOrder, header: false);
                    AddCell(row, 5, r.Done ? "Sim" : "Não", header: false);
                }

                return table;
            }

            var root = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Padding = new Thickness(24, 28, 24, 28),
                Spacing = 12
            };

            // Cabeçalho
            root.Children.Add(new TextBlock
            {
                Text = "Relatório da Máquina",
                FontSize = 22,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center,
                Foreground = baseHigh
            });

            root.Children.Add(new TextBlock
            {
                Text = "GLOMAQ",
                FontSize = 18,
                TextAlignment = TextAlignment.Center,
                Foreground = baseMed,
            });

            root.Children.Add(new TextBlock
            {
                Text = $"Celular: {PhoneOr("CellPhone")}   •   Telefone: {PhoneOr("Phone")}",
                FontSize = 14,
                TextAlignment = TextAlignment.Center,
                Foreground = baseMed
            });

            root.Children.Add(new Border { Height = 1, Background = subtleLine, Margin = new Thickness(0, 8, 0, 8) });

            var topCardsGrid = new Grid
            {
                ColumnSpacing = 16,
                RowSpacing = 0
            };
            topCardsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            topCardsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var machineCard = new Border
            {
                Background = new SolidColorBrush(Colors.White),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(16),
                BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(60, 0, 0, 0)),
                BorderThickness = new Thickness(1),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };
            var cardStack1 = new StackPanel { Spacing = 8 };
            cardStack1.Children.Add(new TextBlock
            {
                Text = "Dados da Máquina",
                FontSize = 18,
                FontWeight = FontWeights.SemiBold,
                Foreground = accent
            });
            var gridMachine = MakeKVGrid();
            AddRow(gridMachine, "Modelo", machineHeader?.Text ?? "Não definido");
            cardStack1.Children.Add(gridMachine);
            machineCard.Child = cardStack1;
            Grid.SetColumn(machineCard, 0);
            topCardsGrid.Children.Add(machineCard);

            var ownerCard = new Border
            {
                Background = new SolidColorBrush(Colors.White),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(16),
                BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(60, 0, 0, 0)),
                BorderThickness = new Thickness(1),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };
            var cardStack2 = new StackPanel { Spacing = 8 };
            cardStack2.Children.Add(new TextBlock
            {
                Text = "Informações do Proprietário",
                FontSize = 18,
                FontWeight = FontWeights.SemiBold,
                Foreground = accent
            });
            var gridOwner = MakeKVGrid();
            var infos = machineInfo?.Text?.Split("  |  ") ?? [];
            if (infos.Length >= 3)
            {
                AddRow(gridOwner, "Proprietário", infos[0].Replace("Proprietário: ", ""));
                AddRow(gridOwner, "Celular/Telefone", infos[1].Replace("Celular/Telefone: ", ""));
                AddRow(gridOwner, "Celular/Telefone 2", infos[2].Replace("Celular/Telefone: ", ""));
            }
            cardStack2.Children.Add(gridOwner);
            ownerCard.Child = cardStack2;
            Grid.SetColumn(ownerCard, 1);
            topCardsGrid.Children.Add(ownerCard);

            root.Children.Add(topCardsGrid);

            if (repairs?.Count > 0)
            {
                var repairCard = new Border
                {
                    Background = new SolidColorBrush(Colors.White),
                    CornerRadius = new CornerRadius(12),
                    Padding = new Thickness(16),
                    BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(60, 0, 0, 0)),
                    BorderThickness = new Thickness(1)
                };
                var cardStack3 = new StackPanel { Spacing = 8 };

                cardStack3.Children.Add(new TextBlock
                {
                    Text = "Informações Sobre o(s) Reparo(s)",
                    FontSize = 18,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = accent
                });

                cardStack3.Children.Add(BuildRepairTable(repairs));
                repairCard.Child = cardStack3;
                root.Children.Add(repairCard);
            }

            return root;
        }

        private StackPanel BuildSingleRecipt(Repair repair)
        {
            var baseHigh = new SolidColorBrush(Colors.Black);
            var baseMed = new SolidColorBrush(Windows.UI.Color.FromArgb(200, 0, 0, 0));
            var accent = new SolidColorBrush((Windows.UI.Color)Application.Current.Resources["SystemAccentColor"]);
            var subtleLine = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 0, 0));

            Grid MakeKVGrid()
            {
                var g = new Grid { Margin = new Thickness(0, 8, 0, 0) };
                g.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(165) });
                g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                return g;
            }
            void AddRow(Grid g, string label, string value)
            {
                int r = g.RowDefinitions.Count - 1;
                var tbLabel = new TextBlock
                {
                    Text = label,
                    FontSize = 16,
                    Foreground = baseMed,
                    Margin = new Thickness(0, 4, 12, 4),
                    TextWrapping = TextWrapping.Wrap
                };
                var tbValue = new TextBlock
                {
                    Text = value,
                    FontSize = 16,
                    Foreground = baseHigh,
                    Margin = new Thickness(0, 4, 0, 4),
                    TextWrapping = TextWrapping.Wrap
                };
                Grid.SetRow(tbLabel, r); Grid.SetColumn(tbLabel, 0);
                Grid.SetRow(tbValue, r); Grid.SetColumn(tbValue, 1);
                g.Children.Add(tbLabel); g.Children.Add(tbValue);
                g.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            }
            string PhoneOr(string key) => (localSettings.Values[key] as string) ?? "Não definido";
            var culture = new System.Globalization.CultureInfo("pt-BR");
            string Money(decimal v) => string.Format(culture, "{0:C}", v);
            string DateFmt(object dt) => dt is System.DateTime d ? d.ToString("dd/MM/yyyy") : (dt?.ToString() ?? "Não definido");

            var root = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Padding = new Thickness(24, 28, 24, 28),
                Spacing = 12
            };

            // Cabeçalho
            root.Children.Add(new TextBlock
            {
                Text = "Recibo",
                FontSize = 22,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center,
                Foreground = baseHigh
            });

            root.Children.Add(new TextBlock
            {
                Text = "GLOMAQ",
                FontSize = 18,
                TextAlignment = TextAlignment.Center,
                Foreground = baseMed,
            });

            root.Children.Add(new TextBlock
            {
                Text = $"Celular: {PhoneOr("CellPhone")}   •   Telefone: {PhoneOr("Phone")}",
                FontSize = 14,
                TextAlignment = TextAlignment.Center,
                Foreground = baseMed
            });

            root.Children.Add(new Border { Height = 1, Background = subtleLine, Margin = new Thickness(0, 8, 0, 8) });

            var topCardsGrid = new Grid
            {
                ColumnSpacing = 16,
                RowSpacing = 0
            };
            topCardsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            topCardsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var machineCard = new Border
            {
                Background = new SolidColorBrush(Colors.White),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(16),
                BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(60, 0, 0, 0)),
                BorderThickness = new Thickness(1),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };
            var cardStack1 = new StackPanel { Spacing = 8 };
            cardStack1.Children.Add(new TextBlock
            {
                Text = "Dados da Máquina",
                FontSize = 18,
                FontWeight = FontWeights.SemiBold,
                Foreground = accent
            });
            var gridMachine = MakeKVGrid();
            AddRow(gridMachine, "Modelo", machineHeader?.Text ?? "Não definido");
            cardStack1.Children.Add(gridMachine);
            machineCard.Child = cardStack1;
            Grid.SetColumn(machineCard, 0);
            topCardsGrid.Children.Add(machineCard);

            var ownerCard = new Border
            {
                Background = new SolidColorBrush(Colors.White),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(16),
                BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(60, 0, 0, 0)),
                BorderThickness = new Thickness(1),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };
            var cardStack2 = new StackPanel { Spacing = 8 };
            cardStack2.Children.Add(new TextBlock
            {
                Text = "Informações do Proprietário",
                FontSize = 18,
                FontWeight = FontWeights.SemiBold,
                Foreground = accent
            });
            var gridOwner = MakeKVGrid();
            var infos = machineInfo?.Text?.Split("  |  ") ?? [];
            if (infos.Length >= 3)
            {
                AddRow(gridOwner, "Proprietário", infos[0].Replace("Proprietário: ", ""));
                AddRow(gridOwner, "Celular/Telefone", infos[1].Replace("Celular/Telefone: ", ""));
                AddRow(gridOwner, "Celular/Telefone 2", infos[2].Replace("Celular/Telefone: ", ""));
            }
            cardStack2.Children.Add(gridOwner);
            ownerCard.Child = cardStack2;
            Grid.SetColumn(ownerCard, 1);
            topCardsGrid.Children.Add(ownerCard);

            root.Children.Add(topCardsGrid);

            var repairCard = new Border
            {
                Background = new SolidColorBrush(Colors.White),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(16),
                BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(60, 0, 0, 0)),
                BorderThickness = new Thickness(1)
            };
            var cardStack3 = new StackPanel { Spacing = 8 };
            cardStack3.Children.Add(new TextBlock
            {
                Text = "Informações Sobre o Reparo",
                FontSize = 18,
                FontWeight = FontWeights.SemiBold,
                Foreground = accent
            });
            var gridRepair = MakeKVGrid();
            AddRow(gridRepair, "Descrição", string.IsNullOrWhiteSpace(repair?.Description) ? "—" : repair.Description);
            AddRow(gridRepair, "Preço", Money(Convert.ToDecimal(repair?.Price ?? 0)));
            AddRow(gridRepair, "Data do Reparo", DateFmt(repair?.Date));
            AddRow(gridRepair, "Ordem de Serviço", string.IsNullOrEmpty(repair?.ServiceOrder) ? "—" : repair.ServiceOrder);
            AddRow(gridRepair, "Concluído", (repair?.Done == true) ? "Sim" : "Não");
            cardStack3.Children.Add(gridRepair);

            repairCard.Child = cardStack3;
            root.Children.Add(repairCard);

            var paymentCard = new Border
            {
                Background = new SolidColorBrush(Colors.White),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(16),
                BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(60, 0, 0, 0)),
                BorderThickness = new Thickness(1)
            };
            var cardStack4 = new StackPanel { Spacing = 8 };
            cardStack4.Children.Add(new TextBlock
            {
                Text = "Informações de Pagamento",
                FontSize = 18,
                FontWeight = FontWeights.SemiBold,
                Foreground = accent
            });
            var gridPayment = MakeKVGrid();
            string paymentMethod = currentPaymentMethod switch
            {
                PaymentMethod.Dinheiro => "Dinheiro",
                PaymentMethod.Credito => "Cartão de Crédito",
                PaymentMethod.Debito => "Cartão de Débito",
                PaymentMethod.Pix => "Pix",
                _ => "Não definido"
            };
            AddRow(gridPayment, "Método de Pagamento", paymentMethod);

            cardStack4.Children.Add(gridPayment);
            paymentCard.Child = cardStack4;
            root.Children.Add(paymentCard);

            return root;
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
                        Title = "Erro na Impressão",
                        Content = "Não foi possível imprimir.",
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