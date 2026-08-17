using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using Microsoft.Win32;

namespace VirtualPeto.Tools
{
    public partial class AudioConverterWindow : Window
    {
        public AudioConverterWindow()
        {
            InitializeComponent();
        }

        private void BtnBrowse_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Audio Files|*.wav;*.mp3;*.ogg;*.m4a;*.flac|All Files|*.*";
            ofd.Title = "Select Audio File";

            if (ofd.ShowDialog() == true)
            {
                TxtInputFile.Text = ofd.FileName;
            }
        }

        private void BtnConvert_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtInputFile.Text) || !File.Exists(TxtInputFile.Text))
            {
                MessageBox.Show("Please select a valid input file.", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string inputPath = TxtInputFile.Text;
            string selectedFormat = (CmbTargetFormat.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString() ?? ".wav";
            string directory = Path.GetDirectoryName(inputPath) ?? string.Empty;
            if (!string.IsNullOrEmpty(SettingsManager.Current.DefaultSaveFolder) && 
                Directory.Exists(SettingsManager.Current.DefaultSaveFolder))
            {
                directory = SettingsManager.Current.DefaultSaveFolder;
            }
            string fileName = Path.GetFileNameWithoutExtension(inputPath);
            string outputPath = Path.Combine(directory, $"{fileName}_converted{selectedFormat}");

            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = $"-y -i \"{inputPath}\" \"{outputPath}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (Process? process = Process.Start(startInfo))
                {
                    if (process != null)
                    {
                        process.WaitForExit();

                        if (process.ExitCode == 0)
                        {
                            MessageBox.Show($"Conversion successful!\nFile saved to: {outputPath}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        else
                        {
                            MessageBox.Show("Conversion failed. Please ensure FFmpeg is installed and added to your system PATH.", "Conversion Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred during conversion:\n{ex.Message}\n\nMake sure FFmpeg is installed.", "Execution Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}