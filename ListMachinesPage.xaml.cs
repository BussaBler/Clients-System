using System;
using System.Collections.Generic;
using System.Linq;
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
                var machinePanel = new StackPanel
                {
                    Margin = new Thickness(10),
                    Padding = new Thickness(10),
                    CornerRadius = new CornerRadius(8),
                    Background = new SolidColorBrush((Color)App.Current.Resources["SystemBaseMediumColor"]),
                };

                var machineHeader = new TextBlock
                {
                    Text = $"{machine.MachineModel} (ID: {machine.MachineId})",
                    FontSize = 20,
                    FontWeight = Microsoft.UI.Text.FontWeights.Bold
                };
                machinePanel.Children.Add(machineHeader);

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
                machinePanel.Children.Add(detailsText);

                machineListPanel.Children.Add(machinePanel);
            }
        }
    }
}