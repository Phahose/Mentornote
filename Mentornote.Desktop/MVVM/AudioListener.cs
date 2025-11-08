#nullable disable
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using NAudio.Wave;

namespace Mentornote.Desktop.MVVM
{
    /// <summary>
    /// Captures system audio (loopback) and emits both:
    ///  - full WAV file at the end (AudioFileReady)
    ///  - small byte[] chunks during recording (AudioChunkReady)
    /// </summary>
    public class AudioListener : IDisposable
    {
        private WasapiLoopbackCapture _capture;
        private WaveFileWriter _writer;
        private string _tempFile;

        private readonly List<byte> _buffer = new();          
        private readonly int _chunkSeconds = 1;              

        // This is an event it is not a regular declaration
        // It says "when the audio file is ready and when we have a new chunck of audi ready for processig, notify anyone who is listening"
        public event EventHandler<string> AudioFileReady;     // full file ready
        public event EventHandler<byte[]> AudioChunkReady;    // chunk ready for real-time processing

        public void StartListening()
        {
            _tempFile = Path.Combine(Path.GetTempPath(), $"meeting_{Guid.NewGuid()}.wav");
            _capture = new WasapiLoopbackCapture(); // capture system audio
            _writer = new WaveFileWriter(_tempFile, _capture.WaveFormat);

    
            _capture.DataAvailable += Capture_DataAvailable;

            _capture.RecordingStopped += Capture_RecordingStopped;

            _capture.StartRecording();
            Console.WriteLine("Listening started...");
        }

        /// <summary>
        /// We get audio data here in small chunks
        /// The Audio is still continually written to the full WAV file
        /// We have to use lock() to protect the in-memory buffer
        /// Its is like closing the door during audio processing while other bits of Audio  is still knocking!
        /// The we invoke the AudioChunkReady event asynchronously to avoid blocking 
        /// This is saying "here is a chunk of audio data for you to process"
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void Capture_DataAvailable(object sender, WaveInEventArgs e)
        {
            // 1️⃣  keep writing to the full meeting file
            _writer.Write(e.Buffer, 0, e.BytesRecorded);

            // 2️⃣  collect data in memory for small chunks
            lock (_buffer)
            {
                _buffer.AddRange(e.Buffer[..e.BytesRecorded]);
            }

            // 3️⃣  if buffer > N seconds, fire chunk event
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

                // After creating wavChunk (as shown earlier)
                if (!IsSilent(chunk, _capture.WaveFormat))
                {
                    await Task.Run(() => AudioChunkReady?.Invoke(this, wavChunk));
                }
                else
                {
                    Console.WriteLine("🔇 Skipped silent chunk — no meaningful audio.");
                }

            }
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
                double normalized = sample / 32768.0; // range -1 to 1
                sumSquares += normalized * normalized;
            }

            double rms = Math.Sqrt(sumSquares / samples);
            return rms < threshold; // true if "quiet enough"
        }


        private void Capture_RecordingStopped(object sender, StoppedEventArgs e)
        {
            // When recording stops, finalize and clean up
            _writer?.Dispose();
            _capture?.Dispose();

            Console.WriteLine("🛑 Recording stopped.");

            // Notify overlay that file is ready
            AudioFileReady?.Invoke(this, _tempFile);
        }

        public void StopListening()
        {
            _capture?.StopRecording();
            Console.WriteLine("Listening stopped. File saved.");
        }

        public void Dispose()
        {
            _writer?.Dispose();
            _capture?.Dispose();
        }
    }
}
