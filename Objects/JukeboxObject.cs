using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WpfAnimatedGif;

namespace VirtualPeto.Objects
{
    public class JukeboxObject : PetWindowBase
    {
        private readonly MediaPlayer _mediaPlayer = new MediaPlayer();
        private readonly Image _visualImage = new Image();
        private readonly MediaElement _visualVideo = new MediaElement();
        private readonly Border _fallbackVisual = new Border();

        private readonly List<string> _playlist = new List<string>();
        private int _currentTrackIndex = -1;
        private JukeboxPlaylistWindow? _playerWindow;

        public bool IsPlaying { get; private set; }
        public bool IsMuted => _mediaPlayer.IsMuted;
        public double Volume => _mediaPlayer.Volume;
        public string CurrentTrackName { get; private set; } = "No track selected";
        public event Action? PlaybackStateChanged;

        public JukeboxObject()
        {
            Width = SettingsManager.Current.JukeboxSize > 0 ? SettingsManager.Current.JukeboxSize : 150;
            Height = Width;
            ResizeMode = ResizeMode.NoResize;
            ShowInTaskbar = false;
            Topmost = true;

            BuildObjectVisual();
            LoadVisualFromSettings();
            LoadPlaylist();

            _mediaPlayer.MediaEnded += (_, __) => PlayNext();
            _mediaPlayer.Volume = 0.5;

            MouseRightButtonUp += JukeboxObject_MouseRightButtonUp;

            ContextMenu = new ContextMenu();
            var openPlayer = new MenuItem { Header = "Playlist" };
            openPlayer.Click += (_, __) => OpenPlayerWindow();
            var closeItem = new MenuItem { Header = "Close Jukebox" };
            closeItem.Click += (_, __) => Close();
            ContextMenu.Items.Add(openPlayer);
            ContextMenu.Items.Add(closeItem);
        }

        private void BuildObjectVisual()
        {
            var root = new Grid();

            _visualImage.Stretch = Stretch.Uniform;
            _visualImage.HorizontalAlignment = HorizontalAlignment.Stretch;
            _visualImage.VerticalAlignment = VerticalAlignment.Stretch;

            _visualVideo.Stretch = Stretch.Uniform;
            _visualVideo.LoadedBehavior = MediaState.Manual;
            _visualVideo.UnloadedBehavior = MediaState.Manual;
            _visualVideo.IsHitTestVisible = false;
            _visualVideo.MediaEnded += (_, __) =>
            {
                if (_visualVideo.Source != null)
                {
                    _visualVideo.Position = TimeSpan.Zero;
                    _visualVideo.Play();
                }
            };

            _fallbackVisual.CornerRadius = new CornerRadius(12);
            _fallbackVisual.Background = new SolidColorBrush(Color.FromRgb(28, 28, 40));
            _fallbackVisual.BorderBrush = new SolidColorBrush(Color.FromRgb(92, 92, 116));
            _fallbackVisual.BorderThickness = new Thickness(2);
            _fallbackVisual.Child = new TextBlock
            {
                Text = "J",
                Foreground = Brushes.White,
                FontSize = 34,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            root.Children.Add(_fallbackVisual);
            root.Children.Add(_visualImage);
            root.Children.Add(_visualVideo);

            Content = root;
        }

        private void JukeboxObject_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            ContextMenu.IsOpen = true;
            e.Handled = true;
        }

        private void OpenPlayerWindow()
        {
            if (_playerWindow == null || !_playerWindow.IsLoaded)
            {
                _playerWindow = new JukeboxPlaylistWindow(this);
                _playerWindow.Closed += (_, __) => _playerWindow = null;
                _playerWindow.Show();
                return;
            }

            _playerWindow.Activate();
        }

        private void LoadPlaylist()
        {
            string folder = SettingsManager.Current.JukeboxMusicFolder;
            _playlist.Clear();
            _currentTrackIndex = -1;

            if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
            {
                RaisePlaybackChanged();
                return;
            }

            _playlist.AddRange(Directory.GetFiles(folder, "*.mp3")
                .Concat(Directory.GetFiles(folder, "*.wav"))
                .Concat(Directory.GetFiles(folder, "*.m4a"))
                .Concat(Directory.GetFiles(folder, "*.wma"))
                .OrderBy(x => x)
                .Select(Path.GetFileName)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x!));

            if (_playlist.Count > 0)
            {
                _currentTrackIndex = 0;
            }

            RaisePlaybackChanged();
        }

        public IReadOnlyList<string> GetPlaylist() => _playlist;

        public void ReloadPlaylist()
        {
            LoadPlaylist();
        }

