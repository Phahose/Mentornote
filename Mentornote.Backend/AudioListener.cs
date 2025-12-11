#nullable disable
using Mentornote.Backend.Models;
using Mentornote.Backend.Services;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace Mentornote.Backend
{
    /// <summary>
    /// Captures system audio (loopback) and emits both:
    ///  - full WAV file at the end (AudioFileReady)
    ///  - small byte[] chunks during recording (AudioChunkReady)
    /// </summary>
    public class AudioListener : IDisposable
    {
        private WasapiLoopbackCapture _capture;
        private string _currentDeviceId;
        private WaveFileWriter _writer;
        private string _tempFile;
        private Timer _deviceMonitor;

        private readonly List<byte> _buffer = new();          
        private readonly int _chunkSeconds = 1;              

        // This is an event it is not a regular declaration
        // It says "when the audio file is ready and when we have a new chunck of audi ready for processig, notify anyone who is listening"
        public event EventHandler<byte[]> AudioChunkReady;    // chunk ready for real-time processing
        private readonly Transcribe _transcribe;
        private readonly GeminiServices _geminiServices;
        public event EventHandler<string> TranscriptReady;
        private readonly List<Utterance> _transcriptHistory = new();
        public string FullMeetingTranscript;
        public Timer _summaryTimer;
        public int _appointmentId;
        public bool _isPaused = false;
        public AudioListener(Transcribe transcribe, GeminiServices geminiServices)
        {
            _transcribe = transcribe;
            _geminiServices = geminiServices;
        }

      
        public void StartListening(int appointmentId)
        {
            _appointmentId = appointmentId;
            var device = GetDefaultOutputDevice();
            _currentDeviceId = device.ID;

            // _tempFile = Path.Combine(Path.GetTempPath(), $"meeting_{Guid.NewGuid()}.wav");
            // _writer = new WaveFileWriter(_tempFile, _capture.WaveFormat);

            // start  capture system audio   
            _capture = new WasapiLoopbackCapture(device);            
            _capture.DataAvailable += Capture_DataAvailable;

            _summaryTimer = new Timer(async _ => await _geminiServices.GenerateRollingSummary(GetTranscriptHistory()), null, 15000, 15000);
            _capture.StartRecording();
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

        private void RestartCapture(MMDevice newDevice)
        {
            Console.WriteLine("🔄 Audio output changed. Restarting capture...");

            _capture?.StopRecording();
            _capture?.Dispose();

            _currentDeviceId = newDevice.ID;

            _capture = new WasapiLoopbackCapture(newDevice);
            _capture.DataAvailable += Capture_DataAvailable;
            _capture.StartRecording();
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

                    // After creating wavChunk 
                    if (!IsSilent(chunk, _capture.WaveFormat))
                    {
                        if (_isPaused == false)
                        {
                            List<Utterance> transcriptList = await _transcribe.DeepGramLiveTranscribe(wavChunk, _appointmentId);

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
