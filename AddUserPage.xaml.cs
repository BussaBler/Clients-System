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
using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Client_System_C_
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class AddUserPage : Page
    {
        private static readonly HttpClient httpClient = new HttpClient();
        public AddUserPage()
        {
            this.InitializeComponent();
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            string idNumber;
            string phoneNumber = string.Empty;

            if (idTypeRadioButtons.SelectedIndex == 0)
            {
                idNumber = cpfBox.Text.Trim();
            }
            else
            {
                idNumber = cnpjBox.Text.Trim();
            }

            if (string.IsNullOrEmpty(firstNameBox.Text))
            {
                firstNameBox.Focus(FocusState.Keyboard);
                return;
            }
            if (string.IsNullOrEmpty(lastNameBox.Text))
            {
                lastNameBox.Focus(FocusState.Keyboard);
                return;
            }
            if (phoneTypeRadioButtons.SelectedIndex == 0)
            {
                if (string.IsNullOrEmpty(cellphoneBox.Text.Replace("(", "").Replace(")", "").Replace("-", "").Replace("_", "").Trim()))
                {
                    cellphoneBox.Focus(FocusState.Keyboard);
                    return;
                }
                phoneNumber = cellphoneBox.Text.Replace("(", "").Replace(")", "").Replace("-", "").Replace("_", "").Trim();
            }
            else if (phoneTypeRadioButtons.SelectedIndex == 1)
            {
                if (string.IsNullOrEmpty(telephoneBox.Text.Replace("(", "").Replace(")", "").Replace("-", "").Replace("_", "").Trim()))
                {
                    telephoneBox.Focus(FocusState.Keyboard);
                    return;
                }
                phoneNumber = telephoneBox.Text.Replace("(", "").Replace(")", "").Replace("-", "").Replace("_", "").Trim();
            }

            DataAcess.InsertUser(
                idNumber,
                firstNameBox.Text,
                lastNameBox.Text,
                emailBox.Text,
                phoneNumber,
                cepBox.Text.Replace("-", "").Trim(),
                string.Join(",", [streetBox.Text, adressNumber.Text, neighborhoodBox.Text, cityBox.Text])
            );

            string idToLoad = string.IsNullOrEmpty(idNumber.Replace(".", "").Replace("-", "").Replace("_", "").Replace("/", "").Trim()) ? phoneNumber : idNumber;
            MainPage.Current?.ContentFrame.Navigate(typeof(UserProfilePage), idToLoad);
        }

        private void IdRadioButtons_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (idTypeRadioButtons.SelectedIndex == 0)
            {
                cpfBox.Visibility = Visibility.Visible;
                cnpjBox.Visibility = Visibility.Collapsed;
                cnpjBox.Text = string.Empty;
            }
            if (idTypeRadioButtons.SelectedIndex == 1)
            {
                cpfBox.Visibility = Visibility.Collapsed;
                cpfBox.Text = string.Empty;
                cnpjBox.Visibility = Visibility.Visible;
            }
        }

        private void PhoneRadioButtons_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (phoneTypeRadioButtons.SelectedIndex == 0)
            {
                cellphoneBox.Visibility = Visibility.Visible;
                telephoneBox.Visibility = Visibility.Collapsed;
                telephoneBox.Text = string.Empty;
            }
            if (phoneTypeRadioButtons.SelectedIndex == 1)
            {
                cellphoneBox.Visibility = Visibility.Collapsed;
                cellphoneBox.Text = string.Empty;
                telephoneBox.Visibility = Visibility.Visible;
            }
        }

        private async void CepBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string cep = cepBox.Text.Replace("-", "").Replace("_", "").Trim();

            if (cep.Length == 8)
            {
                await FetchAddressFromCep(cep);
            }
        }

        private async Task FetchAddressFromCep(string cep)
        {
            
            string url = $"https://viacep.com.br/ws/{cep}/json/";
            string response = await httpClient.GetStringAsync(url);

            var address = JsonConvert.DeserializeObject<BrazilianAddress>(response);

            if (address != null && string.IsNullOrEmpty(address.Erro))
            {
                streetBox.Text = address.Logradouro;
                neighborhoodBox.Text = address.Bairro;
                cityBox.Text = address.Localidade;
            }
            
        }

        private class BrazilianAddress
        {
            [JsonProperty("logradouro")] public string Logradouro { get; set; }
            [JsonProperty("bairro")] public string Bairro { get; set; }
            [JsonProperty("localidade")] public string Localidade { get; set; }
            [JsonProperty("erro")] public string Erro { get; set; }
        }
    }
}
