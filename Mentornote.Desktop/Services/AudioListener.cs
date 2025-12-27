#nullable disable
using Mentornote.Backend.DTO;
using Mentornote.Backend.Models;
using Mentornote.Backend.Services;
using Mentornote.Desktop.Services;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using System.IO;
using System.Media;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Timer = System.Threading.Timer;

namespace Mentornote.Desktop
{
    public class AudioListener : IDisposable
    {
        private WasapiLoopbackCapture _capture;
        private string _currentDeviceId;
        private WaveFileWriter _writer;
        private Timer _deviceMonitor;

        private readonly List<byte> _buffer = new();          
        private readonly int _chunkSeconds = 1;              

        // This is an event it is not a regular declaration
        // It says "when the audio file is ready and when we have a new chunck of audi ready for processig, notify anyone who is listening"
        public event EventHandler<byte[]> AudioChunkReady;    // chunk ready for real-time processing
        public event EventHandler<string> TranscriptReady;
        private readonly List<Utterance> _transcriptHistory = new();
        public string FullMeetingTranscript;
        public Timer _summaryTimer;
        public int _appointmentId;
        public bool _isPaused = false;
        public AudioListener()
        {

        }


        public async Task StartListening(int appointmentId)
        {

            _appointmentId = appointmentId;
            var device = GetDefaultOutputDevice();

            _currentDeviceId = device.ID;

            // start  capture system audio   
            _capture = new WasapiLoopbackCapture(device);
            _capture.DataAvailable += Capture_DataAvailable;
            _capture.StartRecording();


            _capture.RecordingStopped += (s, e) =>
            {
                StopListening(appointmentId);
                if (e.Exception != null)
                {
                    Console.WriteLine($"🛑 RecordingStopped with error: {e.Exception.Message}");
                }
                else
                {
                    Console.WriteLine("🛑 RecordingStopped normally (no error)");
                }
            };
            
            //List<Utterance> utterancesHistory = GetTranscriptHistory();
            //var response = await ApiClient.Client.PostAsJsonAsync("gemini/generateRollingSummary", utterancesHistory);
            //response.EnsureSuccessStatusCode();
            //List<string> transcriptHistory = await response.Content.ReadFromJsonAsync<List<string>>();

            //var transcriptHistory =  _geminiServices.GenerateRollingSummary(utterancesHistory);
            //_summaryTimer = new Timer(async _ => await _geminiServices.GenerateRollingSummary(GetTranscriptHistory()), null, 15000, 15000);
            _summaryTimer = new Timer(
                            async _ =>
                            {
                                try
                                {
                                    var resp = await ApiClient.Client.PostAsJsonAsync(
                                        "gemini/generateRollingSummary",
                                        GetTranscriptHistory()
                                    );

                                    resp.EnsureSuccessStatusCode();

                                    List<string> transcriptHistory = await resp.Content.ReadFromJsonAsync<List<string>>();
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"❌ Summary timer failed: {ex.Message}");
                                }
                            },
                            null,
                            TimeSpan.FromSeconds(15),
                            TimeSpan.FromSeconds(15)
                        );



            StartMonitoringDeviceChanges();
            Console.WriteLine("Listening started...");
        }


        public void StopListening(int appointnmentid)
        {
            if (_capture!=null)
            {
                _capture.StopRecording();
                _summaryTimer?.Dispose();
                Console.WriteLine("Listening stopped. File saved.");
            }
            else
            {
               Console.WriteLine("No active capture to stop.");
            }
          
        }

        public void PauseListening()
        {
            _isPaused = true;
            Console.WriteLine("🎧 Listening paused");
        }

        public void ResumeListening()
        {
            _isPaused = false;
            Console.WriteLine("🎧 Listening resumed");
        }



        private MMDevice GetDefaultOutputDevice()
        {
            return new MMDeviceEnumerator().GetDefaultAudioEndpoint(
                DataFlow.Render,
                Role.Multimedia
            );
        }


