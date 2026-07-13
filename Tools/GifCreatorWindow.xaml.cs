using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Win32;

namespace VirtualPeto.Tools
{
    public partial class GifCreatorWindow : Window
    {
        private string[] _selectedImages = Array.Empty<string>();
        private readonly List<BitmapSource> _previewFrames = new List<BitmapSource>();
        private readonly DispatcherTimer _previewTimer = new DispatcherTimer(DispatcherPriority.Render);
        private int _previewFrameIndex = 0;
        private bool _isUiReady = false;

        public GifCreatorWindow()
        {
            InitializeComponent();
            _previewTimer.Tick += PreviewTimer_Tick;
            _isUiReady = true;
        }

        public GifCreatorWindow(string[] initialImages) : this()
        {
            SetSelectedImages(initialImages);
        }

        private void BtnSelectImages_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Multiselect = true,
                Title = "Select images for GIF",
                Filter = "Image Files (*.png;*.jpg;*.jpeg;*.bmp;*.webp)|*.png;*.jpg;*.jpeg;*.bmp;*.webp"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                SetSelectedImages(openFileDialog.FileNames);
            }
        }

        private void SetSelectedImages(IEnumerable<string> files)
        {
            _selectedImages = files.Where(File.Exists).ToArray();
            TxtImageCount.Text = $"{_selectedImages.Length} images selected";
            BuildPreview();
        }

        private int ParseOrDefault(string? value, int fallback, int min, int max)
        {
            if (!int.TryParse(value, out int parsed)) return fallback;
            if (parsed < min) return min;
            if (parsed > max) return max;
            return parsed;
        }

        private (int Width, int Height, int FrameCount, int DelayMs) GetSettings()
        {
            int width = ParseOrDefault(TxtWidth.Text, 256, 16, 2048);
            int height = ParseOrDefault(TxtHeight.Text, 256, 16, 2048);
            int frameCount = ParseOrDefault(TxtFrameCount.Text, 24, 2, 500);
            int delayMs = ParseOrDefault(TxtDelayMs.Text, 100, 10, 2000);
            return (width, height, frameCount, delayMs);
        }

        private void Settings_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            BuildPreview();
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

        private void BuildPreview()
        {
            if (!_isUiReady || ImgPreview == null || TxtPreviewHint == null) return;
            _previewTimer.Stop();
            _previewFrames.Clear();
            _previewFrameIndex = 0;

            if (_selectedImages.Length == 0)
            {
                ImgPreview.Source = null;
                TxtPreviewHint.Visibility = Visibility.Visible;
                return;
            }

            (int width, int height, int frameCount, int delayMs) = GetSettings();
            int framesToLoad = Math.Min(frameCount, _selectedImages.Length);

            for (int i = 0; i < framesToLoad; i++)
            {
                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.UriSource = new Uri(_selectedImages[i], UriKind.Absolute);
                bitmapImage.EndInit();
                bitmapImage.Freeze();

                BitmapSource renderedFrame = ResizeAndCenterFrame(bitmapImage, width, height);
                _previewFrames.Add(renderedFrame);
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

        private void BtnCreateGif_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedImages.Length < 2)
            {
                MessageBox.Show("Please select at least 2 images.", "GIF Creator", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            (int width, int height, int frameCount, int delayMs) = GetSettings();

            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Title = "Save GIF",
                Filter = "GIF Files (*.gif)|*.gif",
                FileName = "animation.gif",
                InitialDirectory = Path.GetDirectoryName(_selectedImages[0]) ?? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
            };

            if (saveFileDialog.ShowDialog() != true) return;

            try
            {
                int delayCs = Math.Max(1, delayMs / 10);
                GifGenerator generator = new GifGenerator();
                generator.CreateFromImages(_selectedImages, saveFileDialog.FileName, delayCs, width, height, frameCount);
                MessageBox.Show("GIF created successfully.", "GIF Creator", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error creating GIF: " + ex.Message, "GIF Creator", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _previewTimer.Stop();
            base.OnClosed(e);
        }
    }
}