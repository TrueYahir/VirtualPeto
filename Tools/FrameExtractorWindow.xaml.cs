using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace VirtualPeto.Tools
{
    public partial class FrameExtractorWindow : Window
    {
        public FrameExtractorWindow()
        {
            InitializeComponent();
            if (!string.IsNullOrEmpty(SettingsManager.Current.DefaultSaveFolder) && 
                Directory.Exists(SettingsManager.Current.DefaultSaveFolder))
            {
                TxtOutputFolder.Text = SettingsManager.Current.DefaultSaveFolder;
            }
        }

        private void BtnBrowseInput_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "GIF Files|*.gif";
            ofd.Title = "Select GIF File";

            if (ofd.ShowDialog() == true)
            {
                TxtInputFile.Text = ofd.FileName;
                
                try
                {
                    GifBitmapDecoder decoder = new GifBitmapDecoder(new Uri(ofd.FileName), BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);
                    if (decoder.Frames.Count > 0)
                    {
                        ImgPreview.Source = decoder.Frames[0];
                        TxtFrameWidth.Text = decoder.Frames[0].PixelWidth.ToString();
                        TxtFrameHeight.Text = decoder.Frames[0].PixelHeight.ToString();
                    }
                }
                catch
                {
                    ImgPreview.Source = null;
                }
            }
        }

        private void BtnBrowseOutput_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.ValidateNames = false;
            ofd.CheckFileExists = false;
            ofd.CheckPathExists = true;
            ofd.FileName = "Select Folder";
            ofd.Title = "Select Output Directory";

            // --- NUEVO CÓDIGO: Iniciar el diálogo en la carpeta por defecto ---
            if (!string.IsNullOrWhiteSpace(TxtOutputFolder.Text) && Directory.Exists(TxtOutputFolder.Text))
            {
                ofd.InitialDirectory = TxtOutputFolder.Text;
            }
            else if (!string.IsNullOrEmpty(SettingsManager.Current.DefaultSaveFolder) && 
                    Directory.Exists(SettingsManager.Current.DefaultSaveFolder))
            {
                ofd.InitialDirectory = SettingsManager.Current.DefaultSaveFolder;
            }

            if (ofd.ShowDialog() == true)
            {
                TxtOutputFolder.Text = Path.GetDirectoryName(ofd.FileName);
            }
        }

        private void ChkSpriteSheet_Checked(object sender, RoutedEventArgs e)
        {
            if (PanelSpriteSettings != null)
            {
                PanelSpriteSettings.IsEnabled = true;
                PanelSpriteSettings.Opacity = 1.0;
            }
        }

        private void ChkSpriteSheet_Unchecked(object sender, RoutedEventArgs e)
        {
            if (PanelSpriteSettings != null)
            {
                PanelSpriteSettings.IsEnabled = false;
                PanelSpriteSettings.Opacity = 0.5;
            }
        }

        private void BtnExtract_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtInputFile.Text) || !File.Exists(TxtInputFile.Text))
            {
                MessageBox.Show("Please select a valid input GIF file.", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(TxtOutputFolder.Text) || !Directory.Exists(TxtOutputFolder.Text))
            {
                MessageBox.Show("Please select a valid output folder.", "Invalid Output", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (ChkIndividualFrames.IsChecked == false && ChkSpriteSheet.IsChecked == false)
            {
                MessageBox.Show("Please select at least one extraction mode.", "No Mode Selected", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                GifBitmapDecoder decoder = new GifBitmapDecoder(new Uri(TxtInputFile.Text), BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);
                int frameCount = decoder.Frames.Count;
                string baseName = Path.GetFileNameWithoutExtension(TxtInputFile.Text);
                bool processingSuccessful = false;

                string targetDirectory = Path.Combine(TxtOutputFolder.Text, $"{baseName}_Extract");
                if (!Directory.Exists(targetDirectory))
                {
                    Directory.CreateDirectory(targetDirectory);
                }

                if (ChkIndividualFrames.IsChecked == true)
                {
                    for (int i = 0; i < frameCount; i++)
                    {
                        BitmapFrame frame = decoder.Frames[i];
                        string outputPath = Path.Combine(targetDirectory, $"{baseName}_frame_{i:D3}.png");

                        using (FileStream stream = new FileStream(outputPath, FileMode.Create))
                        {
                            PngBitmapEncoder encoder = new PngBitmapEncoder();
                            encoder.Frames.Add(frame);
                            encoder.Save(stream);
                        }
                    }
                    processingSuccessful = true;
                }

                if (ChkSpriteSheet.IsChecked == true)
                {
                    int.TryParse(TxtColumns.Text, out int columns);
                    int.TryParse(TxtFrameWidth.Text, out int frameWidth);
                    int.TryParse(TxtFrameHeight.Text, out int frameHeight);

                    if (columns <= 0) columns = 5;
                    if (frameWidth <= 0) frameWidth = decoder.Frames[0].PixelWidth;
                    if (frameHeight <= 0) frameHeight = decoder.Frames[0].PixelHeight;

                    int rows = (int)Math.Ceiling((double)frameCount / columns);
                    int sheetWidth = columns * frameWidth;
                    int sheetHeight = rows * frameHeight;

                    RenderTargetBitmap renderTarget = new RenderTargetBitmap(sheetWidth, sheetHeight, 96, 96, PixelFormats.Pbgra32);
                    DrawingVisual drawingVisual = new DrawingVisual();

                    using (DrawingContext drawingContext = drawingVisual.RenderOpen())
                    {
                        for (int i = 0; i < frameCount; i++)
                        {
                            int col = i % columns;
                            int row = i / columns;

                            int x = col * frameWidth;
                            int y = row * frameHeight;

                            drawingContext.DrawImage(decoder.Frames[i], new Rect(x, y, frameWidth, frameHeight));
                        }
                    }

                    renderTarget.Render(drawingVisual);

                    string outputPath = Path.Combine(targetDirectory, $"{baseName}_spritesheet.png");

                    using (FileStream stream = new FileStream(outputPath, FileMode.Create))
                    {
                        PngBitmapEncoder encoder = new PngBitmapEncoder();
                        encoder.Frames.Add(BitmapFrame.Create(renderTarget));
                        encoder.Save(stream);
                    }
                    processingSuccessful = true;
                }

                if (processingSuccessful)
                {
                    MessageBox.Show($"Extraction completed successfully.\nFiles saved in:\n{targetDirectory}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred during extraction:\n{ex.Message}", "Extraction Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}