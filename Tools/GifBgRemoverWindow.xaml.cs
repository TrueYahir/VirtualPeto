using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using ImageMagick; 
using WF = System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Controls;

namespace VirtualPeto.Tools
{
    public partial class GifBgRemoverWindow : Window
    {
        private readonly string _currentGifPath;
        private readonly string _libraryPath;
        private readonly List<(byte R, byte G, byte B)> _manualColors = new();
        private readonly Stack<byte[]?> _previewUndoStack = new();
        private byte[]? _previewPixels;
        private int _previewWidth;
        private int _previewHeight;
        private int _previewStride;
        private byte[]? _processedGifData;

        public GifBgRemoverWindow(string gifPath, string libraryPath)
        {
            _currentGifPath = gifPath;
            _libraryPath = libraryPath;
            InitializeComponent();
            
            TxtFileName.Text = Path.GetFileName(_currentGifPath);
            InitializePreview();
        }

        private void BtnRemoveBackground_Click(object sender, RoutedEventArgs e)
        {
            BtnRemoveBackground.IsEnabled = false;
            BtnRemoveBackground.Content = "Processing...";

            try
            {
                byte[] processedBytes = BuildProcessedGifBytes();
                _previewUndoStack.Push(_processedGifData);
                _processedGifData = processedBytes;
                SetProcessedPreviewFromBytes(_processedGifData);
                BtnUndoPreview.IsEnabled = _previewUndoStack.Count > 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error removing GIF background: " + ex.Message, "GIF Background Remover", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            ResetButton();
        }

        private void ResetButton()
        {
            BtnRemoveBackground.IsEnabled = true;
            BtnRemoveBackground.Content = "Remove";
        }

        private static MagickImageCollection CreateProcessedCollection(
            MagickImageCollection sourceCollection,
            int targetWidth,
            int targetHeight,
            List<(byte R, byte G, byte B)> colorsToRemove,
            int fuzzPercent)
        {
            var processedCollection = new MagickImageCollection();
            for (int i = 0; i < sourceCollection.Count; i++)
            {
                var frame = new MagickImage(sourceCollection[i]);
                frame.Alpha(AlphaOption.Set);
                frame.BackgroundColor = MagickColors.Transparent;
                if (frame.Width != targetWidth || frame.Height != targetHeight)
                {
                    frame.Resize(new MagickGeometry((uint)targetWidth, (uint)targetHeight));
                    frame.Page = new MagickGeometry(0, 0, (uint)targetWidth, (uint)targetHeight);
                }

                RemoveBorderConnectedColors(frame, colorsToRemove, fuzzPercent);
                processedCollection.Add(frame);
            }

            return processedCollection;
        }

        private byte[] BuildProcessedGifBytes()
        {
            using var collection = new MagickImageCollection();
            collection.Read(_currentGifPath);
            if (collection.Count == 0)
            {
                throw new InvalidOperationException("The selected GIF could not be read.");
            }

            collection.Coalesce();
            int targetWidth = (int)Math.Max(collection.Max(f => f.Width), 1);
            int targetHeight = (int)Math.Max(collection.Max(f => f.Height), 1);
            int fuzzPercent = GetFuzzPercent();
            List<(byte R, byte G, byte B)> colorsToRemove = BuildColorsToRemove(collection);
            if (colorsToRemove.Count == 0)
            {
                throw new InvalidOperationException("Please enable auto mode or add at least one manual color.");
            }

            using var processedCollection = CreateProcessedCollection(collection, targetWidth, targetHeight, colorsToRemove, fuzzPercent);
            processedCollection[0].AnimationIterations = 0;
            processedCollection.Optimize();

            using var outputStream = new MemoryStream();
            processedCollection.Write(outputStream, MagickFormat.Gif);
            return outputStream.ToArray();
        }

        private int GetFuzzPercent()
        {
            if (int.TryParse(TxtFuzzPercent.Text, out int fuzz))
            {
                return Math.Max(0, Math.Min(100, fuzz));
            }

            return 10;
        }

        private List<(byte R, byte G, byte B)> BuildColorsToRemove(MagickImageCollection collection)
        {
            List<(byte R, byte G, byte B)> colors = new();
            HashSet<string> unique = new(StringComparer.OrdinalIgnoreCase);
            List<(byte R, byte G, byte B)> selectedManualColors = GetSelectedManualColors();
            if (selectedManualColors.Count > 0)
            {
                foreach (var selectedColor in selectedManualColors)
                {
                    AddColorIfNew(colors, unique, selectedColor.R, selectedColor.G, selectedColor.B);
                }

                return colors;
            }

            if (_manualColors.Count > 0)
            {
                foreach (var color in _manualColors)
                {
                    AddColorIfNew(colors, unique, color.R, color.G, color.B);
                }

                return colors;
            }

            if (ChkAutoDetect.IsChecked == true)
            {
                IMagickColor<byte>? detected = GetBackgroundColorFromGif(collection) ?? collection[0].GetPixels().GetPixel(0, 0).ToColor();
                if (detected is not null)
                {
                    AddColorIfNew(colors, unique, detected.R, detected.G, detected.B);
                }
            }

            return colors;
        }

        private List<(byte R, byte G, byte B)> GetSelectedManualColors()
        {
            List<(byte R, byte G, byte B)> selectedColors = new();
            if (LstColors is null || LstColors.SelectedItems.Count == 0)
            {
                return selectedColors;
            }

            foreach (var item in LstColors.SelectedItems)
            {
                string? itemText = item?.ToString();
                if (string.IsNullOrWhiteSpace(itemText))
                {
                    continue;
                }

                string hex = itemText.Trim().TrimStart('#');
                if (hex.Length != 6)
                {
                    continue;
                }

                if (byte.TryParse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber, null, out byte r) &&
                    byte.TryParse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber, null, out byte g) &&
                    byte.TryParse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber, null, out byte b))
                {
                    selectedColors.Add((r, g, b));
                }
            }

