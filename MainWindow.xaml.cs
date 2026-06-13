using System;
using System.Windows;
using System.Windows.Input;

namespace VirtualPeto
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }

        private void PetVideo_MediaEnded(object sender, RoutedEventArgs e)
        {
            // Reiniciar el video al terminar (Loop)
            PetVideo.Position = TimeSpan.Zero;
            PetVideo.Play();
        }
    }
}