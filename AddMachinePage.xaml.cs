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
    public sealed partial class AddMachinePage : Page
    {
        public AddMachinePage()
        {
            this.InitializeComponent();
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(machineIdBox.Text))
            {
                machineIdBox.Focus(FocusState.Programmatic);
                return;
            }

            if (string.IsNullOrWhiteSpace(machineModelBox.Text))
            {
                machineModelBox.Focus(FocusState.Programmatic);
                return;
            }

            DataAcess.InsertMachine(
                machineIdBox.Text.Trim(),
                machineModelBox.Text.Trim(),
                ownerNameBox.Text.Trim(),
                ownerPhoneBox.Text.Trim(),
                ownerPhoneBox2.Text.Trim()
            );

            MainPage.Current?.ContentFrame.Navigate(typeof(MachineProfilePage), machineIdBox.Text.Trim());
            ClearFields();
        }

        private void ClearFields()
        {
            machineIdBox.Text = string.Empty;
            machineModelBox.Text = string.Empty;
            ownerNameBox.Text = string.Empty;
            ownerPhoneBox.Text = string.Empty;
        }
    }
}
