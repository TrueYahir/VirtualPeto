using Microsoft.Win32; 
using System;
using System.IO;
using System.Windows;

namespace VirtualPeto.Objects 
{
    public partial class FoodCreatorWindow : Window
    {
        public FoodCreatorWindow()
        {
            InitializeComponent();
        }

        private void BtnBrowseImage_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = "Select Food Image",
                Filter = "Image Files|*.png;*.jpg;*.jpeg;*.gif"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                TxtImagePath.Text = openFileDialog.FileName;
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtFoodName.Text))
            {
                MessageBox.Show("Please enter a food name.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(TxtImagePath.Text) || !File.Exists(TxtImagePath.Text))
            {
                MessageBox.Show("Please select a valid image for the food.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            string foodName = TxtFoodName.Text;
            string imagePath = TxtImagePath.Text;
            int hungerValue = (int)SliderHunger.Value;
            int healthValue = (int)SliderHealth.Value;
            int energyValue = (int)SliderEnergy.Value;

            try
            {
                // 3. AQUÍ VA TU LÓGICA DE GUARDADO (.vfood)
                // Por ahora, mostraremos un mensaje simulando que se guardó.
                // Dependiendo de tu sistema, aquí podrías empaquetar la imagen y un archivo JSON/XML en un ZIP (.vfood).
                
                string debugMessage = $"Food Created Successfully!\n\n" +
                                      $"Name: {foodName}\n" +
                                      $"Hunger: +{hungerValue}\n" +
                                      $"Health: {healthValue}\n" +
                                      $"Energy: {energyValue}";
                                      
                MessageBox.Show(debugMessage, "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                this.DialogResult = true; 
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving food: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}