using System;
using System.IO;
using System.Windows;
using Microsoft.Win32;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using WpfAnimatedGif;

namespace VirtualPeto
{
    public partial class AnimationSettingsWindow : Window
    {
        private AnimationData _data;
        private string _petDirectory;
        private string _selectedAudioFullPath = string.Empty;

        //TEMPORAL SPRITE SHEET
        private DispatcherTimer _previewTimer = new DispatcherTimer(DispatcherPriority.Render);
        private List<BitmapSource> _previewFrames = new List<BitmapSource>();
        private int _previewFrameIndex = 0;
        private string _imagePath;
        private BitmapImage? _cachedImage;

        public AnimationSettingsWindow(AnimationData data, string petDirectory, string imagePath)
        {
            InitializeComponent();
            _data = data;
            _petDirectory = petDirectory;
            _imagePath = imagePath;
            _previewTimer.Tick += PreviewTimer_Tick;

            if (!_data.IsSpriteSheet)
            {
                TxtCols.IsEnabled = false;
                TxtRows.IsEnabled = false;
                TxtFrames.IsEnabled = false;
            }
            else
            {
                TxtCols.IsEnabled = true;
                TxtRows.IsEnabled = true;
                TxtFrames.IsEnabled = true;
            }

            if (!string.IsNullOrEmpty(_imagePath) && File.Exists(_imagePath))
            {
                _cachedImage = new BitmapImage();
                _cachedImage.BeginInit();
                _cachedImage.CacheOption = BitmapCacheOption.OnLoad;
                _cachedImage.UriSource = new Uri(_imagePath, UriKind.Absolute);
                _cachedImage.EndInit();
                _cachedImage.Freeze(); 
            }

            ChkIsSprite.IsChecked = _data.IsSpriteSheet;
            TxtCols.Text = _data.Columns.ToString();
            TxtRows.Text = _data.Rows.ToString();
            TxtFrames.Text = _data.TotalFrames.ToString();
            TxtFps.Text = _data.Fps.ToString();
            TxtWidth.Text = _data.FrameWidth.ToString();
            TxtHeight.Text = _data.FrameHeight.ToString();
            TxtAudio.Text = _data.SoundPath;

            TxtCols.TextChanged += Txt_TextChanged;
            TxtRows.TextChanged += Txt_TextChanged;
            TxtFrames.TextChanged += Txt_TextChanged;
            TxtFps.TextChanged += Txt_TextChanged;
            TxtWidth.TextChanged += Txt_TextChanged;
            TxtHeight.TextChanged += Txt_TextChanged;

            ChkIsSprite.Checked += Chk_Checked;
            ChkIsSprite.Unchecked += Chk_Checked;

            UpdatePreview();
        }
        private void LoadPreviewImage(string path)
        {
            if(!string.IsNullOrEmpty(path) && File.Exists(path))
            {
                try
                {
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(path, UriKind.Absolute);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    ImgPreview.Source = bitmap;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading preview image: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
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
        private void UpdatePreview()
        {
            _previewTimer.Stop();
            _previewFrames.Clear();
            _previewFrameIndex = 0;
            ImageBehavior.SetAnimatedSource(ImgPreview, null);

            if (string.IsNullOrEmpty(_imagePath) || !File.Exists(_imagePath)) return;
            //bool isSpriteSheet = ChkIsSprite.IsChecked == true; 
            bool isGif = _imagePath.EndsWith(".gif", StringComparison.OrdinalIgnoreCase);

            if (isGif)
            {
                try
                {
                    ImageBehavior.SetAnimatedSource(ImgPreview, _cachedImage);
                }
                catch 
                {
                    ImgPreview.Source = _cachedImage;
                }
                return; 
            }

            try
            {
                int.TryParse(TxtCols.Text, out int columns);
                int.TryParse(TxtRows.Text, out int rows);
                int.TryParse(TxtFrames.Text, out int totalFrames);
                int.TryParse(TxtFps.Text, out int fps);
                int.TryParse(TxtWidth.Text, out int frameWidth);
                int.TryParse(TxtHeight.Text, out int frameHeight);

                if (columns <= 0) columns = 1;
                if (rows <= 0) rows = 1;
                if (fps <= 0) fps = 10;
                
                int maxFrames = columns * rows;
                if (totalFrames <= 0 || totalFrames > maxFrames) totalFrames = maxFrames;

                if (frameWidth <= 0) frameWidth = Math.Max(1, _cachedImage!.PixelWidth / columns);
                if (frameHeight <= 0) frameHeight = Math.Max(1, _cachedImage!.PixelHeight / rows);

                for (int frame = 0; frame < totalFrames; frame++)
                {
                    int x = (frame % columns) * frameWidth;
                    int y = (frame / columns) * frameHeight;
                    
                    if (x >= _cachedImage!.PixelWidth || y >= _cachedImage.PixelHeight) continue;
                    
                    int cropWidth = Math.Min(frameWidth, _cachedImage.PixelWidth - x);
                    int cropHeight = Math.Min(frameHeight, _cachedImage.PixelHeight - y);

                    if (cropWidth <= 0 || cropHeight <= 0) continue;

                    Int32Rect rect = new Int32Rect(x, y, cropWidth, cropHeight);
                    CroppedBitmap cropped = new CroppedBitmap(_cachedImage!, rect); 
                    cropped.Freeze(); 
                    
                    _previewFrames.Add(cropped); 
                }

                if (_previewFrames.Count > 0)
                {
                    ImgPreview.Source = _previewFrames[0];
                    if (_previewFrames.Count > 1)
                    {
                        _previewTimer.Interval = TimeSpan.FromMilliseconds(1000.0 / fps);
                        _previewTimer.Start();
                    }
                }
            }
            catch (Exception)
            {
                // working on it
            }
        }
        private void Settings_Changed(object sender, RoutedEventArgs e)
        {
            if (this.IsLoaded)
            {
                UpdatePreview();
            }
        }

        private void PreviewTimer_Tick(object? sender, EventArgs e)
        {
            if (_previewFrames.Count == 0) return;
            _previewFrameIndex = (_previewFrameIndex + 1) % _previewFrames.Count;
            ImgPreview.Source = _previewFrames[_previewFrameIndex];
        }
        protected override void OnClosed(EventArgs e)
        {
            _previewTimer.Stop();
            base.OnClosed(e);
        }
        private void Txt_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (this.IsLoaded)
            {
                UpdatePreview();
            }
        }
        private void Chk_Checked(object sender, RoutedEventArgs e)
        {
            if (this.IsLoaded)
            {
                UpdatePreview();
            }
        }
       
    }
}