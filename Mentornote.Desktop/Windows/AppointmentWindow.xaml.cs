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
using System.Threading;
using System.Text.RegularExpressions;

namespace Mentornote.Desktop
{
    /// <summary>
    /// Interaction logic for MeetingWindow.xaml
    /// </summary>
    public partial class AppointmentWindow : Window
    {
        private static readonly HttpClient _http = new HttpClient()
        {
            Timeout = TimeSpan.FromMinutes(10) // Set timeout to 10 minutes for large file uploads
        };
      
        public ObservableCollection<File> SelectedFiles { get; set; } = new();
        public ObservableCollection<File> AppointmentFiles { get; set; } = new();
   

        //private async void Window_Loaded(object sender, RoutedEventArgs e)
        //{
        //    // Called once when window opens
        //    DesciptionText = "We We will ber Talking about the ";
        //}

        public AppointmentWindow(int meetingId)
        {
            InitializeComponent();
            PopulateInputs(meetingId); // Example appointment ID
            DataContext = this;

            if (meetingId != 0)
            {
                AddAppointmentButton.Visibility = Visibility.Collapsed;
                UpdateAppointmentButton.Visibility = Visibility.Visible;
            }
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
                        SelectedFiles.Add(new File { FilePath = path });
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
                string fileName = $"{Guid.NewGuid()}_{System.IO.Path.GetFileName(filePath)}";

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

                if (response.StatusCode == System.Net.HttpStatusCode.Accepted)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var jobInfo = JsonSerializer.Deserialize<JobResponse>(json);
                    long jobId = jobInfo!.jobId;

                    System.Windows.MessageBox.Show($"✅ Upload started You can keep working.");

                    StartPollingForStatus(jobId);
                }
            }
            catch (Exception)
            {

                throw;
            }
        }
      
        private void RemoveFile_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is File file)
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

            int userId = 1; // Example user ID
            Appointment appointment = new();
            appointment = dbServices.GetAppointmentById(id,userId); // Example appointment ID


            //Populate existing documents
            List<AppointmentDocuments> docs = new();
            docs = dbServices.GetAppointmentDocumentsByAppointmentId(appointment.Id, userId); // Example appointment ID
            foreach (var doc in docs)
            {
                AppointmentFiles.Add(new File { 
                    FilePath = doc.DocumentPath 
                });
            }

            TitleInput.Text = appointment.Title ?? "New Appointment";
            AppointmentInfoTitle.Text = appointment.Title ?? "New Appointment";
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

        public void StartPollingForStatus(long jobId)
        {
            // 2️⃣  Start polling in background
            _ = Task.Run(async () =>
            {
                while (true)
                {
                    await Task.Delay(5000); // poll every 5 s

                    var jobResponse = await _http.GetAsync($"http://127.0.0.1:5085/api/appointments/status/{jobId}");
                    if (!jobResponse.IsSuccessStatusCode) 
                    {
                        System.Windows.Application.Current.Dispatcher.Invoke(() =>
                             System.Windows.MessageBox.Show("❌ Upload failed:, Processing Error"));
                        break;
                    }
                    

                    var jobInfoJson = await jobResponse.Content.ReadAsStringAsync();
                    var jobInfo = JsonSerializer.Deserialize<JobResponse>(jobInfoJson, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (jobInfo == null)
                    {
                        System.Windows.Application.Current.Dispatcher.Invoke(() =>
                             System.Windows.MessageBox.Show("❌ Upload failed:, Processing Error"));
                        break;
                    }

                    if (jobInfo.Status.Equals("Completed", StringComparison.OrdinalIgnoreCase))
                    {
                        System.Windows.Application.Current.Dispatcher.Invoke(() =>
                             System.Windows.MessageBox.Show($"✅ {jobInfo.ResultMessage}", "Upload Complete"));
                        break;
                    }

                    if (jobInfo.Status.Equals("Failed", StringComparison.OrdinalIgnoreCase))
                    {
                        System.Windows.Application.Current.Dispatcher.Invoke(() =>
                             System.Windows.MessageBox.Show($"❌ Upload failed: {jobInfo.ResultMessage}", "Processing Error"));
                        break;
                    }
                }
            });
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

        public static string GetDisplayFileName(string fullFileName)
        {
            if (string.IsNullOrWhiteSpace(fullFileName))
                return string.Empty;

            // Remove GUID + underscore (only if it looks like a GUID)
            string pattern = @"^[0-9a-fA-F\-]{36}_";
            return Regex.Replace(fullFileName, pattern, "");
        }
    }
    public class File
    {
        public string FilePath { get; set; } = string.Empty;
        public string FileName => AppointmentWindow.GetDisplayFileName(System.IO.Path.GetFileName(FilePath));
    }

}
