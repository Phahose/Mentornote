using Mentornote.Backend.Models;
using Mentornote.Backend.Services;
using Mentornote.Desktop.Pages;
using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;

namespace Mentornote.Desktop.Windows
{
    public partial class ViewSummaryWindow : Window
    {
        private int _appointmentId;
        public DBServices dBServices = new();
        public int UserId = 1; // Placeholder for current user ID

        HttpClient _http = new HttpClient();
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
                var Summaryresponse = dBServices.GetSummaryByAppointmentId(_appointmentId);
                var appointments = dBServices.GetAppointmentById(_appointmentId, UserId);


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
