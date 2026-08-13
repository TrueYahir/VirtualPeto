using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace VirtualPeto.Tools
{
    public partial class VideoBgRemoverWindow : Window
    {
        private string _inputFilePath;
        private string _libraryPath;

        public VideoBgRemoverWindow(string videoPath, string libraryPath)
        {
            InitializeComponent();
            _inputFilePath = videoPath;
            _libraryPath = libraryPath;
            TxtFileName.Text = Path.GetFileName(_inputFilePath);
        }

        private async void BtnProcess_Click(object sender, RoutedEventArgs e)
        {
            BtnProcess.IsEnabled = false;
            BtnProcess.Content = "Processing...";

            try
            {
                string outputFileName = Path.GetFileNameWithoutExtension(_inputFilePath) + "_transparent.gif";
                string outputFilePath = Path.Combine(_libraryPath, outputFileName);

                string colorCode = TxtColorCode.Text;
                string similarity = TxtSimilarity.Text;
                string blend = TxtBlend.Text;

                string ffmpegArgs = $"-i \"{_inputFilePath}\" -vf \"colorkey={colorCode}:{similarity}:{blend},split[s0][s1];[s0]palettegen=reserve_transparent=on:transparency_color=ffffff[p];[s1][p]paletteuse\" -y \"{outputFilePath}\"";

                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = ffmpegArgs,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (Process? process = Process.Start(psi))
                {
                    if (process != null)
                    {
                        await process.WaitForExitAsync();
                    }
                }

                MessageBox.Show("Video processed successfully and added to the library as a transparent GIF.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error processing video: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                BtnProcess.IsEnabled = true;
                BtnProcess.Content = "Process Video";
            }
        }
    }
}