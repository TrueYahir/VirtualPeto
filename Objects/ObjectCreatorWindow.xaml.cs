using Microsoft.Win32;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace VirtualPeto.Objects
{
    public partial class ObjectCreatorWindow : Window
    {
        public ObjectCreatorWindow()
        {
            InitializeComponent();
        }

        private void BtnBrowseImage_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = "Select Object Image",
                Filter = "Image Files|*.png;*.jpg;*.jpeg;*.gif;*.bmp;*.webp"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                TxtImagePath.Text = openFileDialog.FileName;
                UpdatePreview(openFileDialog.FileName);
            }
        }

        private void UpdatePreview(string imagePath)
        {
            if (string.IsNullOrWhiteSpace(imagePath) || !File.Exists(imagePath))
            {
                ImgPreview.Source = null;
                TxtPreviewHint.Visibility = Visibility.Visible;
                return;
            }

            BitmapImage previewImage = new BitmapImage();
            previewImage.BeginInit();
            previewImage.CacheOption = BitmapCacheOption.OnLoad;
            previewImage.UriSource = new Uri(imagePath, UriKind.Absolute);
            previewImage.EndInit();
            previewImage.Freeze();

            ImgPreview.Source = previewImage;
            TxtPreviewHint.Visibility = Visibility.Collapsed;
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtObjectName.Text))
            {
                MessageBox.Show("Please enter an object name.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(TxtImagePath.Text) || !File.Exists(TxtImagePath.Text))
            {
                MessageBox.Show("Please select a valid image for the object.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string objectName = TxtObjectName.Text;
            string category = (CmbCategory.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Unknown";
            string imagePath = TxtImagePath.Text;
            if (!int.TryParse(TxtHappinessBonus.Text, out int happinessValue))
            {
                MessageBox.Show("Please enter a valid number for Happiness Bonus.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!int.TryParse(TxtDurabilityUses.Text, out int durabilityValue) || durabilityValue <= 0)
            {
                MessageBox.Show("Please enter a valid number greater than 0 for Durability Uses.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                string debugMessage = $"Object Created Successfully!\n\n" +
                                      $"Name: {objectName}\n" +
                                      $"Category: {category}\n" +
                                      $"Happiness: +{happinessValue}\n" +
                                      $"Durability: {durabilityValue} uses";
                                      
                MessageBox.Show(debugMessage, "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                this.DialogResult = true; 
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving object: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // --- CANCELAR ---
        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}