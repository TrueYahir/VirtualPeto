using System;
using System.IO;
using System.Windows;
using Microsoft.Win32;

namespace VirtualPeto
{
    public partial class AnimationSettingsWindow : Window
    {
        private AnimationData _data;
        private string _petDirectory;
        private string _selectedAudioFullPath = string.Empty;

        public AnimationSettingsWindow(AnimationData data, string petDirectory)
        {
            InitializeComponent();
            _data = data;
            _petDirectory = petDirectory;

            ChkIsSprite.IsChecked = _data.IsSpriteSheet;
            TxtCols.Text = _data.Columns.ToString();
            TxtRows.Text = _data.Rows.ToString();
            TxtFrames.Text = _data.TotalFrames.ToString();
            TxtFps.Text = _data.Fps.ToString();
            
            TxtWidth.Text = _data.FrameWidth.ToString();
            TxtHeight.Text = _data.FrameHeight.ToString();

            TxtAudio.Text = _data.SoundPath;
        }

        private void BtnBrowseAudio_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Audio Files|*.wav;*.mp3;*.ogg|All Files|*.*";
            
            if (openFileDialog.ShowDialog() == true)
            {
                _selectedAudioFullPath = openFileDialog.FileName;
                TxtAudio.Text = Path.GetFileName(_selectedAudioFullPath);
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            _data.IsSpriteSheet = ChkIsSprite.IsChecked ?? false;
            _data.Columns = int.TryParse(TxtCols.Text, out int c) ? c : 1;
            _data.Rows = int.TryParse(TxtRows.Text, out int r) ? r : 1;
            _data.TotalFrames = int.TryParse(TxtFrames.Text, out int tf) ? tf : 1;
            _data.Fps = int.TryParse(TxtFps.Text, out int fps) ? fps : 10;
            _data.FrameWidth = int.TryParse(TxtWidth.Text, out int w) ? w : 64;
            _data.FrameHeight = int.TryParse(TxtHeight.Text, out int h) ? h : 64;
            if (!string.IsNullOrEmpty(_selectedAudioFullPath) && File.Exists(_selectedAudioFullPath))
            {
                _data.SoundPath = _selectedAudioFullPath;
            }
            else if (string.IsNullOrEmpty(TxtAudio.Text))
            {
                _data.SoundPath = string.Empty;
            }

            this.DialogResult = true;
            this.Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}