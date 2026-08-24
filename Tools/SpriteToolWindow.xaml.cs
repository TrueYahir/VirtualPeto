using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace VirtualPeto.Tools
{
    public class SpriteBox
    {
        public System.Drawing.Rectangle Bounds { get; set; }
        public System.Windows.Shapes.Rectangle VisualRect { get; set; } = new System.Windows.Shapes.Rectangle();
        public bool IsSelected { get; set; }
    }

    public partial class SpriteToolWindow : Window
    {
        private string loadedFilePath = string.Empty;
        private Bitmap? sourceBitmap;
        private List<SpriteBox> detectedSprites = new List<SpriteBox>();

        private Stack<List<System.Drawing.Rectangle>> undoStack = new Stack<List<System.Drawing.Rectangle>>();
        private Stack<List<System.Drawing.Rectangle>> redoStack = new Stack<List<System.Drawing.Rectangle>>();
        private double zoomLevel = 1.0;

        private readonly SolidColorBrush unselectedColor = new SolidColorBrush(System.Windows.Media.Color.FromArgb(150, 74, 144, 226));
        private readonly SolidColorBrush selectedColor = new SolidColorBrush(System.Windows.Media.Color.FromArgb(200, 255, 255, 255));
        private readonly SolidColorBrush selectedFill = new SolidColorBrush(System.Windows.Media.Color.FromArgb(50, 74, 144, 226));

        public SpriteToolWindow()
        {
            InitializeComponent();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (e.Key == Key.Z) BtnUndo_Click(this, new RoutedEventArgs());
                if (e.Key == Key.Y) BtnRedo_Click(this, new RoutedEventArgs());
            }
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

        private void BtnLoadSheet_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog
            {
                Title = "Select Spritesheet",
                Filter = "Image Files|*.png;*.bmp;*.gif"
            };

            if (ofd.ShowDialog() == true)
            {
                loadedFilePath = ofd.FileName;
                undoStack.Clear();
                redoStack.Clear();
                zoomLevel = 1.0;
                stZoom.ScaleX = zoomLevel;
                stZoom.ScaleY = zoomLevel;
                LoadAndAnalyzeSheet();
            }
        }

        private void LoadAndAnalyzeSheet()
        {
            if (sourceBitmap != null)
            {
                sourceBitmap.Dispose();
                sourceBitmap = null;
            }

            detectedSprites.Clear();
            CnvOverlay.Children.Clear();

            sourceBitmap = new Bitmap(loadedFilePath);
            
            BitmapImage bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.UriSource = new Uri(loadedFilePath);
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.EndInit();

            ImgPreview.Source = bitmapImage;
            ImgPreview.Width = sourceBitmap.Width;
            ImgPreview.Height = sourceBitmap.Height;
            CnvOverlay.Width = sourceBitmap.Width;
            CnvOverlay.Height = sourceBitmap.Height;

            int width = sourceBitmap.Width;
            int height = sourceBitmap.Height;
            bool[,] visited = new bool[width, height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (!visited[x, y] && sourceBitmap.GetPixel(x, y).A > 0)
                    {
                        System.Drawing.Rectangle bounds = FindSpriteBounds(sourceBitmap, visited, x, y, width, height);
                        CreateInteractiveBox(bounds, false);
                    }
                    visited[x, y] = true;
                }
            }

            SaveStateToUndo();
            UpdateStatusText();
        }

        private void SaveStateToUndo()
        {
            List<System.Drawing.Rectangle> currentState = detectedSprites.Select(s => s.Bounds).ToList();
            undoStack.Push(currentState);
            redoStack.Clear();
        }

        private void LoadState(List<System.Drawing.Rectangle> state)
        {
            detectedSprites.Clear();
            CnvOverlay.Children.Clear();

            foreach (var rect in state)
            {
                CreateInteractiveBox(rect, false);
            }
            UpdateStatusText();
        }

        private void BtnUndo_Click(object sender, RoutedEventArgs e)
        {
            if (undoStack.Count > 1)
            {
                redoStack.Push(undoStack.Pop());
                LoadState(undoStack.Peek());
            }
        }

        private void BtnRedo_Click(object sender, RoutedEventArgs e)
        {
            if (redoStack.Count > 0)
            {
                List<System.Drawing.Rectangle> state = redoStack.Pop();
                undoStack.Push(state);
                LoadState(state);
            }
        }

        private void BtnMergeSelected_Click(object sender, RoutedEventArgs e)
        {
            var selectedSprites = detectedSprites.Where(s => s.IsSelected).ToList();
            
            if (selectedSprites.Count < 2) return;

            int minX = selectedSprites.Min(s => s.Bounds.X);
            int minY = selectedSprites.Min(s => s.Bounds.Y);
            int maxX = selectedSprites.Max(s => s.Bounds.Right);
            int maxY = selectedSprites.Max(s => s.Bounds.Bottom);

            System.Drawing.Rectangle mergedBounds = new System.Drawing.Rectangle(minX, minY, maxX - minX, maxY - minY);

            foreach (var sprite in selectedSprites)
            {
                CnvOverlay.Children.Remove(sprite.VisualRect);
                detectedSprites.Remove(sprite);
            }

            CreateInteractiveBox(mergedBounds, true);
            SaveStateToUndo();
            UpdateStatusText();
        }

        private void CreateInteractiveBox(System.Drawing.Rectangle bounds, bool isSelected)
        {
            System.Windows.Shapes.Rectangle rect = new System.Windows.Shapes.Rectangle
            {
                Width = bounds.Width,
                Height = bounds.Height,
                Stroke = isSelected ? selectedColor : unselectedColor,
                StrokeThickness = 1,
                Fill = isSelected ? selectedFill : System.Windows.Media.Brushes.Transparent,
                Cursor = Cursors.Hand
            };

            Canvas.SetLeft(rect, bounds.X);
            Canvas.SetTop(rect, bounds.Y);

            SpriteBox spriteBox = new SpriteBox
            {
                Bounds = bounds,
                VisualRect = rect,
                IsSelected = isSelected
            };

            rect.MouseLeftButtonDown += (s, e) =>
            {
                spriteBox.IsSelected = !spriteBox.IsSelected;
                rect.Stroke = spriteBox.IsSelected ? selectedColor : unselectedColor;
                rect.Fill = spriteBox.IsSelected ? selectedFill : System.Windows.Media.Brushes.Transparent;
                UpdateStatusText();
            };

            detectedSprites.Add(spriteBox);
            CnvOverlay.Children.Add(rect);
        }

        private void UpdateStatusText()
        {
            int selectedCount = detectedSprites.Count(s => s.IsSelected);
            TxtStatus.Text = $"{detectedSprites.Count} sprites detected. {selectedCount} selected.";
        }

        private void BtnExportSelected_Click(object sender, RoutedEventArgs e)
        {
            ExportSprites(onlySelected: true);
        }

        private void BtnExportAll_Click(object sender, RoutedEventArgs e)
        {
            ExportSprites(onlySelected: false);
        }

        private void ExportSprites(bool onlySelected)
        {
            if (sourceBitmap == null || string.IsNullOrEmpty(loadedFilePath)) return;

            var spritesToExport = onlySelected ? detectedSprites.Where(s => s.IsSelected).ToList() : detectedSprites;

            if (spritesToExport.Count == 0) return;

            string directory = Path.GetDirectoryName(loadedFilePath) ?? string.Empty;
            string fileName = Path.GetFileNameWithoutExtension(loadedFilePath) ?? "sprites";
            string outputFolder = Path.Combine(directory, fileName + (onlySelected ? "_selected" : "_all"));

            try
            {
                if (!Directory.Exists(outputFolder)) Directory.CreateDirectory(outputFolder);

                int counter = 0;
                foreach (var sprite in spritesToExport)
                {
                    using (Bitmap extracted = sourceBitmap.Clone(sprite.Bounds, sourceBitmap.PixelFormat))
                    {
                        string outPath = Path.Combine(outputFolder, $"sprite_{counter:D3}.png");
                        extracted.Save(outPath, ImageFormat.Png);
                    }
                    counter++;
                }

                MessageBox.Show($"Successfully exported {counter} sprites to:\n{outputFolder}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Export failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private System.Drawing.Rectangle FindSpriteBounds(Bitmap bmp, bool[,] visited, int startX, int startY, int width, int height)
        {
            int minX = startX, maxX = startX, minY = startY, maxY = startY;
            Stack<System.Drawing.Point> stack = new Stack<System.Drawing.Point>();
            stack.Push(new System.Drawing.Point(startX, startY));

            while (stack.Count > 0)
            {
                System.Drawing.Point p = stack.Pop();
                int x = p.X;
                int y = p.Y;

                if (x < 0 || x >= width || y < 0 || y >= height || visited[x, y]) continue;

                if (bmp.GetPixel(x, y).A == 0)
                {
                    visited[x, y] = true;
                    continue;
                }

                visited[x, y] = true;

                if (x < minX) minX = x;
                if (x > maxX) maxX = x;
                if (y < minY) minY = y;
                if (y > maxY) maxY = y;

                stack.Push(new System.Drawing.Point(x + 1, y));
                stack.Push(new System.Drawing.Point(x - 1, y));
                stack.Push(new System.Drawing.Point(x, y + 1));
                stack.Push(new System.Drawing.Point(x, y - 1));
                stack.Push(new System.Drawing.Point(x + 1, y + 1));
                stack.Push(new System.Drawing.Point(x - 1, y - 1));
                stack.Push(new System.Drawing.Point(x + 1, y - 1));
                stack.Push(new System.Drawing.Point(x - 1, y + 1));
            }

            return new System.Drawing.Rectangle(minX, minY, maxX - minX + 1, maxY - minY + 1);
        }

        protected override void OnClosed(EventArgs e)
        {
            if (sourceBitmap != null)
            {
                sourceBitmap.Dispose();
            }
            base.OnClosed(e);
        }
    }
}