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

            string ownerPhone = string.Empty;
            string ownerPhone2 = string.Empty;

            if (ownerPhoneTypeRadioButtons.SelectedIndex == 0)
            {
                ownerPhone = ownerCellphoneBox.Text.Trim();
            } 
            else
            {
                ownerPhone = ownerTelephoneBox.Text.Trim();
            }

            if (ownerPhoneTypeRadioButtons2.SelectedIndex == 0)
            {
                ownerPhone2 = ownerCellphoneBox2.Text.Trim();
            }
            else
            {
                ownerPhone2 = ownerTelephoneBox2.Text.Trim();
            }

            DataAcess.InsertMachine(
                machineIdBox.Text.Trim(),
                machineModelBox.Text.Trim(),
                ownerNameBox.Text.Trim(),
                ownerPhone,
                ownerPhone2
            );

            MainPage.Current?.ContentFrame.Navigate(typeof(MachineProfilePage), machineIdBox.Text.Trim());
            ClearFields();
        }

        private void ClearFields()
        {
            machineIdBox.Text = string.Empty;
            machineModelBox.Text = string.Empty;
            ownerNameBox.Text = string.Empty;
            ownerCellphoneBox.Text = string.Empty;
            ownerTelephoneBox.Text = string.Empty;
            ownerCellphoneBox2.Text = string.Empty;
            ownerTelephoneBox2.Text = string.Empty;
        }

        private void OwnerPhoneTypeRadioButtons_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var radioButtons = sender as RadioButtons;
            if (radioButtons?.Name == "ownerPhoneTypeRadioButtons")
            {
                if (radioButtons.SelectedIndex == 0)
                {
                    ownerCellphoneBox.Visibility = Visibility.Visible;
                    ownerTelephoneBox.Visibility = Visibility.Collapsed;
                    ownerTelephoneBox.Text = string.Empty;
                }
                else if (radioButtons.SelectedIndex == 1)
                {
                    ownerCellphoneBox.Visibility = Visibility.Collapsed;
                    ownerCellphoneBox.Text = string.Empty;
                    ownerTelephoneBox.Visibility = Visibility.Visible;
                }
            }
            else
            {
                if (radioButtons?.SelectedIndex == 0)
                {
                    ownerCellphoneBox2.Visibility = Visibility.Visible;
                    ownerTelephoneBox2.Visibility = Visibility.Collapsed;
                    ownerTelephoneBox2.Text = string.Empty;
                }
                else if (radioButtons?.SelectedIndex == 1)
                {
                    ownerCellphoneBox2.Visibility = Visibility.Collapsed;
                    ownerCellphoneBox2.Text = string.Empty;
                    ownerTelephoneBox2.Visibility = Visibility.Visible;
                }
            }
        }
    }
}
