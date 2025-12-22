using Mentornote.Desktop.Services;
using System.Net.Http.Json;
using System.Windows;


namespace Mentornote.Desktop.Windows
{
    /// <summary>
    /// Interaction logic for ChangePasswordWindow.xaml
    /// </summary>
    public partial class ChangePasswordWindow : Window
    {
        public ChangePasswordWindow()
        {
            InitializeComponent();
        }

        private async void ChangePassword_Click(object sender, RoutedEventArgs e)
        {
            if (NewPasswordBox.Password != ConfirmPasswordBox.Password)
            {
                System.Windows.MessageBox.Show("Passwords do not match.");
                return;
            }

            var request = new
            {
                CurrentPassword = CurrentPasswordBox.Password,
                NewPassword = NewPasswordBox.Password
            };

            var response = await ApiClient.Client.PostAsJsonAsync("auth/change-password", request);

            if (!response.IsSuccessStatusCode)
            {
                System.Windows.MessageBox.Show(
                    await response.Content.ReadAsStringAsync(),
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
                return;
            }

            System.Windows.MessageBox.Show(
                "Password changed successfully. Please log in again.",
                "Success",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );

            AuthManager.Logout();

            var login = new AuthWindow();
            login.Show();

            Owner?.Close();
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
