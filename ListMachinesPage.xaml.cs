using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace Client_System_C_
{
    public sealed partial class ListMachinesPage : Page
    {
        public ListMachinesPage()
        {
            this.InitializeComponent();
            LoadMachines();
        }

        private void LoadMachines()
        {
            machineListPanel.Children.Clear();
            var machines = DataAcess.GetAllMachines();

            if (machines.Count == 0)
            {
                machineListPanel.Children.Add(new TextBlock
                {
                    Text = "Nenhuma máquina cadastrada.",
                    FontSize = 18,
                    TextAlignment = TextAlignment.Center,
                    Margin = new Thickness(10)
                });
                return;
            }

            var sortedMachines = machines.OrderBy(m => m.MachineModel);

            foreach (var machine in sortedMachines)
            {
                var machineButton = new Button
                {
                    Margin = new Thickness(10),
                    Padding = new Thickness(0),
                    Background = new SolidColorBrush((Color)App.Current.Resources["SystemBaseMediumColor"]),
                    BorderThickness = new Thickness(0),
                    CornerRadius = new CornerRadius(8),
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    HorizontalContentAlignment = HorizontalAlignment.Left,
                    Tag = machine
                };
                machineButton.Click += MachineButton_Clicked;

                var contentPanel = new StackPanel
                {
                    Margin = new Thickness(10)
                };

                var machineHeader = new TextBlock
                {
                    Text = $"{machine.MachineModel} (ID: {machine.MachineId})",
                    FontSize = 20,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Colors.Black),
                };
                contentPanel.Children.Add(machineHeader);

                var machineDetails = new List<string>
                {
                    $"Proprietário: {machine.OwnerName}",
                    $"Celular/Telefone: {machine.OwnerPhone}",
                    $"Celular/Telefone: {machine.OwnerPhone2}"
                };

                var detailsText = new TextBlock
                {
                    Text = string.Join("\n", machineDetails),
                    FontSize = 16,
                    Margin = new Thickness(0, 5, 0, 0)
                };
                contentPanel.Children.Add(detailsText);

                machineButton.Content = contentPanel;
                machineListPanel.Children.Add(machineButton);
            }
        }

        private void MachineButton_Clicked(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag is DataAcess.Machine machine)
            {
                Frame.Navigate(typeof(MachineProfilePage), machine.MachineId);
            }
        }
    }
}