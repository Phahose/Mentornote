#nullable disable
using Mentornote.Backend.DTO;
using Mentornote.Backend.Models;
using Mentornote.Desktop.Services;
using Mentornote.Desktop.Windows;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Mentornote.Desktop
{
    public partial class Overlay : Window
    {
        private bool _isListening = false;
        private bool _isPaused = false;

        private int appId;
        private bool _dragFromSuggestionPanel = false;


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
            if (_isListening == false)
            {
                ApiClient.Client.PostAsync($"http://localhost:5085/api/transcribe/start/{appId}", null);
                RecordingCheck.Text = "Listening In Ready To Help!!";
                _isListening = true;
                ListeningSection.Visibility = Visibility.Visible;
                SuggestionSection.Visibility = Visibility.Visible;
            }
            else if (_isListening == true)
            {
                if (_isPaused == false)
                {
                    _isPaused = true;
                    ApiClient.Client.PostAsync($"http://localhost:5085/api/transcribe/pause", null);
                    RecordingCheck.Text = "Listening Paused";
                    SuggestionSection.Visibility = Visibility.Collapsed;
                }
                else
                {
                    _isPaused = false;
                    ApiClient.Client.PostAsync($"http://localhost:5085/api/transcribe/resume", null);
                    RecordingCheck.Text = "Listening";
                    SuggestionSection.Visibility = Visibility.Visible;
                }
            }
        }


        private async void Suggestion_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                StatementText.Text = "Generating suggestion...";

                // 1️ Get transcript
                List<Utterance> UtteraceList = await ApiClient.Client.GetFromJsonAsync<List<Utterance>>("http://localhost:5085/api/transcribe/gettranscript");
                AppSettings settings = await ApiClient.Client.GetFromJsonAsync<AppSettings>("http://localhost:5085/api/settings/getAppSettings");
                List<string> transcriptList = UtteraceList?.Select(u => u.Text).ToList();

                // 2. Safely convert to one single string
                string fullTranscript = string.Empty;

                if (transcriptList != null)
                {
                    fullTranscript = string.Join(" ", transcriptList);
                }
                var cleanedTranscript = CleanTranscript(fullTranscript);


                StatementText.Text = string.Join(" ", cleanedTranscript.Split(' ').TakeLast(15));

                string userQuestion = transcriptList.LastOrDefault();
                string cleanedQuestion = CleanTranscript(userQuestion);
                var recentUtterances = transcriptList
                                        .Distinct()
                                        .TakeLast(settings.RecentUtteranceCount)
                                        .Select(CleanTranscript)
                                        .ToList();

                var memorySummaries =  await ApiClient.Client.GetFromJsonAsync<List<string>>("http://localhost:5085/api/transcribe/memory");

                //Build SuggestionRequest
                var requestPayload = new SuggestionRequest
                {
                    UserQuestion = userQuestion,
                    RecentUtterances = recentUtterances,
                    MemorySummaries = memorySummaries,
                    AppSettings = settings
                };

                //  Serialize to JSON
                var json = JsonSerializer.Serialize(requestPayload);
                using var content = new StringContent(json, Encoding.UTF8, "application/json");


                // POST and get streaming response
                var response = await ApiClient.Client.PostAsync($"http://localhost:5085/api/gemini/suggest/{appId}", content);
                response.EnsureSuccessStatusCode();

                var suggestion = await response.Content.ReadAsStringAsync();
                SuggestionMarkdown.Markdown = suggestion;
            }
            catch (Exception ex)
            {
                SuggestionMarkdown.Markdown = $"Error: {ex.Message}";
            }
        }


        private string CleanTranscript(string rawTranscript)
        {
            if (string.IsNullOrWhiteSpace(rawTranscript))
            {
                return string.Empty;
            }
                

            var cleaned = rawTranscript
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(line => line.Trim())
                .Where(line => !string.IsNullOrEmpty(line))
                .Distinct()
                .ToList();


            return string.Join(" ", cleaned);
        }


        private async void Close_Click(object sender, RoutedEventArgs e)
        {
            await ApiClient.Client.PostAsync($"http://localhost:5085/api/transcribe/stop/{appId}", null);
            _isListening = false;


            //Get transcript
            List<Utterance> transcriptList = await ApiClient.Client.GetFromJsonAsync<List<Utterance>>("http://localhost:5085/api/transcribe/gettranscript");
            string fullTranscript = string.Empty;

            // Handle null or empty
            if (transcriptList == null || transcriptList.Count == 0)
            {
                fullTranscript = "";
            }
            else
            {
                // Join ONLY the text field from each utterance
                fullTranscript = string.Join(" ", transcriptList .Where(u => u != null && !string.IsNullOrWhiteSpace(u.Text)).Select(u => u.Text));
            }

            if (string.IsNullOrWhiteSpace(fullTranscript))
            {
                System.Windows.MessageBox.Show("No transcript available to generate summary.");
                ListeningSection.Visibility = Visibility.Collapsed;
                Close();
                return;
            }
            else
            {
                // Prepare transcript JSON
                var transcriptBody = new { Transcript = fullTranscript };
                var transcriptJson = JsonConvert.SerializeObject(transcriptBody);
                var transcriptContent = new StringContent(transcriptJson, Encoding.UTF8, "application/json");



                // Generate or retrieve summary
                var appointment = await ApiClient.Client.GetFromJsonAsync<Appointment>($"appointments/getAppointmentById/{appId}");
                string summary;
                if (appointment.SummaryExists == false)
                {
                    summary = await ApiClient.Client.PostAsync($"http://localhost:5085/api/gemini/summary/{appId}", transcriptContent).Result.Content.ReadAsStringAsync();
                }
                else
                {
                    summary = "zxcvb";
                }

                if (summary != "zxcvb")
                {
                    var dialog = new SummaryDialog(summary);
                    bool? result = dialog.ShowDialog();

                    if (dialog.SaveClicked)
                    {
                        try
                        {
                            var body = new { summary = summary };
                            var json = JsonConvert.SerializeObject(body);
                            var content = new StringContent(json, Encoding.UTF8, "application/json");
                            var response = await ApiClient.Client.PostAsync($"http://localhost:5085/api/summary/save/{appId}", content);
                            System.Windows.MessageBox.Show("Summary saved!");
                        }
                        catch (Exception ex)
                        {
                            System.Windows.MessageBox.Show($"Error saving summary: {ex.Message}");
                        }
                    }
                }

                ListeningSection.Visibility = Visibility.Collapsed;
                Close();
            }
            
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
        private void Suggestion_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _dragFromSuggestionPanel = true;
        }
        private void Suggestion_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (_dragFromSuggestionPanel && e.LeftButton == MouseButtonState.Pressed)
            {
                _dragFromSuggestionPanel = false; // reset
                DragMove();
            }
        }
        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonUp(e);
            _dragFromSuggestionPanel = false;
        }




        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_LAYERED = 0x80000;

        [DllImport("user32.dll")] static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")] static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
    }

   
}
