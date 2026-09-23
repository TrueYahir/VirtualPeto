using System;
using System.Windows;
using System.Windows.Input;

namespace VirtualPeto.Objects
{
    public partial class JukeboxPlaylistWindow : Window
    {
        private readonly JukeboxObject _jukebox;
        private bool _suppressVolumeChange;

        public JukeboxPlaylistWindow(JukeboxObject jukebox)
        {
            _jukebox = jukebox;
            _suppressVolumeChange = true;
            InitializeComponent();
            _suppressVolumeChange = false;
            _jukebox.PlaybackStateChanged += OnPlaybackStateChanged;
            Loaded += JukeboxPlaylistWindow_Loaded;
        }

        private void JukeboxPlaylistWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _jukebox.ReloadPlaylist();
            SongsList.ItemsSource = _jukebox.GetPlaylist();
            SongsList.SelectedIndex = _jukebox.GetCurrentTrackIndex();
            RefreshUiFromJukebox();
        }

        private void OnPlaybackStateChanged()
        {
            Dispatcher.Invoke(RefreshUiFromJukebox);
        }

        private void RefreshUiFromJukebox()
        {
            TxtCurrentSong.Text = _jukebox.CurrentTrackName;
            TxtStatus.Text = _jukebox.IsPlaying ? "Playing" : "Paused";
            BtnPlayPause.Content = _jukebox.IsPlaying ? "Pause" : "Play";
            BtnMute.Content = _jukebox.IsMuted ? "Unmute" : "Mute";
            TxtVolume.Text = $"Volume: {(int)Math.Round(_jukebox.Volume * 100)}%";

            _suppressVolumeChange = true;
            SldVolume.Value = _jukebox.Volume;
            _suppressVolumeChange = false;

            SongsList.SelectedIndex = _jukebox.GetCurrentTrackIndex();
        }

        private void SongsList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (SongsList.SelectedItem is string selectedFile)
            {
                string musicFolder = SettingsManager.Current.JukeboxMusicFolder;
                string fullPath = System.IO.Path.Combine(musicFolder, selectedFile);
                _jukebox.PlaySpecificSong(fullPath);
            }
        }

        private void BtnPrev_Click(object sender, RoutedEventArgs e) => _jukebox.PlayPrevious();
        private void BtnPlayPause_Click(object sender, RoutedEventArgs e) => _jukebox.TogglePlayPause();
        private void BtnNext_Click(object sender, RoutedEventArgs e) => _jukebox.PlayNext();
        private void BtnMute_Click(object sender, RoutedEventArgs e) => _jukebox.ToggleMute();

        private void SldVolume_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_suppressVolumeChange)
            {
                return;
            }

            _jukebox.SetVolume(e.NewValue);
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Space:
                    _jukebox.TogglePlayPause();
                    e.Handled = true;
                    break;
                case Key.M:
                    _jukebox.ToggleMute();
                    e.Handled = true;
                    break;
                case Key.Left:
                    _jukebox.PlayPrevious();
                    e.Handled = true;
                    break;
                case Key.Right:
                    _jukebox.PlayNext();
                    e.Handled = true;
                    break;
                case Key.Up:
                    _jukebox.SetVolume(_jukebox.Volume + 0.05);
                    e.Handled = true;
                    break;
                case Key.Down:
                    _jukebox.SetVolume(_jukebox.Volume - 0.05);
                    e.Handled = true;
                    break;
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _jukebox.PlaybackStateChanged -= OnPlaybackStateChanged;
            base.OnClosed(e);
        }
    }
}

