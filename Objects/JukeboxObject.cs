using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Interop;
using WpfAnimatedGif;

namespace VirtualPeto.Objects
{
    public class JukeboxObject : PetWindowBase
    {
        private MediaPlayer _mediaPlayer;
        public bool IsPlaying { get; private set; } = false;
        private Image _gifImage;
        private MenuItem _playMenuItem = null!;
        private MenuItem _muteMenuItem = null!;

        public JukeboxObject()
        {
            this.SizeToContent = SizeToContent.WidthAndHeight;

            _mediaPlayer = new MediaPlayer();
            _mediaPlayer.MediaEnded += (s, e) =>
            {
                _mediaPlayer.Position = TimeSpan.Zero;
                _mediaPlayer.Play();
            };

            Grid container = new Grid();

            _gifImage = new Image
            {
                Stretch = Stretch.None,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            string gifPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Jukebox", "Original", "Jukebox.gif");
            if (File.Exists(gifPath))
            {
                BitmapImage bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.UriSource = new Uri(gifPath, UriKind.Absolute);
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.EndInit();
                ImageBehavior.SetAnimatedSource(_gifImage, bmp);
            }
            else
            {
                TextBlock icon = new TextBlock
                {
                    Text = "🎵",
                    Foreground = Brushes.White,
                    FontSize = 28,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                container.Children.Add(new Border
                {
                    Width = 60,
                    Height = 60,
                    CornerRadius = new CornerRadius(10),
                    Background = new SolidColorBrush(Color.FromRgb(30, 30, 40)),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(120, 80, 180)),
                    BorderThickness = new Thickness(2),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                });
                container.Children.Add(icon);
            }

            container.Children.Add(_gifImage);
            Content = container;

            BuildContextMenu();
        }

        private void BuildContextMenu()
        {
            ContextMenu contextMenu = new ContextMenu();

            _playMenuItem = new MenuItem { Header = "Play" };
            _playMenuItem.Click += PlayMenuItem_Click;

            _muteMenuItem = new MenuItem { Header = "Mute" };
            _muteMenuItem.Click += MuteMenuItem_Click;

            MenuItem volumeControlItem = new MenuItem();
            StackPanel volPanel = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            TextBlock volIcon = new TextBlock { Text = "🔊", Foreground = Brushes.White, FontSize = 14, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 10, 0) };
            TextBlock volText = new TextBlock { Text = "Volume: 50%", Foreground = Brushes.White, Width = 80, VerticalAlignment = VerticalAlignment.Center };
            Slider volSlider = new Slider 
            { 
                Width = 100, 
                Minimum = 0, 
                Maximum = 1, 
                Value = 0.5, 
                VerticalAlignment = VerticalAlignment.Center,
                IsSnapToTickEnabled = true,
                TickFrequency = 0.05
            };
            
            volSlider.ValueChanged += (s, e) => 
            {
                _mediaPlayer.Volume = volSlider.Value;
                volText.Text = $"Volume: {(int)(volSlider.Value * 100)}%";
            };

            volPanel.Children.Add(volIcon);
            volPanel.Children.Add(volText);
            volPanel.Children.Add(volSlider);
            volumeControlItem.Header = volPanel;

            MenuItem playlistMenuItem = new MenuItem { Header = "Playlist" };
            playlistMenuItem.Click += PlaylistMenuItem_Click;

            MenuItem closeMenuItem = new MenuItem { Header = "Close" };
            closeMenuItem.Click += (s, e) => this.Close();

            contextMenu.Items.Add(_playMenuItem);
            contextMenu.Items.Add(_muteMenuItem);
            contextMenu.Items.Add(volumeControlItem);
            contextMenu.Items.Add(playlistMenuItem);
            contextMenu.Items.Add(closeMenuItem);

            contextMenu.Opened += ContextMenu_Opened;

            this.ContextMenu = contextMenu;
        }

        private void ContextMenu_Opened(object sender, RoutedEventArgs e)
        {
            if (sender is ContextMenu menu)
            {
                menu.PlacementTarget = this;
                menu.Placement = System.Windows.Controls.Primitives.PlacementMode.Right;
                menu.HorizontalOffset = 8;
                menu.VerticalOffset = 0;

                if (PresentationSource.FromVisual(menu) is HwndSource hwndSource)
                {
                    SetWindowPos(hwndSource.Handle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
                }
            }
        }

        private void PlaylistMenuItem_Click(object sender, RoutedEventArgs e)
        {
            JukeboxPlaylistWindow playlistWindow = new JukeboxPlaylistWindow(this);
            playlistWindow.Show();
        }

        public void PlaySpecificSong(string path)
        {
            if (File.Exists(path))
            {
                _mediaPlayer.Stop();
                _mediaPlayer.Open(new Uri(path));
                _mediaPlayer.Play();
                IsPlaying = true;
                _playMenuItem.Header = "Pause";
            }
        }

        private void PlayMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (IsPlaying)
            {
                _mediaPlayer.Pause();
                IsPlaying = false;
                _playMenuItem.Header = "Play";
            }
            else
            {
                if (_mediaPlayer.Source == null)
                {
                    LoadRandomMusic();
                }

                if (_mediaPlayer.Source != null)
                {
                    _mediaPlayer.Play();
                    IsPlaying = true;
                    _playMenuItem.Header = "Pause";
                }
            }
        }

        private void MuteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            _mediaPlayer.IsMuted = !_mediaPlayer.IsMuted;
            _muteMenuItem.Header = _mediaPlayer.IsMuted ? "Unmute" : "Mute";
        }

        private void LoadRandomMusic()
        {
            string musicFolder = VirtualPeto.SettingsManager.Current.JukeboxMusicFolder;
            if (!string.IsNullOrEmpty(musicFolder) && Directory.Exists(musicFolder))
            {
                string[] songs = Directory.GetFiles(musicFolder, "*.mp3")
                    .Concat(Directory.GetFiles(musicFolder, "*.wav"))
                    .Concat(Directory.GetFiles(musicFolder, "*.m4a"))
                    .Concat(Directory.GetFiles(musicFolder, "*.wma"))
                    .ToArray();

                if (songs.Length > 0)
                {
                    Random rng = new Random();
                    string picked = songs[rng.Next(songs.Length)];
                    _mediaPlayer.Open(new Uri(picked));
                }
                else
                {
                    MessageBox.Show("There's no music selected.", "Jukebox", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            else
            {
                MessageBox.Show("There's no valid music folder configured. Go to settings to choose one.", "Jukebox", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _mediaPlayer.Stop();
            _mediaPlayer.Close();
            base.OnClosed(e);
        }
    }
}
