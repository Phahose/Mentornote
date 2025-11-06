using Mentornote.Backend.DTO;
using Mentornote.Backend.Models;
using Mentornote.Backend.Services;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Windows;
using System.Windows.Shapes;
using static System.Net.WebRequestMethods;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;

namespace Mentornote.Desktop
{
    /// <summary>
    /// Interaction logic for MeetingWindow.xaml
    /// </summary>
    public partial class AppointmentWindow : Window
    {
        private static readonly HttpClient _http = new HttpClient();
        public ObservableCollection<PendingFile> SelectedFiles { get; set; } = new();
   

        //private async void Window_Loaded(object sender, RoutedEventArgs e)
        //{
        //    // Called once when window opens
        //    DesciptionText = "We We will ber Talking about the ";
        //}

        public AppointmentWindow()
        {
            InitializeComponent();
            PopulateInputs(1); // Example appointment ID
            DataContext = this;
        }

        private void ChooseFiles_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Select files to attach",
                Filter = "Documents|*.pdf;*.docx;*.txt|All Files|*.*",
                Multiselect = true
            };

            if (dialog.ShowDialog() == true)
            {
                foreach (var path in dialog.FileNames)
                {
                    if (!SelectedFiles.Any(f => f.FilePath == path))
                        SelectedFiles.Add(new PendingFile { FilePath = path });
                }
            }
        }

        private async void UploadAll_Click(object sender, RoutedEventArgs e)
        {
            // 1️⃣ Pick a file from the user's system
            if (SelectedFiles.Count == 0)
            {
                System.Windows.MessageBox.Show("No files selected to upload!");
                return;
            }
            List<FileDTO> uploadedFilePaths = new List<FileDTO>();

       


            foreach (var pendingFile in SelectedFiles)
            {
                string filePath = pendingFile.FilePath;
                string fileName = System.IO.Path.GetFileName(filePath);
                
                try
                { 
                    // Create file content
                    var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                    var fileContent = new StreamContent(fileStream);
                    fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
                    
                    FileDTO fileDTO = new FileDTO
                    {
                        FileContent = fileContent,
                        FileName = fileName
                    };
                    uploadedFilePaths.Add(fileDTO);

                  
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"❌ Error uploading file: {ex.Message}",
                                    "Error",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Error);
                }
            }

            try
            {
                using var appointment = new MultipartFormDataContent();
                appointment.Add(new StringContent("1"), "UserId");
                appointment.Add(new StringContent(TitleInput.Text), "Title");
                appointment.Add(new StringContent(AppointmentDescription.Text), "Description");
                appointment.Add(new StringContent(OrganizerInput.Text), "Organizer");
                appointment.Add(new StringContent(DateInput.Text), "Date");
                appointment.Add(new StringContent(StartTimeInput.Text), "StartTime");
                appointment.Add(new StringContent(EndTimeInput.Text), "EndTime");
                appointment.Add(new StringContent(DateInput.Text), "Date");
                appointment.Add(new StringContent(OrganizerInput.Text), "Organizer");

                foreach (var file in uploadedFilePaths)
                {
                    appointment.Add(file.FileContent, "Files", file.FileName);
                }
                // 3️⃣ Send to backend API
                var response = await _http.PostAsync("http://127.0.0.1:5085/api/appointments/upload", appointment);

                // 4️⃣ Handle response
                if (!response.IsSuccessStatusCode)
                {
                    System.Windows.MessageBox.Show($"❌ Upload failed: {response.ReasonPhrase}");
                    return;
                }

                var json = await response.Content.ReadAsStringAsync();
                var jobInfo = JsonSerializer.Deserialize<JobResponse>(json);
                long jobId = jobInfo!.Id;

                System.Windows.MessageBox.Show($"✅ Upload started (Job #{jobId}). You can keep working.");

                // 2️⃣  Start polling in background
                _ = Task.Run(async () =>
                {
                    while (true)
                    {
                        await Task.Delay(5000); // poll every 5 s

                        var statusResponse = await _http.GetAsync($"http://127.0.0.1:5085/api/appointments/status/{jobId}");
                        if (!statusResponse.IsSuccessStatusCode) break;

                        var statusJson = await statusResponse.Content.ReadAsStringAsync();
                        var status = JsonSerializer.Deserialize<JobResponse>(statusJson);

                        if (status == null) break;

                        if (status.Status.Equals("Completed", StringComparison.OrdinalIgnoreCase))
                        {
                            System.Windows.Application.Current.Dispatcher.Invoke(() =>
                                 System.Windows.MessageBox.Show($"✅ {status.ResultMessage}", "Upload Complete"));
                            break;
                        }

                        if (status.Status.Equals("Failed", StringComparison.OrdinalIgnoreCase))
                        {
                            System.Windows.Application.Current.Dispatcher.Invoke(() =>
                                 System.Windows.MessageBox.Show($"❌ Upload failed: {status.ResultMessage}", "Processing Error"));
                            break;
                        }
                    }
                });
            }
            catch (Exception)
            {

                throw;
            }
        }
      
        private void RemoveFile_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is PendingFile file)
            {
                var result = System.Windows.MessageBox.Show(
                    $"Remove '{file.FileName}' from selected files?",
                    "Confirm Remove",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                    SelectedFiles.Remove(file);
            }
        }
        private void RemoveAllFile_Click(object sender, RoutedEventArgs e)
        {
            SelectedFiles.Clear();
        }
        
        public void PopulateInputs(int id)
        {
            DBServices dbServices = new DBServices();

            Appointment appointment = new();
            appointment = dbServices.GetAppointmentById(1,1); // Example appointment ID

            List<AppointmentDocuments> docs = new();
            docs = dbServices.GetAppointmentDocumentsByAppointmentId(appointment.Id, 1); // Example appointment ID

            TitleInput.Text = appointment.Title;
            AppointmentDescription.Text = appointment.Description;
            DateInput.Text = appointment.Date.ToString();
            StartTimeInput.Text = appointment.StartTime.ToString();
            EndTimeInput.Text = appointment.EndTime.ToString();
            OrganizerInput.Text = appointment.Organizer;

            if (StartTimeInput.Text == "" && EndTimeInput.Text == "")
            {
                PopulateTimeCombos();
            }
        }

        private void PopulateTimeCombos()
        {
            var start = DateTime.Today;
            var end = start.AddDays(1);

            // Generate times every 30 minutes
            for (var time = start; time < end; time = time.AddMinutes(30))
            {
                string displayTime = time.ToString("h:mm tt"); // 12-hour format, e.g. "5:30 PM"
                StartTimeInput.Items.Add(displayTime);
                EndTimeInput.Items.Add(displayTime);
            }

            // Optionally preselect current nearest half-hour
            var now = DateTime.Now;
            var nearestHalfHour = now.AddMinutes(30 - now.Minute % 30).ToString("h:mm tt");
            StartTimeInput.Text = nearestHalfHour;
            EndTimeInput.Text = DateTime.Now.AddHours(1).ToString("h:mm tt");
        }
    }
    public class PendingFile
    {
        public string FilePath { get; set; } = string.Empty;
        public string FileName => System.IO.Path.GetFileName(FilePath);
    }

}
