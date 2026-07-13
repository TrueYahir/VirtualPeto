using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using ImageMagick; 

namespace VirtualPeto.Tools
{
    public partial class GifBgRemoverWindow : Window
    {
        private string _currentGifPath;
        private string _libraryPath;

        public GifBgRemoverWindow(string gifPath, string libraryPath)
        {
            InitializeComponent();
            _currentGifPath = gifPath;
            _libraryPath = libraryPath;
            
            TxtFileName.Text = Path.GetFileName(_currentGifPath);
        }

        private void BtnRemoveBackground_Click(object sender, RoutedEventArgs e)
        {
            BtnRemoveBackground.IsEnabled = false;
            BtnRemoveBackground.Content = "Processing...";

            try
            {
                using var collection = new MagickImageCollection();
                collection.Read(_currentGifPath);

                if (collection.Count == 0)
                {
                    MessageBox.Show("The selected GIF could not be read.", "GIF Background Remover", MessageBoxButton.OK, MessageBoxImage.Warning);
                    ResetButton();
                    return;
                }

                collection.Coalesce();

                int targetWidth = (int)Math.Max(collection.Max(f => f.Width), 1);
                int targetHeight = (int)Math.Max(collection.Max(f => f.Height), 1);

                var processedCollection = new MagickImageCollection();
                IMagickColor<byte> backgroundColor = GetBackgroundColorFromGif(collection)
                                      ?? collection[0].GetPixels().GetPixel(0, 0).ToColor()!;
                int fuzzPercent = 10;

                for (int i = 0; i < collection.Count; i++)
                {
                    var frame = new MagickImage(collection[i]);
                    frame.Alpha(AlphaOption.Set);
                    frame.ColorFuzz = new Percentage(fuzzPercent);
                    frame.BackgroundColor = MagickColors.Transparent;
                    if (frame.Width != targetWidth || frame.Height != targetHeight)
                    {
                        frame.Resize(new MagickGeometry((uint)targetWidth, (uint)targetHeight));
                        frame.Page = new MagickGeometry(0, 0, (uint)targetWidth, (uint)targetHeight);
                    }

                    RemoveBorderBackground(frame, backgroundColor, fuzzPercent);
                    processedCollection.Add(frame);
                }

                processedCollection[0].AnimationIterations = 0;
                processedCollection.Optimize();

                string outputPath = Path.Combine(_libraryPath, $"cleaned_{Path.GetFileNameWithoutExtension(_currentGifPath)}.gif");
                processedCollection.Write(outputPath);

                MessageBox.Show($"GIF background removed and saved as:\n{Path.GetFileName(outputPath)}", "GIF Background Remover", MessageBoxButton.OK, MessageBoxImage.Information);
                this.Close(); 
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error removing GIF background: " + ex.Message, "GIF Background Remover", MessageBoxButton.OK, MessageBoxImage.Error);
                ResetButton();
            }
        }

        private void ResetButton()
        {
            BtnRemoveBackground.IsEnabled = true;
            BtnRemoveBackground.Content = "Remove Background & Save";
        }

        private static void RemoveBorderBackground(MagickImage frame, IMagickColor<byte> backgroundColor, int fuzzPercent)
        {
            frame.Alpha(AlphaOption.Set);
            frame.VirtualPixelMethod = VirtualPixelMethod.Transparent;
            frame.ColorFuzz = new Percentage(fuzzPercent);
            frame.Transparent(backgroundColor);
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

        private static bool IsColorClose(IMagickColor<byte> color, IMagickColor<byte> backgroundColor, int tolerance)
        {
            return Math.Abs(color.R - backgroundColor.R) <= tolerance &&
                   Math.Abs(color.G - backgroundColor.G) <= tolerance &&
                   Math.Abs(color.B - backgroundColor.B) <= tolerance;
        }
    }
}