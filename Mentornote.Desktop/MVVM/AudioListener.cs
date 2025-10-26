#nullable disable
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mentornote.Desktop.MVVM
{
    public class AudioListener
    {
        private WasapiLoopbackCapture _capture;
        private WaveFileWriter _writer;
        private string _tempFile;

        public event EventHandler<string> AudioFileReady;

        public void StartListening()
        {
            _tempFile = Path.Combine(Path.GetTempPath(), $"meeting_{Guid.NewGuid()}.wav");
            _capture = new WasapiLoopbackCapture();   // captures system output
            _writer = new WaveFileWriter(_tempFile, _capture.WaveFormat);

            _capture.DataAvailable += (s, e) =>
            {
                _writer.Write(e.Buffer, 0, e.BytesRecorded);
            };

            _capture.RecordingStopped += (s, e) =>
            {
                _writer?.Dispose();
                _capture?.Dispose();
                AudioFileReady?.Invoke(this, _tempFile);
            };

            _capture.StartRecording();
            Console.WriteLine("Listening started...");
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