        public void PlaySpecificSong(string path)
        {
            if (!File.Exists(path))
            {
                return;
            }

            _mediaPlayer.Stop();
            _mediaPlayer.Open(new Uri(path, UriKind.Absolute));
            _mediaPlayer.Play();

            IsPlaying = true;
            CurrentTrackName = Path.GetFileName(path);

            int idx = _playlist.FindIndex(x => string.Equals(x, CurrentTrackName, StringComparison.OrdinalIgnoreCase));
            if (idx >= 0)
            {
                _currentTrackIndex = idx;
            }

            RaisePlaybackChanged();
        }

        public void TogglePlayPause()
        {
            if (IsPlaying)
            {
                _mediaPlayer.Pause();
                IsPlaying = false;
                RaisePlaybackChanged();
                return;
            }

            if (_mediaPlayer.Source == null)
            {
                if (_currentTrackIndex < 0 && _playlist.Count > 0)
                {
                    _currentTrackIndex = 0;
                }

                if (_currentTrackIndex >= 0 && _currentTrackIndex < _playlist.Count)
                {
                    string folder = SettingsManager.Current.JukeboxMusicFolder;
                    string fullPath = Path.Combine(folder, _playlist[_currentTrackIndex]);
                    PlaySpecificSong(fullPath);
                    return;
                }

                return;
            }

            _mediaPlayer.Play();
            IsPlaying = true;
            RaisePlaybackChanged();
        }

        public void PlayNext()
        {
            if (_playlist.Count == 0)
            {
                return;
            }

            _currentTrackIndex = _currentTrackIndex >= 0 ? (_currentTrackIndex + 1) % _playlist.Count : 0;
            string folder = SettingsManager.Current.JukeboxMusicFolder;
            PlaySpecificSong(Path.Combine(folder, _playlist[_currentTrackIndex]));
        }

        public void PlayPrevious()
        {
            if (_playlist.Count == 0)
            {
                return;
            }

            _currentTrackIndex = _currentTrackIndex >= 0 ? (_currentTrackIndex - 1 + _playlist.Count) % _playlist.Count : _playlist.Count - 1;
            string folder = SettingsManager.Current.JukeboxMusicFolder;
            PlaySpecificSong(Path.Combine(folder, _playlist[_currentTrackIndex]));
        }

        public void ToggleMute()
        {
            _mediaPlayer.IsMuted = !_mediaPlayer.IsMuted;
            RaisePlaybackChanged();
        }

        public void SetVolume(double value)
        {
            _mediaPlayer.Volume = Math.Max(0, Math.Min(1, value));
            RaisePlaybackChanged();
        }

        public int GetCurrentTrackIndex() => _currentTrackIndex;

        private void RaisePlaybackChanged()
        {
            PlaybackStateChanged?.Invoke();
        }

        private void LoadVisualFromSettings()
        {
            _visualVideo.Stop();
            _visualVideo.Source = null;
            _visualVideo.Visibility = Visibility.Collapsed;
            _visualImage.Visibility = Visibility.Collapsed;
            ImageBehavior.SetAnimatedSource(_visualImage, null);
            _visualImage.Source = null;
            _fallbackVisual.Visibility = Visibility.Visible;

            string selectedPath = SettingsManager.Current.JukeboxVisualPath;
            if (string.IsNullOrWhiteSpace(selectedPath))
            {
                selectedPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Jukebox", "Original", "Jukebox.gif");
            }

            if (string.IsNullOrWhiteSpace(selectedPath) || !File.Exists(selectedPath))
            {
                return;
            }

            string ext = Path.GetExtension(selectedPath).ToLowerInvariant();
            try
            {
                if (ext == ".mp4" || ext == ".webm" || ext == ".avi" || ext == ".mkv" || ext == ".wmv" || ext == ".mov")
                {
                    _visualVideo.Source = new Uri(selectedPath, UriKind.Absolute);
                    _visualVideo.Visibility = Visibility.Visible;
                    _fallbackVisual.Visibility = Visibility.Collapsed;
                    _visualVideo.Play();
                    return;
                }

                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.UriSource = new Uri(selectedPath, UriKind.Absolute);
                bmp.EndInit();
                bmp.Freeze();

                _visualImage.Visibility = Visibility.Visible;
                _fallbackVisual.Visibility = Visibility.Collapsed;

                if (ext == ".gif")
                {
                    ImageBehavior.SetAnimatedSource(_visualImage, bmp);
                }
                else
                {
                    _visualImage.Source = bmp;
                }
            }
            catch
            {
                _visualImage.Visibility = Visibility.Collapsed;
                _visualVideo.Visibility = Visibility.Collapsed;
                _fallbackVisual.Visibility = Visibility.Visible;
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _playerWindow?.Close();
            _mediaPlayer.Stop();
            _mediaPlayer.Close();
            _visualVideo.Stop();
            base.OnClosed(e);
        }
    }
}



