using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace VirtualPeto.Tools
{
    internal class BackgroundEditorWindow : Window
    {
        private string originalPath;
        private string libraryPath;
        private WriteableBitmap editableBitmap = null!;
        private byte[] originalPixels = null!;
        private int width, height, stride;
        private Stack<byte[]> undoStack = new Stack<byte[]>();
        private bool isDrawing = false;
        private Image imgEditor = null!;
        private Slider sldTolerance = null!;
        private Slider sldBrushSize = null!;
        private RadioButton rbMagic = null!;
        private RadioButton rbErase = null!;
        private RadioButton rbRestore = null!;
        private Button btnUndo = null!;

        public BackgroundEditorWindow(string imagePath, string saveDirectory)
        {
            originalPath = imagePath;
            libraryPath = saveDirectory;

            Title = $"Editor de Fondo: {Path.GetFileName(imagePath)}";
            Width = 1000;
            Height = 700;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Background = new SolidColorBrush(Color.FromRgb(15, 15, 20));
            Foreground = Brushes.White;

            SetupUI();
            InitializeImage();
        }

        private void InitializeImage()
        {
            try
            {
                BitmapImage inputImage = new BitmapImage(new Uri(originalPath));
                FormatConvertedBitmap converted = new FormatConvertedBitmap(inputImage, PixelFormats.Bgra32, null, 0);

                width = converted.PixelWidth;
                height = converted.PixelHeight;
                stride = width * 4;
                originalPixels = new byte[height * stride];
                converted.CopyPixels(originalPixels, stride, 0);

                editableBitmap = new WriteableBitmap(width, height, converted.DpiX, converted.DpiY, PixelFormats.Bgra32, null);
                editableBitmap.WritePixels(new Int32Rect(0, 0, width, height), (byte[])originalPixels.Clone(), stride, 0);

                imgEditor.Source = editableBitmap;
            }
            catch (Exception ex) 
            { 
                MessageBox.Show("Error loading image for editor: " + ex.Message); 
                Close(); 
            }
        }

        private void SetupUI()
        {
            Grid mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            Content = mainGrid;

            ScrollViewer scrollViewer = new ScrollViewer
            {
                Background = CreateCheckerboardBrush(),
                Margin = new Thickness(10),
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };
            mainGrid.Children.Add(scrollViewer);

            Grid centerGrid = new Grid { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Background = CreateCheckerboardBrush() };
            scrollViewer.Content = centerGrid;

            imgEditor = new Image { Stretch = Stretch.Uniform };
            centerGrid.Children.Add(imgEditor);

            ScaleTransform zoomTransform = new ScaleTransform(1.0, 1.0);
            centerGrid.LayoutTransform = zoomTransform;

            scrollViewer.PreviewMouseWheel += (s, e) =>
            {
                e.Handled = true; 
                double zoomFactor = e.Delta > 0 ? 1.2 : 1 / 1.2;
                zoomTransform.ScaleX = Math.Max(0.1, Math.Min(20.0, zoomTransform.ScaleX * zoomFactor));
                zoomTransform.ScaleY = Math.Max(0.1, Math.Min(20.0, zoomTransform.ScaleY * zoomFactor));
            };

            imgEditor.PreviewMouseLeftButtonDown += ImgEditor_MouseDown;
            imgEditor.PreviewMouseMove += ImgEditor_MouseMove;
            imgEditor.PreviewMouseLeftButtonUp += ImgEditor_MouseUp;

            Border toolBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(26, 26, 36)),
                Padding = new Thickness(15),
                BorderThickness = new Thickness(0, 1, 0, 0),
                BorderBrush = new SolidColorBrush(Color.FromRgb(51, 51, 51))
            };
            Grid.SetRow(toolBorder, 1);
            mainGrid.Children.Add(toolBorder);

            StackPanel toolStack = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };
            toolBorder.Child = toolStack;

            btnUndo = new Button { Content = "↩ Undo", IsEnabled = false, Padding = new Thickness(10, 5, 10, 5), Margin = new Thickness(0, 0, 15, 0), Foreground = Brushes.Black };
            btnUndo.Click += (s, e) => UndoState();
            toolStack.Children.Add(btnUndo);

            rbMagic = new RadioButton { Content = "Magic Wand", IsChecked = true, Foreground = Brushes.White, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 15, 0) };
            rbErase = new RadioButton { Content = "Eraser", Foreground = Brushes.White, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 15, 0) };
            rbRestore = new RadioButton { Content = "Restore", Foreground = Brushes.White, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 25, 0) };
            toolStack.Children.Add(rbMagic);
            toolStack.Children.Add(rbErase);
            toolStack.Children.Add(rbRestore);

            rbMagic.Checked += (s, e) => { Mouse.OverrideCursor = Cursors.Hand; };
            rbErase.Checked += (s, e) => { Mouse.OverrideCursor = Cursors.Cross; };
            rbRestore.Checked += (s, e) => { Mouse.OverrideCursor = Cursors.UpArrow; };

            toolStack.Children.Add(new TextBlock { Text = "Tol:", Foreground = Brushes.White, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 5, 0) });
            sldTolerance = new Slider { Minimum = 0, Maximum = 255, Value = 35, Width = 80, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 15, 0) };
            toolStack.Children.Add(sldTolerance);

            toolStack.Children.Add(new TextBlock { Text = "Size:", Foreground = Brushes.White, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 5, 0) });
            sldBrushSize = new Slider { Minimum = 1, Maximum = 100, Value = 15, Width = 80, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 30, 0) };
            toolStack.Children.Add(sldBrushSize);

            Button btnSave = new Button
            {
                Content = "SAVE CLEAN IMAGE",
                Background = new SolidColorBrush(Color.FromRgb(44, 58, 44)),
                Foreground = Brushes.White,
                Padding = new Thickness(15, 8, 15, 8),
                FontWeight = FontWeights.Bold
            };
            btnSave.Click += BtnSave_Click;
            toolStack.Children.Add(btnSave);
        }

        private static Brush CreateCheckerboardBrush()
        {
            DrawingBrush brush = new DrawingBrush
            {
                TileMode = TileMode.Tile,
                Viewport = new Rect(0, 0, 16, 16),
                ViewportUnits = BrushMappingMode.Absolute,
                Stretch = Stretch.Fill
            };

            DrawingGroup group = new DrawingGroup();
            group.Children.Add(new GeometryDrawing(Brushes.White, null, new RectangleGeometry(new Rect(0, 0, 16, 16))));
            group.Children.Add(new GeometryDrawing(Brushes.LightGray, null, new RectangleGeometry(new Rect(0, 0, 8, 8))));
            group.Children.Add(new GeometryDrawing(Brushes.LightGray, null, new RectangleGeometry(new Rect(8, 8, 16, 16))));
            brush.Drawing = group;
            return brush;
        }

        private void SaveState()
        {
            byte[] state = new byte[height * stride];
            editableBitmap.CopyPixels(state, stride, 0);
            undoStack.Push(state);
            btnUndo.IsEnabled = true;
        }

        private void UndoState()
        {
            if (undoStack.Count > 0)
            {
                byte[] previousState = undoStack.Pop();
                editableBitmap.WritePixels(new Int32Rect(0, 0, width, height), previousState, stride, 0);
                if (undoStack.Count == 0) btnUndo.IsEnabled = false;
            }
        }

        private void GetBitmapCoordinates(System.Windows.Point pos, out int bitmapX, out int bitmapY)
        {
            double scaleX = width / imgEditor.ActualWidth;
            double scaleY = height / imgEditor.ActualHeight;

            bitmapX = (int)(pos.X * scaleX);
            bitmapY = (int)(pos.Y * scaleY);
        }

        private void ImgEditor_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            GetBitmapCoordinates(e.GetPosition(imgEditor), out int x, out int y);
            if (x < 0 || x >= width || y < 0 || y >= height) return;

            SaveState();

            if (rbMagic.IsChecked == true)
            {
                ApplyFloodFill(x, y);
            }
            else
            {
                isDrawing = true;
                ApplyBrush(x, y);
            }
        }

        private void ImgEditor_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (!isDrawing || rbMagic.IsChecked == true) return;

            GetBitmapCoordinates(e.GetPosition(imgEditor), out int x, out int y);
            ApplyBrush(x, y);
        }

        private void ImgEditor_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            isDrawing = false;
        }

        // Pincels
        private void ApplyBrush(int cx, int cy)
        {
            int brushRadius = (int)sldBrushSize.Value;
            int radiusSq = brushRadius * brushRadius;

            byte[] currentPixels = new byte[height * stride];
            editableBitmap.CopyPixels(currentPixels, stride, 0);

            bool isErase = rbErase.IsChecked == true;
            bool changed = false;

            for (int y = Math.Max(0, cy - brushRadius); y <= Math.Min(height - 1, cy + brushRadius); y++)
            {
                for (int x = Math.Max(0, cx - brushRadius); x <= Math.Min(width - 1, cx + brushRadius); x++)
                {
                    int dx = x - cx;
                    int dy = y - cy;
                    
                    if (dx * dx + dy * dy <= radiusSq)
                    {
                        int index = y * stride + x * 4;
                        
                        if (isErase && currentPixels[index + 3] > 0)
                        {
                            currentPixels[index + 3] = 0; 
                            changed = true;
                        }
                        else if (!isErase && currentPixels[index + 3] == 0)
                        {
                            currentPixels[index] = originalPixels[index];
                            currentPixels[index + 1] = originalPixels[index + 1];
                            currentPixels[index + 2] = originalPixels[index + 2];
                            currentPixels[index + 3] = originalPixels[index + 3];
                            changed = true;
                        }
                    }
                }
            }

            if (changed)
            {
                editableBitmap.WritePixels(new Int32Rect(0, 0, width, height), currentPixels, stride, 0);
            }
        }

        //Magic pen
        private void ApplyFloodFill(int startX, int startY)
        {
            try
            {
                byte[] currentPixels = new byte[height * stride];
                editableBitmap.CopyPixels(currentPixels, stride, 0);

                int index = startY * stride + startX * 4;
                if (currentPixels[index + 3] == 0) return;

                byte targetB = currentPixels[index];
                byte targetG = currentPixels[index + 1];
                byte targetR = currentPixels[index + 2];

                int tolerance = Math.Max(8, (int)sldTolerance.Value);

                Queue<IntPoint> queue = new Queue<IntPoint>();
                HashSet<(int X, int Y)> visited = new HashSet<(int X, int Y)>();
                queue.Enqueue(new IntPoint(startX, startY));
                visited.Add((startX, startY));

                List<IntPoint> region = new List<IntPoint>();
                bool touchesBorder = false;

                while (queue.Count > 0)
                {
                    IntPoint p = queue.Dequeue();
                    int currentIndex = p.Y * stride + p.X * 4;

                    if (currentPixels[currentIndex + 3] == 0) continue;

                    region.Add(p);

                    if (p.X == 0 || p.X == width - 1 || p.Y == 0 || p.Y == height - 1)
                    {
                        touchesBorder = true;
                    }

                    int[] dx = { 0, 0, -1, 1 };
                    int[] dy = { -1, 1, 0, 0 };

                    for (int i = 0; i < 4; i++)
                    {
                        int nx = p.X + dx[i];
                        int ny = p.Y + dy[i];

                        if (nx < 0 || nx >= width || ny < 0 || ny >= height) continue;

                        var key = (nx, ny);
                        if (visited.Contains(key)) continue;

                        int neighborIndex = ny * stride + nx * 4;
                        if (currentPixels[neighborIndex + 3] == 0) continue;

                        byte b = currentPixels[neighborIndex];
                        byte g = currentPixels[neighborIndex + 1];
                        byte r = currentPixels[neighborIndex + 2];

                        double distance = Math.Sqrt(Math.Pow(b - targetB, 2) + Math.Pow(g - targetG, 2) + Math.Pow(r - targetR, 2));
                        if (distance <= tolerance)
                        {
                            visited.Add(key);
                            queue.Enqueue(new IntPoint(nx, ny));
                        }
                    }
                }

                if (!touchesBorder)
                {
                    MessageBox.Show("The magic wand only removes background regions connected to the image border. Try clicking a background area near the edges.", "Tip", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                foreach (var point in region)
                {
                    int pixelIndex = point.Y * stride + point.X * 4;
                    currentPixels[pixelIndex + 3] = 0;
                }

                editableBitmap.WritePixels(new Int32Rect(0, 0, width, height), currentPixels, stride, 0);
            }
            catch (Exception ex) 
            { 
                MessageBox.Show("Error applying magic wand: " + ex.Message); 
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                byte[] cleanPixels = new byte[height * stride];
                editableBitmap.CopyPixels(cleanPixels, stride, 0);

                WriteableBitmap finalBitmap = new WriteableBitmap(width, height, editableBitmap.DpiX, editableBitmap.DpiY, PixelFormats.Bgra32, null);
                finalBitmap.WritePixels(new Int32Rect(0, 0, width, height), cleanPixels, stride, 0);

                string newFileName = "cleaned_" + Path.GetFileNameWithoutExtension(originalPath) + ".png";
                string outputPath = Path.Combine(libraryPath, newFileName);

                using (FileStream stream = new FileStream(outputPath, FileMode.Create))
                {
                    PngBitmapEncoder encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(finalBitmap));
                    encoder.Save(stream);
                }

                MessageBox.Show($"Clean image successfully saved.\nSaved into your Library as:\n{newFileName}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                Close();
            }
            catch (Exception ex) 
            { 
                MessageBox.Show("Error saving cleaned image: " + ex.Message); 
            }
        }

        private struct IntPoint
        {
            public int X { get; }
            public int Y { get; }

            public IntPoint(int x, int y)
            {
                X = x;
                Y = y;
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            Mouse.OverrideCursor = null;
            base.OnClosed(e);
        }
    }
}