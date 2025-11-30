using Mentornote.Backend.DTO;
using Mentornote.Desktop.Services;
using Mentornote.Desktop.Windows;
using NuGet.Common;
using System.Net.Http;
using System.Net.Http.Json;
using System.Windows;
using System.Windows.Controls;


namespace Mentornote.Desktop.Pages
{
    public partial class LoginPage : Page
    {
        HttpClient _http = new HttpClient();
        private readonly AuthWindow _parent;

        public LoginPage(AuthWindow parent)
        {
            InitializeComponent();
            _parent = parent;
        }

        private async void Login_Click(object sender, RoutedEventArgs e)
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
                LoginDTO loginRequest = new()
                { 
                    Email = email, 
                    Password = password 
                };

                var response = await _http.PostAsJsonAsync("http://localhost:5085/api/auth/login", loginRequest);

                if (!response.IsSuccessStatusCode)
                {
                    ErrorMessage.Text = "Invalid email or password.";
                    ErrorMessage.Visibility = Visibility.Visible;
                    return;
                }

                var result = await response.Content.ReadFromJsonAsync<LoginResponseDTO>();
               
                AuthManager.SaveToken(result.Token);
                UserSession.SetUser(result.Token);
                ApiClient.SetToken(result.Token);


                new MainWindow().Show();
                Window.GetWindow(this).Close();
            }
            catch (Exception ex)
            {
                ErrorMessage.Text = $"Error: {ex.Message}";
                ErrorMessage.Visibility = Visibility.Visible;
            }
        }

        private void GoToSignUp_Click(object sender, RoutedEventArgs e)
        {
            _parent.NavigateToSignup();
        }
    }
}
