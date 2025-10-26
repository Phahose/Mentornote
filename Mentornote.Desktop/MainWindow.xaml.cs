using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Mentornote.Desktop
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            // Navigate to the default page on startup (Meetings)
            MainFrame.Navigate(new Pages.Meetings());
            HeaderTitle.Text = "Meetings";
        }

        // When 'Meetings' is clicked, navigate the Frame to MeetingsPage
        private void MeetingsButton_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new Pages.Meetings());
            HeaderTitle.Text = "Meetings";
        }

        // When 'Study' is clicked, navigate the Frame to StudyPage
        private void StudyButton_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new Pages.Study());
            HeaderTitle.Text = "Study";
            
        }

        private void MainFrame_Navigated(object sender, NavigationEventArgs e)
        {

        }
    }
}