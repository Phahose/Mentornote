using Mentornote.Backend.DTO;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.IO;
using System.Net.Http;
using System.Windows;
using System.Windows.Shapes;
using static System.Net.WebRequestMethods;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;

namespace Mentornote.Desktop
{
    /// <summary>
    /// Interaction logic for MeetingWindow.xaml
    /// </summary>
    public partial class MeetingWindow : Window
    {
        public string DescriptionText{ get; set;} = "We will be talking about the project roadmap and key milestones for the upcoming quarter.";
        private static readonly HttpClient _http = new HttpClient();
        public ObservableCollection<PendingFile> SelectedFiles { get; set; } = new();
        public string MeetingTopic { get; set; } = "Project Roadmap Discussion";
        public string MeetingDate { get; set; } = "September 15, 2024";
        public string MeetingTime { get; set; } = "10:00 AM - 11:00 AM";
        public string MeetingLocation { get; set; } = "Conference Room A";
        public string OrganizerName { get; set; } = "Alice Johnson";
        public string Descrption { get; set; } = "Meeting Description";


        //private async void Window_Loaded(object sender, RoutedEventArgs e)
        //{
        //    // Called once when window opens
        //    DesciptionText = "We We will ber Talking about the ";
        //}

        public MeetingWindow()
        {
            InitializeComponent();
            PopulateTimeCombos();
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

                foreach (var file in uploadedFilePaths)
                {
                    appointment.Add(file.FileContent, "Files", file.FileName);
                }
                // 3️⃣ Send to backend API
                var response = await _http.PostAsync("http://127.0.0.1:5085/api/appointments/upload", appointment);

                // 4️⃣ Handle response
                if (response.IsSuccessStatusCode)
                {
                    System.Windows.MessageBox.Show($"✅ uploaded successfully!",
                                     "Upload Complete",
                                     MessageBoxButton.OK,
                                     MessageBoxImage.Information);
                }
                else
                {
                    System.Windows.MessageBox.Show($"❌ Upload failed: {response.ReasonPhrase}",
                                    "Error",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Error);
                }
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