        // Detect audio output device changes
        private void StartMonitoringDeviceChanges()
        {
            _deviceMonitor = new Timer(_ =>
            {
                var device = GetDefaultOutputDevice();

                if (device.ID != _currentDeviceId)
                {
                    RestartCapture(device);
                }

            }, null, 0, 1000); // check every 1 second
        }


        // Restart capture with new device
        private void RestartCapture(MMDevice newDevice)
        {
            Console.WriteLine("🔄 Audio output changed. Restarting capture...");

            _capture?.StopRecording();
            _capture?.Dispose();

            _currentDeviceId = newDevice.ID;

            _capture = new WasapiLoopbackCapture(newDevice);
            _capture.DataAvailable += Capture_DataAvailable;
            _capture.StartRecording();
            Console.WriteLine("Restart Capture");
            Console.WriteLine(_capture.CaptureState);
        }


        private async void  Capture_DataAvailable(object sender, WaveInEventArgs e)
        {
            try
            {
                // 1️  keep writing to the full meeting file
                //_writer.Write(e.Buffer, 0, e.BytesRecorded);

                // 2️  collect data in memory for small chunks
                lock (_buffer)
                {
                    _buffer.AddRange(e.Buffer[..e.BytesRecorded]);
                }

                // 3️  if buffer > 1 second, fire chunk event
                int bytesPerSecond = _capture.WaveFormat.AverageBytesPerSecond;
                if (_buffer.Count >= bytesPerSecond * _chunkSeconds)
                {
                    byte[] chunk;
                    lock (_buffer)
                    {
                        chunk = _buffer.ToArray();
                        _buffer.Clear();
                    }

                    // Wrap the raw PCM chunk as a valid WAV (in-memory) using the SAME format
                    byte[] wavChunk;
                    using (var ms = new MemoryStream())
                    {
                        using (var w = new WaveFileWriter(ms, _capture.WaveFormat))
                        {
                            w.Write(chunk, 0, chunk.Length);
                        }
                        wavChunk = ms.ToArray(); // these bytes are now a proper .wav
                    }

                    // Check for silence before processing
                    if (!IsSilent(chunk, _capture.WaveFormat))
                    {
                        if (_isPaused == false)
                        {
                            var request = new AudioChunkRequest
                            {
                                WavChunk = wavChunk,
                                AppointmentId = _appointmentId,
                            };
                            var response = await ApiClient.Client.PostAsJsonAsync($"transcribe/deepgramTranscribe/live", request);

                            response.EnsureSuccessStatusCode();

                            List<Utterance> transcriptList = await response.Content.ReadFromJsonAsync<List<Utterance>>(); 
                            foreach (var utterance in transcriptList)
                            {
                                if (string.IsNullOrWhiteSpace(utterance.Text))
                                {
                                    utterance.Text = "[No speech detected]";
                                }
                            }

                            Utterance transcript = transcriptList.LastOrDefault();
                            _transcriptHistory.Add(transcript);
                            TranscriptReady?.Invoke(this, transcript.ToString());
                        }
                    }
                }
            }
            catch (Exception)
            {

                throw;
            }
        }


        public List<Utterance> GetTranscriptHistory()
        {
            return _transcriptHistory; 
        }

        private bool IsSilent(byte[] buffer, WaveFormat format, double threshold = 0.01)
        {
            // assumes 16-bit PCM
            int bytesPerSample = format.BitsPerSample / 8;
            int samples = buffer.Length / bytesPerSample;
            if (samples == 0) return true;

            double sumSquares = 0;
            for (int i = 0; i < buffer.Length; i += bytesPerSample)
            {
                short sample = BitConverter.ToInt16(buffer, i);
                double normalized = sample / 32768.0;
                sumSquares += normalized * normalized;
            }

            double rms = Math.Sqrt(sumSquares / samples);
            return rms < threshold;
        }

      
        public void Dispose()
        {
            _writer?.Dispose();
            _capture?.Dispose();
        }
    }

}
