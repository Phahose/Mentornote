#nullable disable
using Mentornote.Backend.DTO;
using Mentornote.Backend.Models;
using Mentornote.Backend.Services;
using Mentornote.Desktop.Services;
using System.Collections.ObjectModel;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;   
using System.Windows.Media;

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
      
        public ObservableCollection<AppointmentFile> SelectedFiles { get; set; } = new();
        public List<AppointmentFile> ExistingRemovedFiles { get; set; } = new();
        public ObservableCollection<AppointmentFile> AppointmentFiles { get; set; } = new();
        private List<FileDTO> uploadedFiles = new();
        private List<int> removedFilesID = new();
        private int appointmentId;


        public AppointmentWindow(int appointmentId)
        {
            InitializeComponent();
            PopulateInputs(appointmentId); 
            HookValidationEvents();
           
            DataContext = this;
            
            this.appointmentId = appointmentId;

            if (appointmentId != 0)
            {
                AddAppointmentButton.Visibility = Visibility.Collapsed;
                UpdateAppointmentButton.Visibility = Visibility.Visible;
                StatusSection.Visibility = Visibility.Visible;
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
                        SelectedFiles.Add(new AppointmentFile { FilePath = path });
                }
            }
        }

        private async void UploadAll_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateLive())
            {
                return;
            }
              
            // 1️⃣ Pick a file from the user's system
            if (SelectedFiles.Count == 0)
            {
                System.Windows.MessageBox.Show("No files selected to upload!");
                return;
            }

           

       
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
                    uploadedFiles.Add(fileDTO);

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
                appointment.Add(new StringContent(TitleInput.Text), "Title");
                appointment.Add(new StringContent(AppointmentDescription.Text), "Description");
                appointment.Add(new StringContent(OrganizerInput.Text), "Organizer");
                appointment.Add(new StringContent(DateInput.Text), "Date");
                appointment.Add(new StringContent(StartTimeInput.Text), "StartTime");
                appointment.Add(new StringContent(EndTimeInput.Text), "EndTime");
                appointment.Add(new StringContent(DateInput.Text), "Date");
                appointment.Add(new StringContent(OrganizerInput.Text), "Organizer");

                foreach (var file in uploadedFiles)
                {
                    appointment.Add(file.FileContent, "Files", file.FileName);
                }


                // 3️⃣ Send to backend API
                var response = await ApiClient.Client.PostAsync("http://127.0.0.1:5085/api/appointments/upload", appointment);

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

        private async void UpdateAppointment_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateLive())
            {
                return;
            }
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
                    uploadedFiles.Add(fileDTO);

                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"❌ Error uploading file: {ex.Message}",
                                    "Error",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Error);
                }
            }

            foreach (var removingFile in ExistingRemovedFiles)
            {
                string filePath = removingFile.FilePath;
                string fileName = $"{Guid.NewGuid()}_{System.IO.Path.GetFileName(filePath)}";
                int fileId = removingFile.FileId;

                removedFilesID.Add(fileId);

               
            }

            try
            {
                using var appointment = new MultipartFormDataContent();
                appointment.Add(new StringContent(TitleInput.Text), "Title");
                appointment.Add(new StringContent(AppointmentDescription.Text), "Description");
                appointment.Add(new StringContent(OrganizerInput.Text), "Organizer");
                appointment.Add(new StringContent(DateInput.Text), "Date");
                appointment.Add(new StringContent(StartTimeInput.Text), "StartTime");
                appointment.Add(new StringContent(EndTimeInput.Text), "EndTime");
                appointment.Add(new StringContent(DateInput.Text), "Date");
                appointment.Add(new StringContent(OrganizerInput.Text), "Organizer");
                appointment.Add(new StringContent(StatusInput.Text), "Status");

                // Add files to remove
                foreach (var fileId in removedFilesID)
                {
                    appointment.Add(new StringContent(fileId.ToString()), "FilesIDsToRemove");
                }

                foreach (var file in uploadedFiles)
                {
                    appointment.Add(file.FileContent, "Files", file.FileName);
                }
                // 3️⃣ Send to backend API
                var response = await ApiClient.Client.PutAsync($"http://127.0.0.1:5085/api/appointments/update/{appointmentId}", appointment);

                // 4️⃣ Handle response
                if (!response.IsSuccessStatusCode)
                {
                    System.Windows.MessageBox.Show($"❌ Update failed: {response.ReasonPhrase}");
                    return;
                }

                if (response.StatusCode == System.Net.HttpStatusCode.Accepted)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var jobInfo = JsonSerializer.Deserialize<JobResponse>(json);
                    long jobId = jobInfo!.jobId;

                    System.Windows.MessageBox.Show($"✅ Update started You can keep working.");

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
            if ((sender as FrameworkElement)?.DataContext is AppointmentFile file)
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

        private void RemoveExistingFile_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is AppointmentFile file)
            {
                var result = System.Windows.MessageBox.Show(
                    $"Remove '{file.FileName}' from existing files?",
                    "Confirm Remove",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    AppointmentFiles.Remove(file);
                    ExistingRemovedFiles.Add(file);
                }
            }
        }
         


        private void RemoveAllFile_Click(object sender, RoutedEventArgs e)
        {
            SelectedFiles.Clear();
        }
        
        public async void PopulateInputs(int id)
        {

            var appointment = await ApiClient.Client.GetFromJsonAsync<Appointment>($"appointments/getAppointmentById/{id}");

            var docs = await ApiClient.Client.GetFromJsonAsync<List<AppointmentDocument>>($"appointments/getAppointmentDocumentsByAppointmentId/{appointment.Id}");
           
            foreach (var doc in docs)
            {
                AppointmentFiles.Add(new AppointmentFile { 
                    FilePath = doc.DocumentPath,
                    FileId = doc.Id
                });
            }

            PopulateTimeCombos();

            TitleInput.Text = appointment.Title;
            AppointmentInfoTitle.Text = appointment.Title;
            AppointmentDescription.Text = appointment.Description;
            DateInput.Text = appointment.Date.ToString();
            StartTimeInput.Text = appointment.StartTime?.ToString("h:mm tt") ?? "";
            EndTimeInput.Text = appointment.EndTime?.ToString("h:mm tt") ?? "";
            OrganizerInput.Text = appointment.Organizer;

            // Set the Default Status
            StatusInput.Items.Add("Scheduled");
            StatusInput.Items.Add("Completed");
            StatusInput.Items.Add("Cancelled");

            if (!string.IsNullOrEmpty(appointment.Status))
            {
                StatusInput.Text = appointment.Status;
            }
            else 
            {
                StatusInput.Text = "Scheduled";
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

            if (string.IsNullOrWhiteSpace(StartTimeInput.Text) || string.IsNullOrWhiteSpace(EndTimeInput.Text))
            {
                var now = DateTime.Now;
                var nearestHalfHour = now.AddMinutes(30 - now.Minute % 30).ToString("h:mm tt");
                StartTimeInput.Text = nearestHalfHour;
                EndTimeInput.Text = DateTime.Now.AddHours(1).ToString("h:mm tt");
            }
        }

        public static string GetDisplayFileName(string fullFileName)
        {
            if (string.IsNullOrWhiteSpace(fullFileName))
                return string.Empty;

            // Remove GUID + underscore (only if it looks like a GUID)
            string pattern = @"^[0-9a-fA-F\-]{36}_";
            return Regex.Replace(fullFileName, pattern, "");
        }

        private void MarkInvalid(System.Windows.Controls.Control control)
        {
            control.BorderBrush = new SolidColorBrush(Colors.Red);
            control.BorderThickness = new Thickness(2);
        }

        private void MarkValid(System.Windows.Controls.Control control)
        {
            control.BorderBrush = new SolidColorBrush(Colors.LightGreen);
            control.BorderThickness = new Thickness(1);
        }

        private void ClearValidation(System.Windows.Controls.Control control)
        {
            control.ClearValue(Border.BorderBrushProperty);
            control.ClearValue(Border.BorderThicknessProperty);
        }

        private bool ValidateLive()
        {
            bool isValid = true;

            // TITLE
            if (string.IsNullOrWhiteSpace(TitleInput.Text))
            {
                MarkInvalid(TitleInput);
                isValid = false;
            }
            else MarkValid(TitleInput);

            // DATE
            if (!DateTime.TryParse(DateInput.Text, out _))
            {
                MarkInvalid(DateInput);
                isValid = false;
            }
            else MarkValid(DateInput);

            // START TIME
            if (!DateTime.TryParse(StartTimeInput.Text, out var startTime))
            {
                MarkInvalid(StartTimeInput);
                isValid = false;
            }
            else MarkValid(StartTimeInput);

            // END TIME
            if (!DateTime.TryParse(EndTimeInput.Text, out var endTime))
            {
                MarkInvalid(EndTimeInput);
                isValid = false;
            }
            else MarkValid(EndTimeInput);

            // CHECK ORDER
            if (DateTime.TryParse(StartTimeInput.Text, out startTime) &&
                DateTime.TryParse(EndTimeInput.Text, out endTime))
            {
                if (startTime >= endTime)
                {
                    MarkInvalid(StartTimeInput);
                    MarkInvalid(EndTimeInput);
                    isValid = false;
                }
            }

            UpdateAppointmentButton.IsEnabled = isValid;
            return isValid;
        }

        private void HookValidationEvents()
        {
            TitleInput.TextChanged += (_, __) => ValidateLive();
            DateInput.SelectedDateChanged += (_, __) => ValidateLive();
            StartTimeInput.SelectionChanged += (_, __) => ValidateLive();
            EndTimeInput.SelectionChanged += (_, __) => ValidateLive();
        }

    }
    public class AppointmentFile
    {
        public string FilePath { get; set; } = string.Empty;
        public string FileName => AppointmentWindow.GetDisplayFileName(System.IO.Path.GetFileName(FilePath));
        public int FileId { get; set; }
    }

}
