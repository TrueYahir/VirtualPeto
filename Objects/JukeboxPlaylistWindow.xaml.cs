using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace VirtualPeto.Objects
{
    public partial class JukeboxPlaylistWindow : Window
    {
        private JukeboxObject _jukebox;

        public JukeboxPlaylistWindow(JukeboxObject jukebox)
        {
            InitializeComponent();
            _jukebox = jukebox;
            LoadSongs();
        }

        private void LoadSongs()
        {
            string musicFolder = VirtualPeto.SettingsManager.Current.JukeboxMusicFolder;
            if (!string.IsNullOrEmpty(musicFolder) && Directory.Exists(musicFolder))
            {
                var songs = Directory.GetFiles(musicFolder, "*.mp3")
                    .Concat(Directory.GetFiles(musicFolder, "*.wav"))
                    .Concat(Directory.GetFiles(musicFolder, "*.m4a"))
                    .Concat(Directory.GetFiles(musicFolder, "*.wma"))
                    .Select(f => Path.GetFileName(f))
                    .ToList();
                
                SongsList.ItemsSource = songs;
            }
        }

        private void SongsList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (SongsList.SelectedItem is string selectedFile)
            {
                string musicFolder = VirtualPeto.SettingsManager.Current.JukeboxMusicFolder;
                string fullPath = Path.Combine(musicFolder, selectedFile);
                _jukebox.PlaySpecificSong(fullPath);
                this.Close();
            }
        }
    }
}