            return selectedColors;
        }

        private static void AddColorIfNew(List<(byte R, byte G, byte B)> colors, HashSet<string> unique, byte r, byte g, byte b)
        {
            string key = $"{r:X2}{g:X2}{b:X2}";
            if (!unique.Add(key))
            {
                return;
            }

            colors.Add((r, g, b));
        }

        private static void RemoveBorderConnectedColors(MagickImage frame, List<(byte R, byte G, byte B)> colorsToRemove, int fuzzPercent)
        {
            int width = (int)frame.Width;
            int height = (int)frame.Height;
            int stride = width * 4;
            int tolerance = Math.Max(0, Math.Min(64, fuzzPercent));
            var pixelCollection = frame.GetPixels();
            if (pixelCollection is null)
            {
                return;
            }
            byte[] pixels = pixelCollection.ToByteArray(PixelMapping.BGRA) ?? Array.Empty<byte>();
            if (pixels.Length < stride * height)
            {
                return;
            }
            bool[] visited = new bool[width * height];
            Queue<(int X, int Y)> queue = new Queue<(int X, int Y)>();

            for (int x = 0; x < width; x++)
            {
                EnqueueIfMatch(x, 0);
                EnqueueIfMatch(x, height - 1);
            }

            for (int y = 1; y < height - 1; y++)
            {
                EnqueueIfMatch(0, y);
                EnqueueIfMatch(width - 1, y);
            }

            while (queue.Count > 0)
            {
                var point = queue.Dequeue();
                int pixelOffset = (point.Y * stride) + (point.X * 4);
                pixels[pixelOffset + 3] = 0;
                pixelCollection.SetPixel(point.X, point.Y, new byte[] { pixels[pixelOffset], pixels[pixelOffset + 1], pixels[pixelOffset + 2], 0 });

                TryNeighbor(point.X - 1, point.Y);
                TryNeighbor(point.X + 1, point.Y);
                TryNeighbor(point.X, point.Y - 1);
                TryNeighbor(point.X, point.Y + 1);
            }
            return;

            void TryNeighbor(int x, int y)
            {
                if (x < 0 || y < 0 || x >= width || y >= height)
                {
                    return;
                }

                int index = y * width + x;
                if (visited[index])
                {
                    return;
                }

                int offset = (y * stride) + (x * 4);
                if (pixels[offset + 3] == 0)
                {
                    visited[index] = true;
                    return;
                }

                if (!MatchesAnyTargetColor(pixels[offset + 2], pixels[offset + 1], pixels[offset], colorsToRemove, tolerance))
                {
                    visited[index] = true;
                    return;
                }

                visited[index] = true;
                queue.Enqueue((x, y));
            }

            void EnqueueIfMatch(int x, int y)
            {
                int index = y * width + x;
                if (visited[index])
                {
                    return;
                }

                int offset = (y * stride) + (x * 4);
                if (pixels[offset + 3] == 0)
                {
                    visited[index] = true;
                    return;
                }

                if (!MatchesAnyTargetColor(pixels[offset + 2], pixels[offset + 1], pixels[offset], colorsToRemove, tolerance))
                {
                    visited[index] = true;
                    return;
                }

                visited[index] = true;
                queue.Enqueue((x, y));
            }
        }

        private static bool MatchesAnyTargetColor(byte r, byte g, byte b, List<(byte R, byte G, byte B)> colorsToRemove, int tolerance)
        {
            for (int i = 0; i < colorsToRemove.Count; i++)
            {
                var target = colorsToRemove[i];
                if (Math.Abs(r - target.R) <= tolerance &&
                    Math.Abs(g - target.G) <= tolerance &&
                    Math.Abs(b - target.B) <= tolerance)
                {
                    return true;
                }
            }

            return false;
        }

        private static IMagickColor<byte>? GetBackgroundColorFromGif(MagickImageCollection collection)
        {
            var colorCounts = new Dictionary<string, (int Count, IMagickColor<byte> Color)>();
            foreach (MagickImage frame in collection)
            {
                AddBorderSamples(colorCounts, frame);
            }

            var best = colorCounts.Values.OrderByDescending(v => v.Count).FirstOrDefault();
            if (best.Count == 0 || best.Color is null)
            {
                return null;
            }

            return best.Color;
        }

        private static void AddBorderSamples(Dictionary<string, (int Count, IMagickColor<byte> Color)> colorCounts, MagickImage frame)
        {
            int width = (int)frame.Width;
            int height = (int)frame.Height;
            int step = Math.Max(1, Math.Min(width, height) / 12);

            for (int x = 0; x < width; x += step)
            {
                AddBorderSample(colorCounts, frame, x, 0);
                AddBorderSample(colorCounts, frame, x, height - 1);
            }
            for (int y = 0; y < height; y += step)
            {
                AddBorderSample(colorCounts, frame, 0, y);
                AddBorderSample(colorCounts, frame, width - 1, y);
            }

            AddBorderSample(colorCounts, frame, 0, 0);
            AddBorderSample(colorCounts, frame, width - 1, 0);
            AddBorderSample(colorCounts, frame, 0, height - 1);
            AddBorderSample(colorCounts, frame, width - 1, height - 1);
        }

        private static void AddBorderSample(Dictionary<string, (int Count, IMagickColor<byte> Color)> colorCounts, MagickImage frame, int x, int y)
        {
            if (x < 0 || x >= frame.Width || y < 0 || y >= frame.Height) return;

            var pixel = frame.GetPixels().GetPixel(x, y);
            if (pixel is null) return;

            var color = pixel.ToColor();
            if (color is null || color.A < 200) return;

            string key = $"{color.R / 8},{color.G / 8},{color.B / 8}";
            if (!colorCounts.TryGetValue(key, out var existing))
            {
                colorCounts[key] = (1, color);
            }
            else
            {
                colorCounts[key] = (existing.Count + 1, existing.Color);
            }
        }

        private void BtnAddColor_Click(object sender, RoutedEventArgs e)
        {
            using var colorDialog = new WF.ColorDialog
            {
                AllowFullOpen = true,
                FullOpen = true
            };

            if (colorDialog.ShowDialog() != WF.DialogResult.OK)
            {
                return;
            }

            byte r = colorDialog.Color.R;
            byte g = colorDialog.Color.G;
            byte b = colorDialog.Color.B;
            AddManualColor(r, g, b);
        }

        private void BtnRemoveColor_Click(object sender, RoutedEventArgs e)
        {
            int selectedIndex = LstColors.SelectedIndex;
            if (selectedIndex < 0 || selectedIndex >= _manualColors.Count)
            {
                return;
            }

            _manualColors.RemoveAt(selectedIndex);
            LstColors.Items.RemoveAt(selectedIndex);
        }

        private void BtnClearColors_Click(object sender, RoutedEventArgs e)
        {
            _manualColors.Clear();
            LstColors.Items.Clear();
            TxtPickedColor.Text = "Last selected: none";
        }

        private void InitializePreview()
        {
            try
            {
                ImgOriginalPreview.Source = new BitmapImage(new Uri(_currentGifPath, UriKind.Absolute));
                using var collection = new MagickImageCollection();
                collection.Read(_currentGifPath);
                if (collection.Count == 0)
                {
                    TxtPreviewHint.Visibility = Visibility.Visible;
                    return;
                }

                using var firstFrame = new MagickImage(collection[0]);
                _previewWidth = (int)firstFrame.Width;
                _previewHeight = (int)firstFrame.Height;
                _previewStride = _previewWidth * 4;
                _previewPixels = firstFrame.GetPixels().ToByteArray(PixelMapping.BGRA);
                _processedGifData = null;
                ImgProcessedPreview.Source = ImgOriginalPreview.Source;
                BtnUndoPreview.IsEnabled = false;
                TxtPreviewHint.Visibility = Visibility.Collapsed;
            }
            catch
            {
                _previewPixels = null;
                _previewWidth = 0;
                _previewHeight = 0;
                _previewStride = 0;
                _processedGifData = null;
                ImgOriginalPreview.Source = null;
                ImgProcessedPreview.Source = null;
                BtnUndoPreview.IsEnabled = false;
                TxtPreviewHint.Visibility = Visibility.Visible;
            }
        }

        private void ImgPreview_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_previewPixels is null || _previewWidth <= 0 || _previewHeight <= 0 || ImgOriginalPreview.ActualWidth <= 0 || ImgOriginalPreview.ActualHeight <= 0)
            {
                return;
            }

            Point mousePoint = e.GetPosition(ImgOriginalPreview);
            if (!TryMapToPixel(mousePoint, out int px, out int py))
            {
                return;
            }

            int pixelIndex = (py * _previewStride) + (px * 4);
            if (pixelIndex < 0 || pixelIndex + 2 >= _previewPixels.Length)
            {
                return;
            }

            byte b = _previewPixels[pixelIndex];
            byte g = _previewPixels[pixelIndex + 1];
            byte r = _previewPixels[pixelIndex + 2];
            AddManualColor(r, g, b);
        }

        private bool TryMapToPixel(Point point, out int x, out int y)
        {
            x = 0;
            y = 0;

            double controlWidth = ImgOriginalPreview.ActualWidth;
            double controlHeight = ImgOriginalPreview.ActualHeight;
            if (controlWidth <= 0 || controlHeight <= 0)
            {
                return false;
            }

            double imageAspect = (double)_previewWidth / _previewHeight;
            double controlAspect = controlWidth / controlHeight;
            double renderedWidth;
            double renderedHeight;
            double offsetX;
            double offsetY;

            if (imageAspect > controlAspect)
            {
                renderedWidth = controlWidth;
                renderedHeight = controlWidth / imageAspect;
                offsetX = 0;
                offsetY = (controlHeight - renderedHeight) * 0.5;
            }
            else
            {
                renderedHeight = controlHeight;
                renderedWidth = controlHeight * imageAspect;
                offsetX = (controlWidth - renderedWidth) * 0.5;
                offsetY = 0;
            }

            if (point.X < offsetX || point.X > offsetX + renderedWidth || point.Y < offsetY || point.Y > offsetY + renderedHeight)
            {
                return false;
            }

            double normalizedX = (point.X - offsetX) / renderedWidth;
            double normalizedY = (point.Y - offsetY) / renderedHeight;
            x = Math.Max(0, Math.Min(_previewWidth - 1, (int)(normalizedX * _previewWidth)));
            y = Math.Max(0, Math.Min(_previewHeight - 1, (int)(normalizedY * _previewHeight)));
            return true;
        }

        private void AddManualColor(byte r, byte g, byte b)
        {
            if (_manualColors.Any(c => c.R == r && c.G == g && c.B == b))
            {
                return;
            }

            _manualColors.Add((r, g, b));
            string itemText = $"#{r:X2}{g:X2}{b:X2}";
            LstColors.Items.Add(itemText);
            LstColors.SelectedIndex = LstColors.Items.Count - 1;
            TxtPickedColor.Text = $"Last selected: {itemText}";
        }

        private void BtnUndoPreview_Click(object sender, RoutedEventArgs e)
        {
            if (_previewUndoStack.Count == 0)
            {
                return;
            }

            _processedGifData = _previewUndoStack.Pop();
            SetProcessedPreviewFromBytes(_processedGifData);
            BtnUndoPreview.IsEnabled = _previewUndoStack.Count > 0;
        }

        private void BtnSaveResult_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                byte[] bytesToSave = _processedGifData ?? BuildProcessedGifBytes();
                string outputPath = Path.Combine(_libraryPath, $"cleaned_{Path.GetFileNameWithoutExtension(_currentGifPath)}.gif");
                File.WriteAllBytes(outputPath, bytesToSave);
                MessageBox.Show($"Saved as:\n{Path.GetFileName(outputPath)}", "GIF Background Remover", MessageBoxButton.OK, MessageBoxImage.Information);
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error saving GIF: " + ex.Message, "GIF Background Remover", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SetProcessedPreviewFromBytes(byte[]? gifBytes)
        {
            if (ImgProcessedPreview is null || ImgOriginalPreview is null || TxtPreviewHint is null)
            {
                return;
            }

            if (gifBytes is null || gifBytes.Length == 0)
            {
                ImgProcessedPreview.Source = ImgOriginalPreview.Source;
                TxtPreviewHint.Visibility = Visibility.Collapsed;
                return;
            }

            try
            {
                using MemoryStream gifStream = new MemoryStream(gifBytes);
                BitmapImage previewImage = new BitmapImage();
                previewImage.BeginInit();
                previewImage.CacheOption = BitmapCacheOption.OnLoad;
                previewImage.StreamSource = gifStream;
                previewImage.EndInit();
                previewImage.Freeze();

                ImgProcessedPreview.Source = previewImage;
                TxtPreviewHint.Visibility = Visibility.Collapsed;
            }
            catch
            {
                ImgProcessedPreview.Source = null;
                TxtPreviewHint.Visibility = Visibility.Visible;
            }
        }
    }
}