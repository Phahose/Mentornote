#nullable disable
using Mentornote.Desktop.Services;
using Mentornote.Desktop.Windows;
using NuGet.Common;
using System.Windows;
using System.Windows.Forms;
using Mentornote.Desktop.Models;

namespace Mentornote.Desktop
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        public App()
        {
         
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            if (AuthManager.IsLoggedIn())
            {
                TokenResponse token = AuthManager.LoadTokens();

                ApiClient.SetToken(token.AccessToken, token.RefreshToken);
                UserSession.SetUser(token.AccessToken);                  
                new MainWindow().Show();
            }
            else
            {
                new AuthWindow().Show();
            }
        }
    }
}
