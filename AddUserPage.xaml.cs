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
            if (string.IsNullOrEmpty(phoneBox.Text.Replace("(", "").Replace(")", "").Replace("-", "").Replace("_", "").Trim()))
            {
                phoneBox.Focus(FocusState.Keyboard);
                return;
            }

            DataAcess.InsertUser(
                idNumber,
                firstNameBox.Text,
                lastNameBox.Text,
                emailBox.Text,
                phoneBox.Text.Replace("(", "").Replace(")", "").Replace("-", "").Replace("_", "").Trim(),
                cepBox.Text.Replace("-", "").Trim(),
                string.Join(",", [streetBox.Text, adressNumber.Text, neighborhoodBox.Text, cityBox.Text])
            );

            string idToLoad = string.IsNullOrEmpty(idNumber.Replace("_", "").Replace("-", "")) ? phoneBox.Text.Replace("(", "").Replace(")", "").Replace("-", "").Replace("_", "").Trim() : idNumber;
            MainPage.Current?.ContentFrame.Navigate(typeof(UserProfilePage), idToLoad);
        }

        private void RadioButtons_SelectionChanged(object sender, SelectionChangedEventArgs e)
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
