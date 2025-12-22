using Mentornote.Desktop.Helpers;
using Mentornote.Desktop.Models;
using Mentornote.Desktop.Services;
using Mentornote.Desktop.Windows;
using System.Windows;
using System.Windows.Controls;


namespace Mentornote.Desktop.Pages
{
    public partial class ProfilePage : Page
    {
        public ProfilePage()
        {
            InitializeComponent();
            LoadUser();
        }

        private void LoadUser()
        {
            TokenResponse token = AuthManager.LoadTokens();
            var user = JwtHelper.DecodeToken(token.AccessToken); 

            FullNameText.Text = $"{user.FirstName} {user.LastName}";
            EmailText.Text = user.Email;
            CreatedAtText.Text = user.CreatedAt.ToString();
            UserIdText.Text = user.UserId.ToString();
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            UserSession.Clear();
            AuthManager.Logout();

            // take user back to AuthWindow
            new AuthWindow().Show();

            // close the MainWindow
            Window.GetWindow(this).Close();
        }

        private void ChangePassword_Click(object sender, RoutedEventArgs e)
        {
            var window = new ChangePasswordWindow();
            window.ShowDialog();
        }

    }
}
