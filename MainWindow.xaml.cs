using System;
using System.Windows;
using System.Windows.Media.Imaging;
using WpfAnimatedGif;

namespace VirtualPeto
{
    public partial class MainWindow : PetWindowBase
    {
        public MainWindow(string mediaPath, bool isVideo, double size, string soundPath, double volume)
        {
            InitializeComponent();
            
            this.Width = size;
            this.Height = size;

            if (isVideo)
            {
                PetVideo.Visibility = Visibility.Visible;
                PetVideo.Source = new Uri(mediaPath);
                PetVideo.Play();
            }
            else
            {
                PetVideo.Visibility = Visibility.Collapsed;
                
                var animImage = new BitmapImage();
                animImage.BeginInit();
                animImage.UriSource = new Uri(mediaPath);
                animImage.EndInit();
                ImageBehavior.SetAnimatedSource(PetImage, animImage);
            }
            
            if (!string.IsNullOrEmpty(soundPath))
            {
                PlayAudio(soundPath, volume, true);
            }
        }

        private void PetVideo_MediaEnded(object sender, RoutedEventArgs e)
        {
            PetVideo.Position = TimeSpan.Zero;
            PetVideo.Play();
        }
    }
}