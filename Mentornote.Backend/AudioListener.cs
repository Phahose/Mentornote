#nullable disable
using Azure.Core;
using DocumentFormat.OpenXml.Office2010.ExcelAc;
using Mentornote.Backend;
using Mentornote.Backend.Models;
using Microsoft.AspNetCore.Mvc;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;

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
        private WaveFileWriter _writer;
        private string _tempFile;

        private readonly List<byte> _buffer = new();          
        private readonly int _chunkSeconds = 1;              

        // This is an event it is not a regular declaration
        // It says "when the audio file is ready and when we have a new chunck of audi ready for processig, notify anyone who is listening"
        public event EventHandler<string> AudioFileReady;     // full file ready
        public event EventHandler<byte[]> AudioChunkReady;    // chunk ready for real-time processing
        private readonly Transcribe _transcribe;
        public event EventHandler<string> TranscriptReady;
        private readonly List<string> _transcriptHistory = new();

        public string FullMeetingTranscript;





        public int _appointmentId;
        public AudioListener(Transcribe transcribe)
        {
            _transcribe = transcribe;
        }

        public void StartListening(int appointmentId)
        {
            _tempFile = Path.Combine(Path.GetTempPath(), $"meeting_{Guid.NewGuid()}.wav");


            _capture = new WasapiLoopbackCapture(); //start  capture system audio
            _writer = new WaveFileWriter(_tempFile, _capture.WaveFormat);

            _appointmentId = appointmentId;
            _capture.DataAvailable += Capture_DataAvailable;

            _capture.RecordingStopped += Capture_RecordingStopped;


            _capture.StartRecording();
            Console.WriteLine("Listening started...");
        }

        private async void  Capture_DataAvailable(object sender, WaveInEventArgs e)
        {

            try
            {
                // 1️  keep writing to the full meeting file
                _writer.Write(e.Buffer, 0, e.BytesRecorded);

                // 2️  collect data in memory for small chunks
                lock (_buffer)
                {
                    _buffer.AddRange(e.Buffer[..e.BytesRecorded]);
                }

                // 3️  if buffer > N seconds, fire chunk event
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
                        // await Task.Run(() => AudioChunkReady?.Invoke(this, wavChunk));

                        List<string> transcriptList =  await _transcribe.DeepGramLiveTranscribe(wavChunk, _appointmentId);

                        string transcript = transcriptList.LastOrDefault() ?? "[No speech detected]";

                        if (!string.IsNullOrWhiteSpace(transcript))
                        {
                            _transcriptHistory.Add(transcript);
                            TranscriptReady?.Invoke(this, transcript);
                        }

                    }
                }
            }
            catch (Exception)
            {

                throw;
            }
       
            
        }

        public List<string> GetTranscriptHistory()
        {
            return _transcriptHistory.ToList(); // return a copy for safety
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


        private void Capture_RecordingStopped(object sender, StoppedEventArgs e)
        {
            // When recording stops, finalize and clean up
            _writer?.Dispose();
            _capture?.Dispose();

            Console.WriteLine("🛑 Recording stopped.");

            // Notify overlay that file is ready
            _ = GetEndOfMeetingTranscript(_tempFile, _appointmentId);
        }

        private async Task GetEndOfMeetingTranscript(string filePath, int appointmentId)
        {
            try
            {
                byte[] audioBytes = await System.IO.File.ReadAllBytesAsync(filePath);

                // 1️⃣ Full transcription
                 List<string> fullList = await _transcribe.DeepGramLiveTranscribe(audioBytes, appointmentId);
                 string fullTranscript = string.Join(" ", fullList);

                FullMeetingTranscript = fullTranscript;
                Console.WriteLine("✔ Final summary generated.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Error generating final summary: " + ex.Message);
            }

        }

        public string GetFullMeetingTranscript()
        {
            return FullMeetingTranscript;
        }
        public void StopListening(int appointnmentid)
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
