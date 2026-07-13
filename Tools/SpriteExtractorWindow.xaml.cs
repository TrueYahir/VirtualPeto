using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Win32;

namespace VirtualPeto.Tools
{
    public partial class SpriteExtractorWindow : Window
    {
        private string _selectedSpriteSheetPath = string.Empty;
        private readonly DispatcherTimer _previewTimer = new DispatcherTimer(DispatcherPriority.Render);
        private readonly List<BitmapSource> _previewFrames = new List<BitmapSource>();
        private int _previewFrameIndex = 0;
        private bool _isUiReady = false;

        public SpriteExtractorWindow()
        {
            InitializeComponent();
            _previewTimer.Tick += PreviewTimer_Tick;
            _isUiReady = true;
        }

        private void BtnSelectSprite_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Sprite Sheets (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp",
                Title = "Select Sprite Sheet"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                _selectedSpriteSheetPath = openFileDialog.FileName;
                TxtSelectedSprite.Text = Path.GetFileName(_selectedSpriteSheetPath);
                BuildPreview();
            }
        }

        private int ParseOrDefault(string? value, int fallback, int min, int max)
        {
            if (!int.TryParse(value, out int parsed)) return fallback;
            if (parsed < min) return min;
            if (parsed > max) return max;
            return parsed;
        }

        private (int Columns, int Rows, int Width, int Height, int FrameCount, int DelayMs) GetSettings()
        {
            int columns = ParseOrDefault(TxtSpriteColumns.Text, 1, 1, 100);
            int rows = ParseOrDefault(TxtSpriteRows.Text, 1, 1, 100);
            int width = ParseOrDefault(TxtOutputWidth.Text, 256, 16, 2048);
            int height = ParseOrDefault(TxtOutputHeight.Text, 256, 16, 2048);
            int frameCount = ParseOrDefault(TxtFrameCount.Text, 24, 1, 5000);
            int delayMs = ParseOrDefault(TxtDelayMs.Text, 100, 10, 2000);
            return (columns, rows, width, height, frameCount, delayMs);
        }

        private BitmapSource ResizeAndCenterFrame(BitmapSource source, int width, int height)
        {
            double scaleX = width / (double)source.PixelWidth;
            double scaleY = height / (double)source.PixelHeight;
            double scale = Math.Min(scaleX, scaleY);

            int targetW = Math.Max(1, (int)Math.Round(source.PixelWidth * scale));
            int targetH = Math.Max(1, (int)Math.Round(source.PixelHeight * scale));

            BitmapSource scaledFrame = source;
            if (targetW != source.PixelWidth || targetH != source.PixelHeight)
            {
                scaledFrame = new TransformedBitmap(source, new ScaleTransform(scale, scale));
            }

            DrawingVisual drawingVisual = new DrawingVisual();
            using (DrawingContext drawingContext = drawingVisual.RenderOpen())
            {
                drawingContext.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, width, height));
                double x = (width - scaledFrame.PixelWidth) / 2.0;
                double y = (height - scaledFrame.PixelHeight) / 2.0;
                drawingContext.DrawImage(scaledFrame, new Rect(x, y, scaledFrame.PixelWidth, scaledFrame.PixelHeight));
            }

            RenderTargetBitmap rendered = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
            rendered.Render(drawingVisual);
            rendered.Freeze();
            return rendered;
        }

        private void Settings_TextChanged(object sender, TextChangedEventArgs e)
        {
            BuildPreview();
        }

        private void BuildPreview()
        {
            if (!_isUiReady || ImgPreview == null || TxtPreviewHint == null) return;

            _previewTimer.Stop();
            _previewFrames.Clear();
            _previewFrameIndex = 0;

            if (string.IsNullOrEmpty(_selectedSpriteSheetPath) || !File.Exists(_selectedSpriteSheetPath))
            {
                ImgPreview.Source = null;
                TxtPreviewHint.Visibility = Visibility.Visible;
                return;
            }

            (int columns, int rows, int width, int height, int frameCount, int delayMs) = GetSettings();

            BitmapImage spriteSheet = new BitmapImage();
            spriteSheet.BeginInit();
            spriteSheet.CacheOption = BitmapCacheOption.OnLoad;
            spriteSheet.UriSource = new Uri(_selectedSpriteSheetPath, UriKind.Absolute);
            spriteSheet.EndInit();
            spriteSheet.Freeze();

            int frameWidth = Math.Max(1, spriteSheet.PixelWidth / columns);
            int frameHeight = Math.Max(1, spriteSheet.PixelHeight / rows);
            int totalFrames = columns * rows;
            int framesToLoad = Math.Min(Math.Max(1, frameCount), totalFrames);

            for (int frame = 0; frame < framesToLoad; frame++)
            {
                int x = (frame % columns) * frameWidth;
                int y = (frame / columns) * frameHeight;
                Int32Rect rect = new Int32Rect(x, y, frameWidth, frameHeight);
                CroppedBitmap cropped = new CroppedBitmap(spriteSheet, rect);
                cropped.Freeze();
                _previewFrames.Add(ResizeAndCenterFrame(cropped, width, height));
            }

            if (_previewFrames.Count == 0)
            {
                ImgPreview.Source = null;
                TxtPreviewHint.Visibility = Visibility.Visible;
                return;
            }

            TxtPreviewHint.Visibility = Visibility.Collapsed;
            ImgPreview.Source = _previewFrames[0];
            if (_previewFrames.Count > 1)
            {
                _previewTimer.Interval = TimeSpan.FromMilliseconds(delayMs);
                _previewTimer.Start();
            }
        }

        private void PreviewTimer_Tick(object? sender, EventArgs e)
        {
            if (_previewFrames.Count == 0) return;
            _previewFrameIndex = (_previewFrameIndex + 1) % _previewFrames.Count;
            ImgPreview.Source = _previewFrames[_previewFrameIndex];
        }

        private void BtnExtractSprites_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_selectedSpriteSheetPath) || !File.Exists(_selectedSpriteSheetPath))
            {
                MessageBox.Show("Please select a sprite sheet first.", "Sprite Extractor", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            (int columns, int rows, int width, int height, int frameCount, int delayMs) = GetSettings();

            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "GIF Image|*.gif",
                FileName = "ExtractedPet.gif"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    SpriteExtractor extractor = new SpriteExtractor();
                    int delayCs = Math.Max(1, delayMs / 10);
                    extractor.ExtractToGif(_selectedSpriteSheetPath, saveFileDialog.FileName, columns, rows, delayCs, width, height, frameCount);

                    MessageBox.Show("GIF created successfully.", "Sprite Extractor", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error creating GIF: " + ex.Message, "Sprite Extractor", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _previewTimer.Stop();
            base.OnClosed(e);
        }
    }
}