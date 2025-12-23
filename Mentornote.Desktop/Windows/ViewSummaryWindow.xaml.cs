#nullable disable
using Mentornote.Backend.Models;
using Mentornote.Backend.Services;
using Mentornote.Desktop.Services;
using System.Net.Http.Json;
using System.Windows;


namespace Mentornote.Desktop.Windows
{
    public partial class ViewSummaryWindow : Window
    {
        private int _appointmentId;
        public ViewSummaryWindow(int appointmentId)
        {
            InitializeComponent();
            _appointmentId = appointmentId;
            Populate_Summary();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private async void Populate_Summary()
        {
            try
            {

                await MarkdownBrowser.EnsureCoreWebView2Async();
                var Summaryresponse = await ApiClient.Client.GetFromJsonAsync<AppointmentSummary>($"appointments/getSummaryByAppointmentId/{_appointmentId}");
                
                var appointments = await ApiClient.Client.GetFromJsonAsync<Appointment>($"appointments/getAppointmentById/{_appointmentId}");


                var html = Markdig.Markdown.ToHtml(Summaryresponse.SummaryText);
                MarkdownBrowser.NavigateToString($"<html><body style='color:white;background:#1E1E1E;font-family:Segoe UI;'>{html}</body></html>");


                //SummaryTextBlock.Text = Summaryresponse.SummaryText;
                AppointmentTitle.Text = appointments.Title;
                AppointmentDate.Text = appointments.Date.ToString();
                AppointmentStatus.Text = appointments.Status;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error fetching summary: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
