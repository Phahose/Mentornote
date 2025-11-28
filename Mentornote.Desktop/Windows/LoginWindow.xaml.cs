using System.Net.Http;
using System.Net.Http.Json;
using System.Windows;
using System.Windows.Controls;
using Mentornote.Backend.DTO;
using Mentornote.Desktop.Services;

using static System.Net.WebRequestMethods;

namespace Mentornote.Desktop.Windows
{
    public partial class LoginWindow : Window
    {
        HttpClient _http = new HttpClient();
        
        public LoginWindow()
        {
            InitializeComponent();
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            ErrorMessage.Visibility = Visibility.Collapsed;

            var email = EmailBox.Text.Trim();
            var password = PasswordBox.Password.Trim();

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ErrorMessage.Text = "Please enter your email and password.";
                ErrorMessage.Visibility = Visibility.Visible;
                return;
            }

            try
            {
                var loginRequest = new { Email = email, Password = password };
                var response = await _http.PostAsJsonAsync("auth/login", loginRequest);

                if (!response.IsSuccessStatusCode)
                {
                    ErrorMessage.Text = "Invalid email or password.";
                    ErrorMessage.Visibility = Visibility.Visible;
                    return;
                }

                var result = await response.Content.ReadFromJsonAsync<LoginResponseDTO>();
                AuthManager.SaveToken(result.Token);

                new MainWindow().Show();
                this.Close();
            }
            catch (Exception ex)
            {
                ErrorMessage.Text = $"Error: {ex.Message}";
                ErrorMessage.Visibility = Visibility.Visible;
            }
        }

    }
}
