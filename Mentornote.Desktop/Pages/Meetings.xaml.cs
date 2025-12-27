#nullable disable
using Mentornote.Backend.Models;
using Mentornote.Desktop.Helpers;
using Mentornote.Desktop.Services;
using Mentornote.Desktop.Windows;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net.Http.Json;
using System.Windows;


namespace Mentornote.Desktop.Pages
{

    public partial class Meetings : System.Windows.Controls.Page
    {

        public ObservableCollection<Appointment> Upcoming { get; } = new();
        public ObservableCollection<Appointment> Appointments { get; } = new();
        public ObservableCollection<Appointment> Past { get; } = new();
        public int UserId;

        public Meetings()
        {
            InitializeComponent();
            var user = UserSession.CurrentUser;

            UserId = user.UserId;
            string email = user.Email;

            this.DataContext = this;
            var appointments = GetAppointments();
        }

        private void OpenOverlay_Click(object sender, RoutedEventArgs e)
        {
            var button = (System.Windows.Controls.Button)sender;

            if (button?.Tag is int meetingId)
            {
                // Check if Overlay for this meeting is already open
                var existingOverlay = System.Windows.Application.Current.Windows
                    .OfType<Overlay>()
                    .FirstOrDefault();

                if (existingOverlay != null)
                {
                    existingOverlay.Activate(); // Bring it to front
                    return;
                }
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

        private void RefreshAppointments_Click(object sender, RoutedEventArgs e)
        {
            Upcoming.Clear();
            Past.Clear();
            Appointments.Clear();
            var appointments = GetAppointments();
        }
        private void ViewSummary_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as System.Windows.Controls.Button;
            if (button?.Tag is int appointmentId)
            {
                var window = new ViewSummaryWindow(appointmentId);
                window.Owner = Window.GetWindow(this);
                window.ShowDialog();
            }
        }


        private async void DeleteAppointment_Click(object sender, RoutedEventArgs e)
        {
            var button = (System.Windows.Controls.Button)sender;

            var confirm = System.Windows.MessageBox.Show(
                $"Delete appointment '{button.Tag}'? Are you sure? You want to delete There is NO WAY of Retriving it",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes)
            {
                return;
            }

            int appointmentId = (int)button.Tag;   
            int userId = UserId;


            try
            {
               
                string endpoint = $"appointments/deleteAppointment/{appointmentId}";

                using var response = await ApiClient.Client.DeleteAsync(endpoint);  

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

        private void ViewSummaryButton_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button && button.DataContext is Appointment appointment)
            {

                if (appointment.SummaryExists == true)
                {
                    button.Visibility = Visibility.Visible;
                }
                else
                {
                    button.Visibility = Visibility.Collapsed;
                }

            }
            else
            {
                // If for some reason it can't read the appointment, hide the button
                ((System.Windows.Controls.Button)sender).Visibility = Visibility.Collapsed;
            }
        }

        private async Task<List<Appointment>> GetAppointments()
        {
            try
            {
                if (JwtHelper.IsTokenExpired(ApiClient.AccessToken))
                {
                    await ApiClient.RefreshAccessTokenAsync();
                }
                var appointments = await ApiClient.Client.GetFromJsonAsync<List<Appointment>>($"appointments/getAppointmentsByUserId");
                Console.WriteLine(appointments);
                var today = DateTime.Today;
                var now = DateTime.Now;


                if (appointments != null)
                {
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
                                    StartTime = appointment.StartTime,
                                    EndTime = appointment.EndTime,
                                    AppointmentType = appointment.AppointmentType,
                                    Status = appointment.Status,
                                    SummaryExists = appointment.SummaryExists
                                });
                            }
                            else
                            {
                                Past.Add(new Appointment
                                {
                                    Id = appointment.Id,
                                    Title = appointment.Title,
                                    StartTime = appointment.StartTime,
                                    EndTime = appointment.EndTime,
                                    Status = appointment.Status,
                                    SummaryExists = appointment.SummaryExists,
                                    AppointmentType = appointment.AppointmentType,
                                });
                            }
                        }

                        else
                        {
                            Past.Add(new Appointment
                            {
                                Id = appointment.Id,
                                Title = appointment.Title,
                                StartTime = appointment.StartTime,
                                EndTime = appointment.EndTime,
                                Status = appointment.Status,
                                SummaryExists = appointment.SummaryExists,
                                AppointmentType = appointment.AppointmentType,
                            });
                        }

                        Appointments.Add(appointment);
                    }

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
                    }

                    if (Upcoming.Count == 0)
                    {
                        NoUpcomingAppointmentsPanel.Visibility = Visibility.Visible;
                        UpcomingAppointmentsList.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        NoUpcomingAppointmentsPanel.Visibility = Visibility.Collapsed;
                        UpcomingAppointmentsList.Visibility = Visibility.Visible;
                    }

                    if (Past.Count == 0)
                    {
                        NoAppointmentsPanel.Visibility = Visibility.Visible;
                        PastAppointmentsList.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        NoAppointmentsPanel.Visibility = Visibility.Collapsed;
                        PastAppointmentsList.Visibility = Visibility.Visible;
                    }
                }
                return appointments;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error fetching appointments: {ex.Message}");
            }
            return null;
        }
    }

}
