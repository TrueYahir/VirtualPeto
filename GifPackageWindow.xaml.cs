using System;
using System.IO;
using System.IO.Compression;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using WpfAnimatedGif;

namespace VirtualPeto
{
    public partial class GifPackageWindow : Window
    {
        private MediaPlayer _mediaPlayer = new MediaPlayer();
        private string? _selectedGifPath;
        private string? _selectedSoundPath;

        public GifPackageWindow()
        {
            InitializeComponent();
        }

        private void BtnBrowseGif_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog { Filter = "GIF Images (*.gif)|*.gif" };
            if (dlg.ShowDialog() == true)
            {
                _selectedGifPath = dlg.FileName;
                TxtGifPath.Text = Path.GetFileName(_selectedGifPath);
                if (string.IsNullOrWhiteSpace(TxtName.Text))
                {
                    TxtName.Text = Path.GetFileNameWithoutExtension(_selectedGifPath);
                }
                BitmapImage img = new BitmapImage(new Uri(_selectedGifPath));
                ImageBehavior.SetAnimatedSource(ImgPreview, img);
            }
        }

        private void BtnBrowseSound_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog { Filter = "Audio Files (*.mp3;*.wav)|*.mp3;*.wav" };
            if (dlg.ShowDialog() == true)
            {
                _selectedSoundPath = dlg.FileName;
                TxtSoundPath.Text = Path.GetFileName(_selectedSoundPath);
                SoundControls.Visibility = Visibility.Visible;
                _mediaPlayer.Open(new Uri(_selectedSoundPath));
            }
        }

        private void BtnPlaySound_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedSoundPath != null)
            {
                _mediaPlayer.Position = TimeSpan.Zero;
                _mediaPlayer.Volume = SldVolume.Value;
                _mediaPlayer.Play();
            }
        }

        private void SldVolume_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (TxtVolumeVal != null)
            {
                TxtVolumeVal.Text = $"{(int)(SldVolume.Value * 100)}%";
                _mediaPlayer.Volume = SldVolume.Value;
            }
        }

        private void BtnBuild_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtName.Text) || string.IsNullOrEmpty(_selectedGifPath))
            {
                MessageBox.Show("Please enter a name and select a GIF file.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SaveFileDialog saveDlg = new SaveFileDialog
            {
                Filter = "VirtualPeto Package (*.vpet)|*.vpet",
                FileName = TxtName.Text
            };

            if (saveDlg.ShowDialog() == true)
            {
                try
                {
                    string tempDir = Path.Combine(Path.GetTempPath(), "vpet_build_" + Guid.NewGuid().ToString());
                    Directory.CreateDirectory(tempDir);
                    string extGif = Path.GetExtension(_selectedGifPath);
                    string targetGifName = "pet" + extGif;
                    File.Copy(_selectedGifPath, Path.Combine(tempDir, targetGifName));
                    string? targetSoundName = null;
                    if (!string.IsNullOrEmpty(_selectedSoundPath))
                    {
                        string extSound = Path.GetExtension(_selectedSoundPath);
                        targetSoundName = "sound" + extSound;
                        File.Copy(_selectedSoundPath, Path.Combine(tempDir, targetSoundName));
                    }
                    var config = new
                    {
                        Name = TxtName.Text.Trim(),
                        GifFile = targetGifName,
                        SoundFile = targetSoundName,
                        Volume = SldVolume.Value
                    };

                    string jsonString = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(Path.Combine(tempDir, "config.json"), jsonString);
                    if (File.Exists(saveDlg.FileName)) File.Delete(saveDlg.FileName);
                    ZipFile.CreateFromDirectory(tempDir, saveDlg.FileName);
                    Directory.Delete(tempDir, true);

                    MessageBox.Show("Package .vpet built successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    this.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error building package: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}