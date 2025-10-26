using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Mentornote.Desktop.Pages
{
    /// <summary>
    /// Interaction logic for Meetings.xaml
    /// </summary>
    public partial class Meetings : Page
    {
        // Expose collections the XAML can bind to
        public ObservableCollection<MeetingItem> Upcoming { get; } = new();
        public ObservableCollection<MeetingItem> Past { get; } = new();

        public Meetings()
        {
            InitializeComponent();
            // Make this page its own DataContext so bindings can see properties above
            this.DataContext = this;

            // Seed some demo items (replace later with API/DB)
            Upcoming.Add(new MeetingItem
            {
                Title = "AAIP Evaluation Sync",
                When = DateTime.Now.AddHours(2),
                Status = "Upcoming"
            });
            Upcoming.Add(new MeetingItem
            {
                Title = "Data Integration Review",
                When = DateTime.Now.AddDays(1).AddHours(3),
                Status = "Upcoming"
            });

            Past.Add(new MeetingItem
            {
                Title = "Kickoff Meeting",
                When = DateTime.Now.AddDays(-5).AddHours(1),
                Status = "Completed"
            });
            Past.Add(new MeetingItem
            {
                Title = "Dashboard Planning",
                When = DateTime.Now.AddDays(-7).AddHours(2),
                Status = "Completed"
            });
    }

        private void OpenOverlay_Click(object sender, RoutedEventArgs e)
        {
            var overlay = new Overlay();
            overlay.Show();
        }
    }
    // Simple model for display
    public class MeetingItem
    {
        public string Title { get; set; } = "";
        public DateTime When { get; set; }
        public string Status { get; set; } = "";

        // Convenience property for UI display of date/time
        public string WhenText => When.ToString("dddd, MMM d, yyyy - h:mm tt");
    }
}
