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
        public string DesciptionText{ get; set;} = "We will be talking about the project roadmap and key milestones for the upcoming quarter.";
        private static readonly HttpClient _http = new HttpClient();
        public ObservableCollection<PendingFile> SelectedFiles { get; set; } = new();


        /*       private async void Window_Loaded(object sender, RoutedEventArgs e)
               {
                   // Called once when window opens
                   DesciptionText = "We We will ber Talking about the ";
               }*/

        public MeetingWindow()
        {
            InitializeComponent();
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
            foreach (var pendingFile in SelectedFiles)
            {
                string filePath = pendingFile.FilePath;
                string fileName = System.IO.Path.GetFileName(filePath);
                try
                { 
                    using var form = new MultipartFormDataContent();
                    // Create file content
                    using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                    var fileContent = new StreamContent(fileStream);
                    fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");

                    // Add file and other fields to form
                    form.Add(fileContent, "File", fileName);
                    form.Add(new StringContent("1"), "AppointmentId");
                    form.Add(new StringContent("1"), "UserId");       
                    
                    // 3️⃣ Send to your backend API
                    // Adjust URL to your actual backend route
                    var response = await _http.PostAsync("http://127.0.0.1:5085/api/appointments/upload", form);

                    // 4️⃣ Handle response
                    if (response.IsSuccessStatusCode)
                    {
                        System.Windows.MessageBox.Show($"✅ {fileName} uploaded successfully!",
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
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"❌ Error uploading file: {ex.Message}",
                                    "Error",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Error);
                }
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



    }
    public class PendingFile
    {
        public string FilePath { get; set; } = string.Empty;
        public string FileName => System.IO.Path.GetFileName(FilePath);
    }

}
