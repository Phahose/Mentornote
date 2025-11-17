using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.Drawing.Diagrams;
using Mentornote.Backend.Models;
using Mentornote.Backend.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
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
using static System.Net.WebRequestMethods;

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
        private static readonly HttpClient _http = new HttpClient()
        {
            Timeout = TimeSpan.FromMinutes(10) // Set timeout to 10 minutes for large file uploads
        };


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


                var today = DateTime.Today;
                var now = DateTime.Now;

                foreach (var appointment in appointments)
                {
                    // FUTURE DATE → always upcoming
                    if (appointment.Date > today)
                    {
                        Upcoming.Add(appointment);
                    }
                    // TODAY → compare only the time portion
                    else if (appointment.Date == today)
                    {
                        if (appointment.StartTime.HasValue && appointment.StartTime.Value.TimeOfDay > now.TimeOfDay)

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
                    }
                    // PAST DATE → always past
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

            if (Upcoming.Count == 0)
            {
                NoUpcomingAppointmentsPanel.Visibility = Visibility.Visible;
                UpcomingAppointmentsList.Visibility = Visibility.Collapsed;
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
        private async void DeleteAppointment_Click(object sender, RoutedEventArgs e)
        {
            var button = (System.Windows.Controls.Button)sender;

            var confirm = System.Windows.MessageBox.Show(
                $"Delete appointment '{button.Tag}'? Are you sure? You want to delete There is NO WAY of Retriving it back",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes)
                return;

            int appointmentId = (int)button.Tag;   

            try
            {
                string url = $"http://localhost:5085/api/appointments/{appointmentId}";

                using var response = await _http.DeleteAsync(url);  

                if (!response.IsSuccessStatusCode)
                {
                    string error = await response.Content.ReadAsStringAsync();
                    System.Windows.MessageBox.Show($"Delete failed: {error}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                System.Windows.MessageBox.Show("Appointment deleted successfully.", "Success");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
