using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace VirtualPeto.Tools
{
    public partial class VideoBgRemoverWindow : Window
    {
        private string _inputFilePath;
        private string _libraryPath;
        private string _tempFilePath = string.Empty;

        public VideoBgRemoverWindow(string videoPath, string libraryPath)
        {
            InitializeComponent();
            _inputFilePath = videoPath;
            _libraryPath = libraryPath;
            TxtFileName.Text = Path.GetFileName(_inputFilePath);

            MediaOriginal.Source = new Uri(_inputFilePath);
            MediaOriginal.Play();
            UpdateColorPreview();
        }

        private void TxtColorCode_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateColorPreview();
        }

        private void UpdateColorPreview()
        {
            if (ColorPreviewBox == null || TxtColorCode == null) return;
            try
            {
                string hex = TxtColorCode.Text.Trim();
                if (hex.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) hex = "#" + hex.Substring(2);
                else if (!hex.StartsWith("#")) hex = "#" + hex;

                if (hex.Length == 7 || hex.Length == 9)
                {
                    var color = (Color)ColorConverter.ConvertFromString(hex);
                    ColorPreviewBox.Background = new SolidColorBrush(color);
                }
            }
            catch { }
        }

        private async void BtnGeneratePreview_Click(object sender, RoutedEventArgs e)
        {
            BtnGeneratePreview.IsEnabled = false;
            BtnSaveResult.IsEnabled = false;
            BtnGeneratePreview.Content = "Processing...";
            TxtPreviewHint.Visibility = Visibility.Visible;
            TxtPreviewHint.Text = "Generating Preview...";
            
            MediaProcessed.Stop();
            MediaProcessed.Close();
            MediaProcessed.Source = null;

            try
            {
                string outputFileName = Path.GetFileNameWithoutExtension(_inputFilePath) + "_preview.mp4";
                _tempFilePath = Path.Combine(Path.GetTempPath(), outputFileName);

                string colorCode = TxtColorCode.Text.Replace("#", "0x");
                string similarity = TxtSimilarity.Text;
                string blend = TxtBlend.Text;
                string targetFps = TxtFps.Text;
                string targetWidth = TxtWidth.Text;

                string ffmpegArgs = $"-i \"{_inputFilePath}\" -filter_complex \"[0:v]fps={targetFps},scale={targetWidth}:-2:flags=lanczos[scaled];[scaled]split[bg_raw][fg_raw];[bg_raw]drawbox=c=#1A1A24:t=fill,format=yuv420p[bg_solid];[fg_raw]chromakey={colorCode}:{similarity}:{blend}[fg_transparent];[bg_solid][fg_transparent]overlay=format=yuv420\" -c:v libx264 -preset ultrafast -y \"{_tempFilePath}\"";

                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = ffmpegArgs,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (Process? process = Process.Start(psi))
                {
                    if (process != null) await process.WaitForExitAsync();
                }

                MediaProcessed.Source = new Uri(_tempFilePath);
                MediaProcessed.Play();
                
                TxtPreviewHint.Visibility = Visibility.Collapsed;
                BtnSaveResult.IsEnabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                TxtPreviewHint.Text = "Error.";
            }
            finally
            {
                BtnGeneratePreview.IsEnabled = true;
                BtnGeneratePreview.Content = "Generate Preview";
            }
        }

        private async void BtnSaveResult_Click(object sender, RoutedEventArgs e)
        {
            BtnSaveResult.IsEnabled = false;
            BtnSaveResult.Content = "Saving...";

            try
            {
                MediaProcessed.Stop();
                MediaProcessed.Close();
                MediaProcessed.Source = null;

                string colorCode = TxtColorCode.Text.Replace("#", "0x");
                string similarity = TxtSimilarity.Text;
                string blend = TxtBlend.Text;
                string targetFps = TxtFps.Text;
                string targetWidth = TxtWidth.Text;
                string baseFileName = Path.GetFileNameWithoutExtension(_inputFilePath);

                if (ChkKeepAudio.IsChecked == true)
                {
                    string tempGifPath = Path.Combine(Path.GetTempPath(), baseFileName + "_temp.gif");
                    string tempWavPath = Path.Combine(Path.GetTempPath(), baseFileName + "_temp.wav");
                    string finalPackagePath = Path.Combine(_libraryPath, baseFileName + ".vpet");

                    string gifArgs = $"-i \"{_inputFilePath}\" -vf \"fps={targetFps},scale={targetWidth}:-2:flags=lanczos,chromakey={colorCode}:{similarity}:{blend},format=rgba,split[s0][s1];[s0]palettegen=reserve_transparent=on:transparency_color=ffffff[p];[s1][p]paletteuse=alpha_threshold=128\" -gifflags -transdiff -y \"{tempGifPath}\"";
                    string audioArgs = $"-i \"{_inputFilePath}\" -vn -acodec pcm_s16le -y \"{tempWavPath}\"";

                    using (Process? processGif = Process.Start(new ProcessStartInfo { FileName = "ffmpeg", Arguments = gifArgs, UseShellExecute = false, CreateNoWindow = true }))
                    {
                        if (processGif != null) await processGif.WaitForExitAsync();
                    }

                    using (Process? processAudio = Process.Start(new ProcessStartInfo { FileName = "ffmpeg", Arguments = audioArgs, UseShellExecute = false, CreateNoWindow = true }))
                    {
                        if (processAudio != null) await processAudio.WaitForExitAsync();
                    }

                    if (File.Exists(finalPackagePath))
                    {
                        File.Delete(finalPackagePath);
                    }

                    using (FileStream zipToOpen = new FileStream(finalPackagePath, FileMode.Create))
                    using (ZipArchive archive = new ZipArchive(zipToOpen, ZipArchiveMode.Create))
                    {
                        if (File.Exists(tempGifPath))
                        {
                            archive.CreateEntryFromFile(tempGifPath, Path.GetFileName(tempGifPath));
                        }
                        
                        if (File.Exists(tempWavPath))
                        {
                            archive.CreateEntryFromFile(tempWavPath, Path.GetFileName(tempWavPath));
                        }

                        ZipArchiveEntry configEntry = archive.CreateEntry("config.json");
                        using (StreamWriter writer = new StreamWriter(configEntry.Open()))
                        {
                            string volumeStr = "0.5";
                            string jsonContent = $"{{\"Name\":\"{baseFileName}\",\"GifFile\":\"{Path.GetFileName(tempGifPath)}\",\"SoundFile\":\"{Path.GetFileName(tempWavPath)}\",\"Volume\":{volumeStr},\"IsSmartPet\":false}}";
                            writer.Write(jsonContent);
                        }
                    }

                    if (File.Exists(tempGifPath)) File.Delete(tempGifPath);
                    if (File.Exists(tempWavPath)) File.Delete(tempWavPath);
                }
                else
                {
                    string finalFilePath = Path.Combine(_libraryPath, baseFileName + "_transparent.gif");
                    string ffmpegArgs = $"-i \"{_inputFilePath}\" -vf \"fps={targetFps},scale={targetWidth}:-2:flags=lanczos,chromakey={colorCode}:{similarity}:{blend},format=rgba,split[s0][s1];[s0]palettegen=reserve_transparent=on:transparency_color=ffffff[p];[s1][p]paletteuse=alpha_threshold=128\" -gifflags -transdiff -y \"{finalFilePath}\"";

                    ProcessStartInfo psi = new ProcessStartInfo
                    {
                        FileName = "ffmpeg",
                        Arguments = ffmpegArgs,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    using (Process? process = Process.Start(psi))
                    {
                        if (process != null) await process.WaitForExitAsync();
                    }
                }

                MessageBox.Show("File processed successfully and saved.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                BtnSaveResult.IsEnabled = true;
                BtnSaveResult.Content = "Save to Library";
            }
        }

        private void MediaOriginal_MediaEnded(object sender, RoutedEventArgs e)
        {
            MediaOriginal.Position = TimeSpan.Zero;
            MediaOriginal.Play();
        }

        private void MediaProcessed_MediaEnded(object sender, RoutedEventArgs e)
        {
            MediaProcessed.Position = TimeSpan.Zero;
            MediaProcessed.Play();
        }

        protected override void OnClosed(EventArgs e)
        {
            MediaOriginal.Close();
            MediaProcessed.Close();
            
            if (!string.IsNullOrEmpty(_tempFilePath) && File.Exists(_tempFilePath))
            {
                try { File.Delete(_tempFilePath); } catch { }
            }
            
            base.OnClosed(e);
        }
    }
}