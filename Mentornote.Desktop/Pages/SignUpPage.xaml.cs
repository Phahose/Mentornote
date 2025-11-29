using Mentornote.Desktop.Windows;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;


namespace Mentornote.Desktop.Pages
{
    public partial class SignUpPage : Page
    {
        private readonly AuthWindow _parent;
        private readonly HttpClient _http = new HttpClient { BaseAddress = new Uri("http://localhost:5085/api/") };


        public SignUpPage(AuthWindow parent)
        {
            InitializeComponent();
            _parent = parent;
        }

        

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            UpdatePasswordStrength(PasswordBox.Password);
        }

        private void UpdatePasswordStrength(string password)
        {
            int score = 0;

            if (password.Length >= 8) score++;
            if (Regex.IsMatch(password, @"\d")) score++;                      // Has number
            if (Regex.IsMatch(password, "[a-z]") && Regex.IsMatch(password, "[A-Z]")) score++;
            if (Regex.IsMatch(password, @"[!@#$%^&*(),.?""{}|<>]")) score++; // Has symbol

            PasswordStrengthBar.Value = score;

            switch (score)
            {
                case 0:
                case 1:
                    PasswordStrengthBar.Foreground = new SolidColorBrush(Colors.Red);
                    PasswordStrengthText.Text = "Weak Password";
                    PasswordStrengthText.Foreground = new SolidColorBrush(Colors.Red);
                    break;

                case 2:
                    PasswordStrengthBar.Foreground = new SolidColorBrush(Colors.Orange);
                    PasswordStrengthText.Text = "Medium Strength";
                    PasswordStrengthText.Foreground = new SolidColorBrush(Colors.Orange);
                    break;

                case 3:
                case 4:
                    PasswordStrengthBar.Foreground = new SolidColorBrush(Colors.Green);
                    PasswordStrengthText.Text = "Strong Password";
                    PasswordStrengthText.Foreground = new SolidColorBrush(Colors.Green);
                    break;
            }
        }

        private async void SignUp_Click(object sender, RoutedEventArgs e)
        {
            ErrorMessage.Visibility = Visibility.Collapsed;

            // VALIDATION
            if (string.IsNullOrWhiteSpace(FirstNameBox.Text) ||
                string.IsNullOrWhiteSpace(LastNameBox.Text) ||
                string.IsNullOrWhiteSpace(EmailBox.Text) ||
                string.IsNullOrWhiteSpace(PasswordBox.Password) ||
                string.IsNullOrWhiteSpace(ConfirmPasswordBox.Password))
            {
                ShowError("All fields are required.");
                return;
            }

            if (!Regex.IsMatch(EmailBox.Text, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                ShowError("Please enter a valid email.");
                return;
            }

            if (PasswordBox.Password != ConfirmPasswordBox.Password)
            {
                ShowError("Passwords do not match.");
                return;
            }

            if (PasswordStrengthBar.Value < 2)
            {
                ShowError("Password is too weak.");
                return;
            }

            // SEND REGISTER REQUEST
            var user = new
            {
                FirstName = FirstNameBox.Text,
                LastName = LastNameBox.Text,
                Email = EmailBox.Text,
                Password = PasswordBox.Password,
                UserType = "Free"
            };

            try
            {
                var response = await _http.PostAsJsonAsync("http://localhost:5085/api/auth/register", user);

                if (!response.IsSuccessStatusCode)
                {
                    ShowError(await response.Content.ReadAsStringAsync());
                    return;
                }

                 System.Windows.Forms.MessageBox.Show("Account created! Please log in.");
                _parent.NavigateToLogin();
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        private void GoToLogin_Click(object sender, RoutedEventArgs e)
        {
            _parent.NavigateToLogin();
        }

        private void ShowError(string msg)
        {
            ErrorMessage.Text = msg;
            ErrorMessage.Visibility = Visibility.Visible;
        }
    }
}
