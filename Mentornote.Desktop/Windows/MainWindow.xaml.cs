using System.Windows;
using Mentornote.Desktop.Pages;    
using System.Windows.Navigation;


namespace Mentornote.Desktop
{
    public partial class MainWindow : Window
    {

        public MainWindow()
        {
            InitializeComponent();
            // Navigate to the default page on startup (Meetings)
            MainFrame.Navigate(new Meetings());
            HeaderTitle.Text = "Mentornote";
        }

        // When 'Meetings' is clicked, navigate the Frame to MeetingsPage
        private void MeetingsButton_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new Meetings());
        }

        private void ProfileButton_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new ProfilePage());
        }
        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new SettingsPage());
        }


        private void MainFrame_Navigated(object sender, NavigationEventArgs e)
        {

        }
    }
}