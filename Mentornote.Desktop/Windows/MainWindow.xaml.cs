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
            HeaderTitle.Text = "Meetings";
        }

        // When 'Meetings' is clicked, navigate the Frame to MeetingsPage
        private void MeetingsButton_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new Meetings());
            HeaderTitle.Text = "Meetings";
        }

        // When 'Study' is clicked, navigate the Frame to StudyPage
        private void StudyButton_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new Study());
            HeaderTitle.Text = "Study";
            
        }

        private void ProfileButton_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new ProfilePage());
            HeaderTitle.Text = "Profile";
        }


        private void MainFrame_Navigated(object sender, NavigationEventArgs e)
        {

        }
    }
}