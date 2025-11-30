#nullable disable
using Mentornote.Desktop.Services;
using Mentornote.Desktop.Windows;
using NuGet.Common;
using System.Windows;
using System.Windows.Forms;

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
                string token = AuthManager.LoadToken();

                ApiClient.SetToken(token);
                UserSession.SetUser(token);
                      


                new MainWindow().Show();
            }
            else
            {
                new AuthWindow().Show();
            }
        }
    }

}
