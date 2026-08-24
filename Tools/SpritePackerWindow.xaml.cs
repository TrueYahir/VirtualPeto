using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace VirtualPeto.Tools
{
    public partial class SpritePackerWindow : Window
    {
        private List<string> spriteFiles = new List<string>();
        private Bitmap? packedBitmap;
        private double zoomLevel = 1.0;

        public SpritePackerWindow()
        {
            InitializeComponent();
        }

        private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (e.Delta > 0) zoomLevel += 0.1;
                else zoomLevel -= 0.1;

                if (zoomLevel < 0.1) zoomLevel = 0.1;
                if (zoomLevel > 10.0) zoomLevel = 10.0;

                stZoom.ScaleX = zoomLevel;
                stZoom.ScaleY = zoomLevel;
                e.Handled = true;
            }
        }

        private void ResetZoom()
        {
            zoomLevel = 1.0;
            stZoom.ScaleX = zoomLevel;
            stZoom.ScaleY = zoomLevel;
        }

        private void BtnAddSprites_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog
            {
                Title = "Select Sprites to Pack",
                Filter = "PNG Files|*.png",
                Multiselect = true
            };

            if (ofd.ShowDialog() == true)
            {
                spriteFiles.AddRange(ofd.FileNames);
                UpdatePreview();
            }
        }

        private void BtnClearSprites_Click(object sender, RoutedEventArgs e)
        {
            spriteFiles.Clear();
            ResetZoom();
            UpdatePreview();
        }

        private void UpdatePreview()
        {
            if (packedBitmap != null)
            {
                packedBitmap.Dispose();
                packedBitmap = null;
            }

            ImgPreview.Source = null;
            TxtStatus.Text = $"{spriteFiles.Count} sprites added. Ctrl + MouseWheel to zoom.";

            if (spriteFiles.Count == 0) return;

            try
            {
                int totalWidth = 0;
                int maxHeight = 0;
                List<Bitmap> bitmaps = new List<Bitmap>();

                foreach (string file in spriteFiles)
                {
                    Bitmap bmp = new Bitmap(file);
                    bitmaps.Add(bmp);
                    totalWidth += bmp.Width;
                    if (bmp.Height > maxHeight) maxHeight = bmp.Height;
                }

                packedBitmap = new Bitmap(totalWidth, maxHeight);

                using (Graphics g = Graphics.FromImage(packedBitmap))
                {
                    int currentX = 0;
                    foreach (Bitmap bmp in bitmaps)
                    {
                        g.DrawImage(bmp, currentX, 0);
                        currentX += bmp.Width;
                        bmp.Dispose();
                    }
                }

                ImgPreview.Source = BitmapToImageSource(packedBitmap);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Preview failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private BitmapImage BitmapToImageSource(Bitmap bitmap)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                bitmap.Save(memory, ImageFormat.Png);
                memory.Position = 0;
                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                bitmapImage.Freeze();
                return bitmapImage;
            }
        }

        private void BtnExportSheet_Click(object sender, RoutedEventArgs e)
        {
            if (packedBitmap == null || spriteFiles.Count == 0)
            {
                MessageBox.Show("No sprites to export.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SaveFileDialog sfd = new SaveFileDialog
            {
                Title = "Save Packed Spritesheet",
                Filter = "PNG Image|*.png",
                FileName = "packed_spritesheet.png"
            };

            if (sfd.ShowDialog() == true)
            {
                try
                {
                    packedBitmap.Save(sfd.FileName, ImageFormat.Png);
                    MessageBox.Show("Successfully exported packed sheet!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Export failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            if (packedBitmap != null)
            {
                packedBitmap.Dispose();
            }
            base.OnClosed(e);
        }
    }
}