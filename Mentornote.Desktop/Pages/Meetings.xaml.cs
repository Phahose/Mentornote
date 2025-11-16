using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.Drawing.Diagrams;
using Mentornote.Backend.Models;
using Mentornote.Backend.Services;
using Microsoft.Extensions.DependencyInjection;
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
        /*public ObservableCollection<MeetingItem> Upcoming { get; } = new();*/
        public ObservableCollection<Appointment> Upcoming { get; } = new();
        public ObservableCollection<Appointment> Appointments { get; } = new();
        public ObservableCollection<Appointment> Past { get; } = new();
        public DBServices dBServices = new();
        public int UserId = 1; // Placeholder for current user ID

        public Meetings()
        {
            InitializeComponent();
            
            // Make this page its own DataContext so bindings can see properties above
            this.DataContext = this;
            var appointments = GetAppointments();
            if (appointments == null || !appointments.Any())
            {
                NoAppointmentsPanel.Visibility = Visibility.Visible;
                UpcomingAppointmentsList.Visibility = Visibility.Collapsed;
                PastAppointmentsList.Visibility = Visibility.Collapsed;
            }
            else
            {
                NoAppointmentsPanel.Visibility = Visibility.Collapsed;
                UpcomingAppointmentsList.Visibility = Visibility.Visible; 
                PastAppointmentsList.Visibility = Visibility.Visible; 
               
                foreach (var appointment in appointments)
                {
                    if (appointment.StartTime >= DateTime.UtcNow)
                    {
                        Upcoming.Add(new Appointment
                        {
                            Id = appointment.Id,
                            Title = appointment.Title,
                            Description = appointment.Description,
                            StartTime = appointment.StartTime,
                            EndTime = appointment.EndTime,
                            Status = appointment.Status,
                        });
                    }
                    else
                    {
                        Past.Add(new Appointment
                        {
                            Id = appointment.Id,
                            Title = appointment.Title,
                            Description = appointment.Description,
                            StartTime = appointment.StartTime,
                            EndTime = appointment.EndTime,
                            Status = appointment.Status,
                        });
                    }

                    Appointments.Add(appointment);
                }
            }
           

        }

        private void OpenOverlay_Click(object sender, RoutedEventArgs e)
        {
            var button = (System.Windows.Controls.Button)sender;
            Console.WriteLine(button.Tag);

            if (button?.Tag is int meetingId)
            {
                var overlay = new Overlay(meetingId);
                overlay.Show();
            }

            
        }
        private void LoadMore_Click(object sender, RoutedEventArgs e)
        {
            var button = (System.Windows.Controls.Button)sender;
            bool? pageOpen = null;
            if (button?.Tag is int meetingId)
            {
                var meetingWindow = new AppointmentWindow(meetingId);
                pageOpen = meetingWindow.ShowDialog();
            }
            if (button?.Tag is "0")
            {
                var meetingWindow = new AppointmentWindow(0);
                pageOpen = meetingWindow.ShowDialog();
            }

            if (pageOpen == true)
            {

            }
        }
        private void DeleteAppointment_Click(object sender, RoutedEventArgs e)
        {
            var button = (System.Windows.Controls.Button)sender;
            var result = System.Windows.MessageBox.Show(
                    $"Delete '{button.Tag}' appointment Are you sure",
                    "Confirm Remove",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                 
            }

        }


        private List<Appointment> GetAppointments()
        {
            var appointments = dBServices.GetAppointmentsByUserId(UserId);
            Appointments.Clear();
          
            return appointments;
        }
    }

}
