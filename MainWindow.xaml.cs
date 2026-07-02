using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WpfAnimatedGif;
using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace VirtualPeto
{
    public partial class MainWindow : Window
    {
        private MediaPlayer _petAudioPlayer = new MediaPlayer();

        public MainWindow(string mediaPath, bool isVideo, double size, string soundPath, double volume)
        {
            InitializeComponent();

            this.Width = size;
            this.Height = size;

            if (isVideo)
            {
                PetVideo.Visibility = Visibility.Visible;
                PetVideo.Source = new Uri(mediaPath);
                PetVideo.Play();
            }
            else
            {
                PetVideo.Visibility = Visibility.Collapsed;
                
                var animImage = new BitmapImage();
                animImage.BeginInit();
                animImage.UriSource = new Uri(mediaPath);
                animImage.EndInit();
                ImageBehavior.SetAnimatedSource(PetImage, animImage);
            }

            if (!string.IsNullOrEmpty(soundPath))
            {
                _petAudioPlayer.Open(new Uri(soundPath));
                _petAudioPlayer.Volume = volume;
                _petAudioPlayer.MediaEnded += (s, e) => { _petAudioPlayer.Position = TimeSpan.Zero; _petAudioPlayer.Play(); };
                _petAudioPlayer.Play();
            }
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }

        private void PetVideo_MediaEnded(object sender, RoutedEventArgs e)
        {
            PetVideo.Position = TimeSpan.Zero;
            PetVideo.Play();
        }

        protected override void OnClosed(EventArgs e)
        {
            _petAudioPlayer.Stop();
            _petAudioPlayer.Close();
            base.OnClosed(e);
        }

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hwnd, int index);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);
        public const int WS_EX_TRANSPARENT = 0x00000020;
        public const int GWL_EXSTYLE = (-20);
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            this.Topmost = true;
            if (ConfigWindow.IsOverlappingEnabled)
            {
                SetClickThrough(ConfigWindow.IsOverlappingEnabled);
            }
        }
        public void SetClickThrough(bool isClickThrough)
        {
            IntPtr hwnd = new WindowInteropHelper(this).Handle;
            int extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            if (isClickThrough)
            {
                SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_TRANSPARENT);
            }
            else
            {
                SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle & ~WS_EX_TRANSPARENT);
            }
        }
    }
}