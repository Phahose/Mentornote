using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.IO;
using System.Net.Http;
using System.Windows;
using System.Windows.Shapes;
using static System.Net.WebRequestMethods;

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
        }

        private async void UploadFile_Click(object sender, RoutedEventArgs e)
        {
            // 1️⃣ Pick a file from the user's system
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Select a file to attach",
                Filter = "Documents|*.pdf;*.docx;*.txt|All Files|*.*",
                Multiselect = false
            };

            if (dialog.ShowDialog() == true)
            {
                string filePath = dialog.FileName;
                string fileName = System.IO.Path.GetFileName(filePath);

                try
                {
                    // 2️⃣ Prepare the multipart form data
                    using var form = new MultipartFormDataContent();
                    using var fileStream = System.IO.File.OpenRead(filePath);
                    var fileContent = new StreamContent(fileStream);

                    form.Add(fileContent, "file", fileName);

                    // 3️⃣ Send to your backend API
                    // Adjust URL to your actual backend route
                    var response = await _http.PostAsync( "http://127.0.0.1:5085/api/context/upload",
                        form);

                    // 4️⃣ Handle response
                    if (response.IsSuccessStatusCode)
                    {
                       System.Windows.MessageBox.Show($"✅ {fileName} uploaded successfully!",
                                        "Upload Complete",
                                        MessageBoxButton.OK,
                                        MessageBoxImage.Information);

                        // Optionally refresh the attachment list here
                        // await LoadAttachmentsAsync();
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
    }
    public class PendingFile
    {
        public string FilePath { get; set; } = string.Empty;
        public string FileName => System.IO.Path.GetFileName(FilePath);
    }

}
