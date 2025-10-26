#nullable disable
using Mentornote.Desktop.MVVM;
using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using static System.Net.Mime.MediaTypeNames;

namespace Mentornote.Desktop
{
    public partial class Overlay : Window
    {
        private AudioListener _listener;
        private bool _isListening = false;
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

        private void OnAudioFileReady(object sender, string filePath)
        {
            RecordedText.Text = "Audio Saved";
        }

        private void Mic_Click(object sender, RoutedEventArgs e)
        {
            if (!_isListening)
            {
                _listener = new AudioListener();
                _listener.AudioFileReady += OnAudioFileReady;
                _listener.StartListening();
                _isListening = true;
                Console.WriteLine("Started capturing system audio...");
                RecordingCheck.Text = "Halooooooooooooooooo";
            }
            else 
            {                
                _listener.StopListening();
                _isListening = false;
                Console.WriteLine("Stopped capturing system audio.");
                RecordingCheck.Text = "Not Recording";
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            _listener?.StopListening();
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
