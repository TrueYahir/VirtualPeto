using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
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

        private void PetContextMenu_Opened(object sender, RoutedEventArgs e)
        {
            if (sender is ContextMenu menu)
            {
                menu.PlacementTarget = this;
                menu.Placement = PlacementMode.Right;
                menu.HorizontalOffset = 8;
                menu.VerticalOffset = 0;

                if (PresentationSource.FromVisual(menu) is HwndSource hwndSource)
                {
                    SetWindowPos(hwndSource.Handle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
                }
            }
        }

        private void MenuClosePet_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}