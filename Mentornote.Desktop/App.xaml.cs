#nullable disable
using Mentornote.Desktop.Services;
using Mentornote.Desktop.Windows;
using System.Windows;

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
                // Token exists → go straight to main dashboard
                new MainWindow().Show();
            }
            else
            {
                // No token → show login/signup flow
                new AuthWindow().Show();
            }
        }
    }

}
