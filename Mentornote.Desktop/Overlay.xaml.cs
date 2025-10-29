#nullable disable
using Mentornote.Backend;
using Mentornote.Desktop.MVVM;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Text.Json;
using System.Threading.Tasks;

namespace Mentornote.Desktop
{
    public partial class Overlay : Window
    {
        private AudioListener _listener;
        private bool _isListening = false;
        private static readonly HttpClient _http = new HttpClient();
        private string _meetingId;
       
        public Overlay()
        {
            InitializeComponent();

            // Ensure the overlay fills the primary screen perfectly
            var screen = System.Windows.Forms.Screen.PrimaryScreen;
            this.Left = screen.Bounds.Left;
            this.Top = screen.Bounds.Top;
            this.Width = screen.Bounds.Width;
            this.Height = screen.Bounds.Height;

            Loaded += (_, __) => MakeTransparentLayer();
        }

        private void Mic_Click(object sender, RoutedEventArgs e)
        {
            if (!_isListening)
            {
                 _meetingId = Guid.NewGuid().ToString();
                _listener = new AudioListener();
               // _listener.AudioFileReady += ProcessAudioFile;
                _listener.AudioChunkReady += ProcessLiveAudio;
                _listener.StartListening();
                _isListening = true;
                Console.WriteLine("Started capturing system audio...");
                ListeningSection.Visibility = Visibility.Visible;
            }
            else 
            {                
                _listener.StopListening();
                _isListening = false;
                Console.WriteLine("Stopped capturing system audio.");
                RecordingCheck.Text = "Not Recording";
            }
        }

        private async void Suggestion_Click(object sender, RoutedEventArgs e)
        {
            Helper helper = new();
            StatementText.Text = "Generating suggestion...";
            var transcript = await helper.GetFullTranscriptAsync();
            StatementText.Text = transcript;
            var json = JsonSerializer.Serialize(transcript);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _http.PostAsync("http://localhost:5085/api/gemini/suggest", content);
            response.EnsureSuccessStatusCode();

            var suggestion = await response.Content.ReadAsStringAsync();
            SuggestionText.Text = suggestion;
        }


        private async void ProcessLiveAudio(object sender, byte[] chunk)
        {
            try
            {
                using var content = new ByteArrayContent(chunk);
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("audio/wav");


                _http.DefaultRequestHeaders.Remove("X-Meeting-ID");
                _http.DefaultRequestHeaders.Add("X-Meeting-ID", _meetingId);

                // send to your API endpoint (running in Speko.Backend or local ASP.NET)
                var response = await _http.PostAsync("http://localhost:5085/api/transcribe", content);
                response.EnsureSuccessStatusCode();

                // 2️⃣ Parse JSON into usable C# object
                var result = await response.Content.ReadFromJsonAsync<TranscibeResponse>();

                string transcript = result?.Text ?? ""; 

            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error sending chunk: {ex.Message}");
            }

        }

        private async void ProcessAudioFile(object sender, string filePath)
        {
            RecordedText.Text = "Audio Saved";
            try
            {
                Console.WriteLine($"[Summary] Uploading final audio file: {filePath}");

                using var content = new MultipartFormDataContent();
                var fileContent = new ByteArrayContent(await File.ReadAllBytesAsync(filePath));
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("audio/wav");
                content.Add(fileContent, "file", Path.GetFileName(filePath));

                var response = await _http.PostAsync("https://localhost:5085/api/transcribe/final", content);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"[Summary] Response: {json}");

                // optional: delete temp file after use
                File.Delete(filePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error sending final file: {ex.Message}");
            }
        }


        private void Close_Click(object sender, RoutedEventArgs e)
        {
            _listener?.StopListening();
            ListeningSection.Visibility = Visibility.Collapsed;
            Close();
        }

        // --- make the background transparent but still allow button clicks ---
        private void MakeTransparentLayer()
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            int exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);

            // Apply only WS_EX_LAYERED (keeps transparency, allows interaction)
            SetWindowLong(hwnd, GWL_EXSTYLE, exStyle | WS_EX_LAYERED);
        }

        // allow dragging if transparency is disabled for debugging
        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            DragMove();
        }

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_LAYERED = 0x80000;

        [DllImport("user32.dll")] static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")] static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
    }

   
}
