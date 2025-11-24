#nullable disable
using Mentornote.Backend;
using Mentornote.Desktop.MVVM;
using Mentornote.Desktop.Windows;
using Mentornote.Models;
using Mentornote.Services;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using static System.Net.WebRequestMethods;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;

namespace Mentornote.Desktop
{
    public partial class Overlay : Window
    {
        private AudioListener _listener;
        private bool _isListening = false;
        private static readonly HttpClient _http = new HttpClient();
        private string _meetingId;
        private int appId;

        public Overlay(int appointmentId)
        {
            InitializeComponent();

            // Ensure the overlay fills the primary screen perfectly
            var screen = System.Windows.Forms.Screen.PrimaryScreen;
            this.Left = screen.Bounds.Left;
            this.Top = screen.Bounds.Top;
            this.Width = screen.Bounds.Width;
            this.Height = screen.Bounds.Height;

            appId = appointmentId;

            Loaded += (_, __) => MakeTransparentLayer();
        }

        private void Mic_Click(object sender, RoutedEventArgs e)
        {
            if (!_isListening)
            {
                // _listener.AudioFileReady += ProcessAudioFile;

                _http.PostAsync( $"http://localhost:5085/api/transcribe/start/{appId}", null);

                _isListening = true;
                Console.WriteLine("Started capturing system audio...");
                ListeningSection.Visibility = Visibility.Visible;
            }
            else 
            {
                _http.PostAsync($"http://localhost:5085/api/transcribe/stop/{appId}", null);
                _isListening = false;
                Console.WriteLine("Stopped capturing system audio.");
                RecordingCheck.Text = "Not Recording";
            }
        }


        private async void Suggestion_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Helper helper = new();
                StatementText.Text = "Generating suggestion...";

                // 1️⃣ Get transcript
                List<string> transcriptList = await _http.GetFromJsonAsync<List<string>>("http://localhost:5085/api/transcribe/gettranscript");

                //  2. Safely convert to one single string
                string fullTranscript = string.Empty;

                if (transcriptList != null)
                {
                    fullTranscript = string.Join(" ", transcriptList);
                }
                var cleanedTranscript = CleanTranscript(fullTranscript);


                //var transcript = await helper.GetFullTranscriptAsync();
                //var cleanedTranscript = CleanTranscript(transcript);


                StatementText.Text = string.Join(" ", cleanedTranscript.Split(' ').TakeLast(15));

                // 2️⃣ Serialize to JSON
                var json = JsonSerializer.Serialize(cleanedTranscript);
                using var content = new StringContent(json, Encoding.UTF8, "application/json");

                // 3️⃣ POST and get streaming response
                var response = await _http.PostAsync($"http://localhost:5085/api/gemini/suggest/{appId}", content);
                response.EnsureSuccessStatusCode();

                var suggestion = await response.Content.ReadAsStringAsync();
                SuggestionText.Text = suggestion;
            }
            catch (Exception ex)
            {
                SuggestionText.Text = $"Error: {ex.Message}";
            }
        }


        private string CleanTranscript(string rawTranscript)
        {
            if (string.IsNullOrWhiteSpace(rawTranscript))
                return string.Empty;

            // Remove duplicate lines and trim whitespace
            var cleaned = rawTranscript
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(line => line.Trim())
                .Where(line => !string.IsNullOrEmpty(line))
                .Distinct() // remove exact duplicates
                .ToList();

            // Join back into a single paragraph
            return string.Join(" ", cleaned);
        }

     


        private async void Close_Click(object sender, RoutedEventArgs e)
        {
            _listener?.StopListening(appId);
            string summary = await  _http.GetStringAsync($"http://localhost:5085/api/gemini/summary/{appId}");

            if (summary != "zxcvb")
            {
                var dialog = new SummaryDialog(summary);
                bool? result = dialog.ShowDialog();


                if (dialog.SaveClicked)
                {
                    // User clicked “Save”
                    // TODO: Save to DB — you can call your backend endpoint here
                    await _http.PostAsJsonAsync($"http://localhost:5085/api/transcribe/save/{appId}", summary);
                }
                else
                {
                    // User clicked “Discard”
                    // do nothing
                }
            }

            

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
