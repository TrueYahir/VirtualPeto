using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WpfAnimatedGif;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows.Threading;

namespace VirtualPeto
{
    public partial class MainWindow : Window
    {
        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hwnd, int index);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_SHOWWINDOW = 0x0040;
        private const uint SWP_NOACTIVATE = 0x0010;

        public const int WS_EX_TRANSPARENT = 0x00000020;
        public const int WS_EX_TOPMOST = 0x00000008;
        public const int WS_EX_TOOLWINDOW = 0x00000080;
        public const int WS_EX_NOACTIVATE = 0x08000000;
        public const int GWL_EXSTYLE = (-20);

        private const int WM_WINDOWPOSCHANGING = 0x0046;

        [StructLayout(LayoutKind.Sequential)]
        private struct WINDOWPOS
        {
            public IntPtr hwnd;
            public IntPtr hwndInsertAfter;
            public int x;
            public int y;
            public int cx;
            public int cy;
            public uint flags;
        }

        private MediaPlayer _petAudioPlayer = new MediaPlayer();
        private DispatcherTimer _topMostTimer = new DispatcherTimer();

        public MainWindow(string mediaPath, bool isVideo, double size, string soundPath, double volume)
        {
            InitializeComponent();
            
            this.ShowActivated = false;
            this.Width = size;
            this.Height = size;
            this.ShowInTaskbar = false;

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

            _topMostTimer.Tick += (s, e) => ForceTopMost();
        }

        private void ForceTopMost()
        {
            if (ConfigWindow.IsOverlappingEnabled)
            {
                this.Topmost = false;
                this.Topmost = true;
            }

            IntPtr hwnd = new WindowInteropHelper(this).Handle;
            if (hwnd != IntPtr.Zero)
            {
                SetWindowPos(hwnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW | SWP_NOACTIVATE);
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
            _topMostTimer.Stop();
            _petAudioPlayer.Stop();
            _petAudioPlayer.Close();
            base.OnClosed(e);
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            
            IntPtr hwnd = new WindowInteropHelper(this).Handle;
            HwndSource source = HwndSource.FromHwnd(hwnd);
            source.AddHook(WndProc);

            ApplyOverlaySettings();
        }

        public void ApplyOverlaySettings()
        {
            IntPtr hwnd = new WindowInteropHelper(this).Handle;
            if (hwnd == IntPtr.Zero) return;

            int extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            
            extendedStyle &= ~(WS_EX_TOPMOST | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE | WS_EX_TRANSPARENT);

            if (ConfigWindow.IsOverlappingEnabled)
            {
                _topMostTimer.Interval = TimeSpan.FromMilliseconds(50);
                _topMostTimer.Start();
                extendedStyle |= WS_EX_TOPMOST | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE;
            }
            else
            {
                _topMostTimer.Stop();
                this.Topmost = true;
                extendedStyle |= WS_EX_TOPMOST;
            }

            if (ConfigWindow.IsPetLocked)
            {
                extendedStyle |= WS_EX_TRANSPARENT;
            }

            SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle);
            ForceTopMost();
        }

        public void SetClickThrough(bool dummyValue)
        {
            ApplyOverlaySettings();
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wp, IntPtr lp, ref bool handled)
        {
            if (msg == WM_WINDOWPOSCHANGING && ConfigWindow.IsOverlappingEnabled)
            {
                WINDOWPOS wpStruct = Marshal.PtrToStructure<WINDOWPOS>(lp);
                if (wpStruct.hwndInsertAfter != HWND_TOPMOST)
                {
                    wpStruct.hwndInsertAfter = HWND_TOPMOST;
                    Marshal.StructureToPtr(wpStruct, lp, true);
                }
            }
            return IntPtr.Zero;
        }
    }
}