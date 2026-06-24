using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Input;
using System.Windows.Ink;
using Microsoft.Win32;
using WpfAnimatedGif;
using ImageMagick;

namespace VirtualPeto
{
    // === MODELS ===

    public class LibraryItem : INotifyPropertyChanged
    {
        public string Name { get; set; } = string.Empty;
        public string FullPath { get; set; } = string.Empty;
        public BitmapImage? Thumbnail { get; set; }
        public bool IsVideo { get; set; }

        public Visibility ImageIconVisibility => IsVideo ? Visibility.Collapsed : Visibility.Visible;
        public Visibility VideoIconVisibility => IsVideo ? Visibility.Visible : Visibility.Collapsed;

        private bool _isActive;
        public bool IsActive
        {
            get { return _isActive; }
            set { _isActive = value; OnPropertyChanged(); OnPropertyChanged(nameof(VisibilityIndicator)); }
        }
        public Visibility VisibilityIndicator => _isActive ? Visibility.Visible : Visibility.Collapsed;

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class PetConfig
    {
        public string Name { get; set; } = "Unknown";
        public int WalkSpeed { get; set; } = 5;
        public double Scale { get; set; } = 1.0;
        public AnimationsConfig Animations { get; set; } = new AnimationsConfig();
    }

    public class AnimationsConfig
    {
        public string Idle { get; set; } = string.Empty;
        public string WalkLeft { get; set; } = string.Empty;
        public string WalkRight { get; set; } = string.Empty;
        public string Sleep { get; set; } = string.Empty;
        public string LookAtScreen { get; set; } = string.Empty;
    }

    public class PetItem : INotifyPropertyChanged
    {
        public string DirectoryPath { get; set; } = string.Empty;
        public PetConfig Config { get; set; } = new PetConfig();
        public BitmapImage? Thumbnail { get; set; }
        public string Name => Config.Name;

        private bool _isActive;
        public bool IsActive
        {
            get { return _isActive; }
            set { _isActive = value; OnPropertyChanged(); OnPropertyChanged(nameof(VisibilityIndicator)); }
        }
        public Visibility VisibilityIndicator => _isActive ? Visibility.Visible : Visibility.Collapsed;

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
    internal struct IntPoint
    {
        public int X, Y;
        public IntPoint(int x, int y) { X = x; Y = y; }
    }

    // === MAIN WINDOW CLASS ===

    public partial class ConfigWindow : Window
    {
        private string libraryPath;
        private string petsPath;
        private List<LibraryItem> fullLibraryList = new List<LibraryItem>();
        private List<PetItem> fullPetsList = new List<PetItem>();
        
        private Dictionary<LibraryItem, MainWindow> activeLibraryWindows = new Dictionary<LibraryItem, MainWindow>();
        private Dictionary<PetItem, MainWindow> activePetsWindows = new Dictionary<PetItem, MainWindow>();

        private readonly string[] validImages = { ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".webp", ".ico", ".tiff" };
        private readonly string[] validVideos = { ".mp4", ".avi", ".mkv", ".webm", ".mov", ".wmv" };

        // Variables temporales de herramientas
        private string[] selectedToolsGifImages = Array.Empty<string>();
        private string selectedToolsBgImage = string.Empty;
        private string selectedSpriteSheetPath = string.Empty;
        private bool autoClearCache = false;

        public ConfigWindow()
        {
            InitializeComponent();
            
            string baseDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "VirtualPeto");
            
            libraryPath = Path.Combine(baseDataPath, "Library");
            petsPath = Path.Combine(baseDataPath, "Pets");
            
            if (!Directory.Exists(libraryPath)) Directory.CreateDirectory(libraryPath);
            if (!Directory.Exists(petsPath)) Directory.CreateDirectory(petsPath);
            
            LoadLibrary();
            LoadPets();
        }

        // === HELPERS ===

        private BitmapImage? LoadImageToMemory(string path)
        {
            if (!File.Exists(path)) return null;

            try
            {
                BitmapImage image = new BitmapImage();
                byte[] imageData = File.ReadAllBytes(path);
                MemoryStream memStream = new MemoryStream(imageData);
                
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad; 
                image.StreamSource = memStream;                  
                image.EndInit();
                
                if (!path.ToLower().EndsWith(".gif")) image.Freeze(); 
                
                return image;
            }
            catch { return null; }
        }

        private void CopyDirectory(string source, string destination)
        {
            DirectoryInfo dir = new DirectoryInfo(source);
            if (!dir.Exists) throw new DirectoryNotFoundException($"Directory not found: {source}");
            
            Directory.CreateDirectory(destination);
            foreach (FileInfo file in dir.GetFiles())
            {
                file.CopyTo(Path.Combine(destination, file.Name), true);
            }
            foreach (DirectoryInfo subDir in dir.GetDirectories())
            {
                CopyDirectory(subDir.FullName, Path.Combine(destination, subDir.Name));
            }
        }

        // === LIBRARY LOGIC ===

        private void LoadLibrary()
        {
            if (!Directory.Exists(libraryPath)) return;
            
            fullLibraryList = Directory.GetFiles(libraryPath, "*.*")
                .Where(f => validImages.Contains(Path.GetExtension(f).ToLower()) || validVideos.Contains(Path.GetExtension(f).ToLower()))
                .Select(path => 
                {
                    bool isVid = validVideos.Contains(Path.GetExtension(path).ToLower());
                    return new LibraryItem 
                    { 
                        Name = Path.GetFileName(path), 
                        FullPath = path, 
                        IsVideo = isVid,
                        Thumbnail = isVid ? null : LoadImageToMemory(path), 
                        IsActive = activeLibraryWindows.Any(v => v.Key.FullPath == path)
                    };
                }).ToList();
            
            ApplyLibraryFilters();
        }

        private void ApplyLibraryFilters()
        {
            if (fullLibraryList == null || LstLibrary == null) return;

            var filteredList = fullLibraryList.AsEnumerable();
            string searchTxt = TxtSearchLibrary?.Text?.ToLower().Trim() ?? "";
            if (!string.IsNullOrEmpty(searchTxt))
            {
                filteredList = filteredList.Where(m => m.Name.ToLower().Contains(searchTxt));
            }
            if (CmbFilterType != null && CmbFilterType.SelectedItem is ComboBoxItem selectedItem)
            {
                string type = selectedItem.Content.ToString()!;
                if (type == "Images")
                    filteredList = filteredList.Where(m => !m.IsVideo && !m.Name.ToLower().EndsWith(".gif"));
                else if (type == "GIFs")
                    filteredList = filteredList.Where(m => !m.IsVideo && m.Name.ToLower().EndsWith(".gif"));
                else if (type == "Videos")
                    filteredList = filteredList.Where(m => m.IsVideo);
            }

            if (ChkFilterActive != null && ChkFilterActive.IsChecked == true)
            {
                filteredList = filteredList.Where(m => m.IsActive);
            }

            LstLibrary.ItemsSource = null;
            LstLibrary.ItemsSource = filteredList.ToList();
        }

        private void Filters_Changed(object sender, RoutedEventArgs e)
        {
            ApplyLibraryFilters();
        }

        private void BtnAddLibrary_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog { 
                Filter = "Media Files|*.png;*.jpg;*.jpeg;*.gif;*.bmp;*.webp;*.mp4;*.avi;*.mkv;*.webm;*.mov|All Files|*.*" 
            };
            
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    File.Copy(dialog.FileName, Path.Combine(libraryPath, Path.GetFileName(dialog.FileName)), true);
                    LoadLibrary();
                }
                catch (Exception ex) { MessageBox.Show("Error adding file: " + ex.Message); }
            }
        }

        private void LstLibrary_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try 
            {
                VidLibraryPreview.Stop();

                if (LstLibrary.SelectedItem == null || !(LstLibrary.SelectedItem is LibraryItem selected))
                {
                    TxtSelectedLibraryName.Text = "None selected";
                    ImageBehavior.SetAnimatedSource(ImgLibraryPreview, null);
                    ImgLibraryPreview.Source = null;
                    ImgLibraryPreview.Visibility = Visibility.Visible;
                    VidLibraryPreview.Visibility = Visibility.Collapsed;
                    TxtEmptyLibraryPreview.Visibility = Visibility.Visible;
                    PnlSize.Visibility = Visibility.Collapsed;
                    PnlFps.Visibility = Visibility.Collapsed;
                    return;
                }

                TxtEmptyLibraryPreview.Visibility = Visibility.Collapsed;
                PnlSize.Visibility = Visibility.Visible;
                TxtSelectedLibraryName.Text = selected.Name;

                if (selected.IsVideo)
                {
                    PnlFps.Visibility = Visibility.Collapsed;
                    ImgLibraryPreview.Visibility = Visibility.Collapsed;
                    VidLibraryPreview.Visibility = Visibility.Visible;
                    
                    VidLibraryPreview.Source = new Uri(selected.FullPath);
                    VidLibraryPreview.Play();
                }
                else
                {
                    ImgLibraryPreview.Visibility = Visibility.Visible;
                    VidLibraryPreview.Visibility = Visibility.Collapsed;

                    BitmapImage? imgSource = LoadImageToMemory(selected.FullPath);

                    if (selected.Name.ToLower().EndsWith(".gif"))
                    {
                        PnlFps.Visibility = Visibility.Visible;
                        ImageBehavior.SetAnimatedSource(ImgLibraryPreview, null);
                        if(imgSource != null) ImageBehavior.SetAnimatedSource(ImgLibraryPreview, imgSource);
                    }
                    else
                    {
                        PnlFps.Visibility = Visibility.Collapsed;
                        ImageBehavior.SetAnimatedSource(ImgLibraryPreview, null);
                        ImgLibraryPreview.Source = imgSource;
                    }
                }
            }
            catch { }
        }

        private void VidLibraryPreview_MediaEnded(object sender, RoutedEventArgs e)
        {
            VidLibraryPreview.Position = TimeSpan.Zero;
            VidLibraryPreview.Play();
        }

        private void BtnIncreaseFps_Click(object sender, RoutedEventArgs e) { if (int.TryParse(TxtFps.Text, out int current) && current < 60) TxtFps.Text = (current + 1).ToString(); }
        private void BtnDecreaseFps_Click(object sender, RoutedEventArgs e) { if (int.TryParse(TxtFps.Text, out int current) && current > 1) TxtFps.Text = (current - 1).ToString(); }

        private void BtnDeleteLibrary_Click(object sender, RoutedEventArgs e)
        {
            if (LstLibrary.SelectedItem is LibraryItem selected)
            {
                if (MessageBox.Show($"Delete {selected.Name}?", "Confirm", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    try
                    {
                        LstLibrary.SelectedIndex = -1;
                        ImageBehavior.SetAnimatedSource(ImgLibraryPreview, null);
                        ImgLibraryPreview.Source = null;
                        VidLibraryPreview.Stop();

                        if (activeLibraryWindows.TryGetValue(selected, out MainWindow? openWindow))
                        {
                            openWindow.Close();
                        }

                        fullLibraryList.Remove(selected);
                        GC.Collect(); GC.WaitForPendingFinalizers();
                        if (File.Exists(selected.FullPath)) File.Delete(selected.FullPath);
                        
                        LoadLibrary();
                    }
                    catch (Exception ex) { MessageBox.Show("Error deleting: " + ex.Message); LoadLibrary(); }
                }
            }
        }

        private void BtnLaunchLibrary_Click(object sender, RoutedEventArgs e)
        {
            if (LstLibrary.SelectedItem is LibraryItem selected)
            {
                if (activeLibraryWindows.ContainsKey(selected)) { activeLibraryWindows[selected].Activate(); return; }

                MainWindow newWindow = new MainWindow();
                newWindow.Width = SldSize.Value;
                newWindow.Height = SldSize.Value;

                if (selected.IsVideo)
                {
                    newWindow.PetImage.Visibility = Visibility.Collapsed;
                    newWindow.PetVideo.Visibility = Visibility.Visible;
                    newWindow.PetVideo.Source = new Uri(selected.FullPath);
                    newWindow.PetVideo.Play();
                }
                else
                {
                    BitmapImage? imgSource = LoadImageToMemory(selected.FullPath);
                    if (selected.Name.ToLower().EndsWith(".gif"))
                    {
                        if(imgSource != null) ImageBehavior.SetAnimatedSource(newWindow.PetImage, imgSource);
                        if (int.TryParse(TxtFps.Text, out int fps) && fps > 0)
                        {
                            var controller = ImageBehavior.GetAnimationController(newWindow.PetImage);
                            if (controller != null) ImageBehavior.SetAnimationDuration(newWindow.PetImage, new Duration(TimeSpan.FromMilliseconds((1000 / fps) * controller.FrameCount)));
                        }
                    }
                    else { newWindow.PetImage.Source = imgSource; }
                }

                newWindow.Closed += (s, args) => 
                { 
                    selected.IsActive = false; 
                    activeLibraryWindows.Remove(selected); 
                    ApplyLibraryFilters();
                    if (autoClearCache)
                    {
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                        GC.Collect();
                    }
                };
                
                activeLibraryWindows.Add(selected, newWindow);
                selected.IsActive = true;
                newWindow.Show();
                ApplyLibraryFilters(); 
            }
        }

        private void BtnCloseLibrary_Click(object sender, RoutedEventArgs e)
        {
            if (LstLibrary.SelectedItem is LibraryItem selected && activeLibraryWindows.ContainsKey(selected))
            {
                activeLibraryWindows[selected].Close();
            }
        }

        // === SMART PETS LOGIC ===

        private void LoadPets()
        {
            if (!Directory.Exists(petsPath)) return;
            fullPetsList.Clear();

            string[] folders = Directory.GetDirectories(petsPath);
            foreach (string folder in folders)
            {
                string jsonFile = Path.Combine(folder, "config.json");
                if (File.Exists(jsonFile))
                {
                    try
                    {
                        string jsonString = File.ReadAllText(jsonFile);
                        PetConfig? config = JsonSerializer.Deserialize<PetConfig>(jsonString);
                        if (config != null)
                        {
                            string idlePath = Path.Combine(folder, config.Animations.Idle);
                            fullPetsList.Add(new PetItem
                            {
                                DirectoryPath = folder,
                                Config = config,
                                Thumbnail = File.Exists(idlePath) ? LoadImageToMemory(idlePath) : null,
                                IsActive = activePetsWindows.Any(v => v.Key.DirectoryPath == folder)
                            });
                        }
                    }
                    catch { }
                }
            }
            UpdatePetsList(fullPetsList);
        }

        private void UpdatePetsList(List<PetItem> list)
        {
            LstPets.ItemsSource = null;
            LstPets.ItemsSource = list;
        }

        private void BtnAddPet_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog { Title = "Select config.json", Filter = "JSON (*.json)|*.json" };
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    string sourcePath = Path.GetDirectoryName(dialog.FileName)!;
                    string folderName = new DirectoryInfo(sourcePath).Name;
                    string targetPath = Path.Combine(petsPath, folderName);

                    if (!Directory.Exists(targetPath))
                    {
                        CopyDirectory(sourcePath, targetPath);
                        LoadPets();
                    }
                    else
                    {
                        MessageBox.Show("A pet with this folder name already exists.");
                    }
                }
                catch (Exception ex) { MessageBox.Show("Error adding pet: " + ex.Message); }
            }
        }

        private void LstPets_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try 
            {
                if (LstPets.SelectedItem == null || !(LstPets.SelectedItem is PetItem selected))
                {
                    TxtSelectedPetName.Text = "None selected";
                    ImageBehavior.SetAnimatedSource(ImgPetPreview, null);
                    ImgPetPreview.Source = null;
                    TxtEmptyPetPreview.Visibility = Visibility.Visible;
                    return;
                }

                TxtEmptyPetPreview.Visibility = Visibility.Collapsed;
                TxtSelectedPetName.Text = selected.Name;

                string imgPath = Path.Combine(selected.DirectoryPath, selected.Config.Animations.Idle);
                BitmapImage? imgSource = LoadImageToMemory(imgPath);

                if (imgPath.ToLower().EndsWith(".gif"))
                {
                    ImageBehavior.SetAnimatedSource(ImgPetPreview, null);
                    if(imgSource != null) ImageBehavior.SetAnimatedSource(ImgPetPreview, imgSource);
                }
                else
                {
                    ImageBehavior.SetAnimatedSource(ImgPetPreview, null);
                    ImgPetPreview.Source = imgSource;
                }
            }
            catch { }
        }

        private void BtnDeletePet_Click(object sender, RoutedEventArgs e)
        {
            if (LstPets.SelectedItem is PetItem selected)
            {
                if (MessageBox.Show($"Delete {selected.Name}?", "Confirm", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    try
                    {
                        LstPets.SelectedIndex = -1;
                        ImageBehavior.SetAnimatedSource(ImgPetPreview, null);
                        ImgPetPreview.Source = null;

                        if (activePetsWindows.TryGetValue(selected, out MainWindow? openWindow))
                        {
                            openWindow.Close();
                        }

                        fullPetsList.Remove(selected);
                        GC.Collect(); GC.WaitForPendingFinalizers();
                        if (Directory.Exists(selected.DirectoryPath)) Directory.Delete(selected.DirectoryPath, true);
                        
                        LoadPets();
                    }
                    catch (Exception ex) { MessageBox.Show("Error deleting: " + ex.Message); LoadPets(); }
                }
            }
        }

        private void BtnLaunchPet_Click(object sender, RoutedEventArgs e)
        {
            if (LstPets.SelectedItem is PetItem selected)
            {
                if (activePetsWindows.ContainsKey(selected)) { activePetsWindows[selected].Activate(); return; }

                MainWindow newWindow = new MainWindow();
                newWindow.Width = 150 * selected.Config.Scale;
                newWindow.Height = 150 * selected.Config.Scale;

                string idlePath = Path.Combine(selected.DirectoryPath, selected.Config.Animations.Idle);
                BitmapImage? imgSource = LoadImageToMemory(idlePath);
                
                if (idlePath.ToLower().EndsWith(".gif")) 
                {
                    if(imgSource != null) ImageBehavior.SetAnimatedSource(newWindow.PetImage, imgSource);
                }
                else newWindow.PetImage.Source = imgSource;

                newWindow.Closed += (s, args) => { selected.IsActive = false; activePetsWindows.Remove(selected);
                    if (autoClearCache)
                    {
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                        GC.Collect();
                    }
                };
                activePetsWindows.Add(selected, newWindow);
                selected.IsActive = true;
                newWindow.Show();
            }
        }

        private void BtnClosePet_Click(object sender, RoutedEventArgs e)
        {
            if (LstPets.SelectedItem is PetItem selected && activePetsWindows.ContainsKey(selected))
            {
                activePetsWindows[selected].Close();
            }
        }

        private void TxtSearchPet_TextChanged(object sender, TextChangedEventArgs e)
        {
            string txt = TxtSearchPet.Text.ToLower().Trim();
            UpdatePetsList(string.IsNullOrEmpty(txt) ? fullPetsList : fullPetsList.Where(m => m.Name.ToLower().Contains(txt)).ToList());
        }

        // === CREATE PET LOGIC ===

        private void BtnGeneratePetTemplate_Click(object sender, RoutedEventArgs e)
        {
            string petName = TxtNewPetName.Text.Trim();
            
            if (string.IsNullOrEmpty(petName))
            {
                MessageBox.Show("Please enter a name for the pet.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string newPetDirectory = Path.Combine(petsPath, petName);

            if (Directory.Exists(newPetDirectory))
            {
                MessageBox.Show("A pet with this name already exists.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                Directory.CreateDirectory(newPetDirectory);
                Directory.CreateDirectory(Path.Combine(newPetDirectory, "assets"));

                PetConfig templateConfig = new PetConfig
                {
                    Name = petName,
                    WalkSpeed = 5,
                    Scale = 1.0,
                    Animations = new AnimationsConfig
                    {
                        Idle = "assets/idle.gif", 
                        WalkLeft = ChkAnimWalkLeft.IsChecked == true ? "assets/walk_left.gif" : "",
                        WalkRight = ChkAnimWalkRight.IsChecked == true ? "assets/walk_right.gif" : "",
                        Sleep = ChkAnimSleep.IsChecked == true ? "assets/sleep.gif" : "",
                        LookAtScreen = ChkAnimLookScreen.IsChecked == true ? "assets/look_screen.gif" : ""
                    }
                };

                var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
                string jsonString = JsonSerializer.Serialize(templateConfig, jsonOptions);
                File.WriteAllText(Path.Combine(newPetDirectory, "config.json"), jsonString);

                MessageBox.Show($"Template generated successfully!\n\nLocation: {newPetDirectory}\n\nDrop your GIFs in the 'assets' folder.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                
                TxtNewPetName.Clear();
                ChkAnimWalkLeft.IsChecked = false;
                ChkAnimWalkRight.IsChecked = false;
                ChkAnimSleep.IsChecked = false;
                ChkAnimLookScreen.IsChecked = false;
                
                LoadPets(); 
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error generating pet folder: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Tools logic

        private void BtnToolsSelectGifImages_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Multiselect = true,
                Filter = "Images (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                selectedToolsGifImages = openFileDialog.FileNames;
                TxtToolsGifCount.Text = $"{selectedToolsGifImages.Length} images selected";
            }
        }

        private void BtnToolsGenerateGif_Click(object sender, RoutedEventArgs e)
        {
            if (selectedToolsGifImages == null || selectedToolsGifImages.Length < 2)
            {
                MessageBox.Show("Please select at least 2 images to create a GIF animation.", "GIF Creator", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            MessageBox.Show("To compile GIFs natively in C#, we need to install the 'Magick.NET-Q8-AnyCPU' NuGet package.\n\nTell me: 'Let's install Magick.NET' and I'll give you the instructions to make this button work!", "Library Needed", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnToolsSelectBgImage_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Images (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                selectedToolsBgImage = openFileDialog.FileName;
                TxtToolsBgImageName.Text = Path.GetFileName(selectedToolsBgImage);
                ImgToolsBgPreview.Source = LoadImageToMemory(selectedToolsBgImage);
            }
        }

        private void BtnToolsRemoveBg_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(selectedToolsBgImage) || !File.Exists(selectedToolsBgImage))
            {
                MessageBox.Show("Please select an image first.", "Background Remover", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            BackgroundEditorWindow editor = new BackgroundEditorWindow(selectedToolsBgImage, libraryPath);
            editor.Closed += (s, args) => LoadLibrary(); 
            editor.ShowDialog();
        }

        private void BtnToolsSelectSprite_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Sprite Sheets (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp",
                Title = "Select Sprite Sheet"
            };
            if (openFileDialog.ShowDialog() == true)
            {
                selectedSpriteSheetPath = openFileDialog.FileName;
                TxtToolsSpriteName.Text = System.IO.Path.GetFileName(selectedSpriteSheetPath);
                TxtToolsSpriteName.Foreground = System.Windows.Media.Brushes.White;
            }
        }

        private void BtnToolsConvertSpriteToGif_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(selectedSpriteSheetPath) || !File.Exists(selectedSpriteSheetPath))
            {
                MessageBox.Show("Please select a Sprite Sheet first.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!int.TryParse(TxtSpriteColumns.Text, out int columns) || !int.TryParse(TxtSpriteRows.Text, out int rows) || columns <= 0 || rows <= 0)
            {
                MessageBox.Show("Please enter valid numbers greater than 0 for rows and columns.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            try
            {
                BitmapImage spriteSheet = new BitmapImage(new Uri(selectedSpriteSheetPath));
                int frameWidth = spriteSheet.PixelWidth / columns;
                int frameHeight = spriteSheet.PixelHeight / rows;
                GifBitmapEncoder encoder = new GifBitmapEncoder();
                for (int y = 0; y < rows; y++)
                {
                    for (int x = 0; x < columns; x++)
                    {
                        Int32Rect rect = new Int32Rect(x * frameWidth, y * frameHeight, frameWidth, frameHeight);
                        CroppedBitmap frame = new CroppedBitmap(spriteSheet, rect);
                        encoder.Frames.Add(BitmapFrame.Create(frame));
                    }
                }
                string outputPath = Path.Combine(libraryPath, $"SpriteAnim_{DateTime.Now.Ticks}.gif");
                using (FileStream fs = new FileStream(outputPath, FileMode.Create))
                {
                    encoder.Save(fs);
                }

                MessageBox.Show("GIF created successfully and saved", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadLibrary();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error converting Sprite Sheet: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnToolsExtractSprites_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(selectedSpriteSheetPath) || !System.IO.File.Exists(selectedSpriteSheetPath))
            {
                MessageBox.Show("Please select a sprite sheet first.");
                return;
            }

            if (!int.TryParse(TxtSpriteColumns.Text, out int columns) || columns <= 0 ||
                !int.TryParse(TxtSpriteRows.Text, out int rows) || rows <= 0)
            {
                MessageBox.Show("Please enter valid numbers (greater than 0) for Columns and Rows.");
                return;
            }

            Microsoft.Win32.SaveFileDialog saveFileDialog = new Microsoft.Win32.SaveFileDialog();
            saveFileDialog.Filter = "GIF Image|*.gif";
            saveFileDialog.FileName = "ExtractedPet.gif";

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    using (MagickImageCollection collection = new MagickImageCollection())
                    {
                        using (MagickImage spriteSheet = new MagickImage(selectedSpriteSheetPath))
                        {
                            int frameWidth = (int)(spriteSheet.Width / columns);
                            int frameHeight = (int)(spriteSheet.Height / rows);

                            for (int y = 0; y < rows; y++)
                            {
                                for (int x = 0; x < columns; x++)
                                {
                                    MagickImage frame = new MagickImage(spriteSheet);
                                    frame.Crop(new MagickGeometry(x * frameWidth, y * frameHeight, (uint)frameWidth, (uint)frameHeight));
                                    frame.ResetPage(); 
                                    frame.AnimationDelay = 10; 
                                    frame.GifDisposeMethod = GifDisposeMethod.Background; 
                                    collection.Add(frame);
                                }
                            }
                        }
                        collection[0].AnimationIterations = 0;
                        collection.Write(saveFileDialog.FileName);
                    }

                    MessageBox.Show("¡GIF generated correctly!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error generating the GIF: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // === SETTINGS ===

        private void ChkAutoClearCache_Checked(object sender, RoutedEventArgs e) => autoClearCache = true;
        private void ChkAutoClearCache_Unchecked(object sender, RoutedEventArgs e) => autoClearCache = false;
        private void BtnClearCache_Click(object sender, RoutedEventArgs e)
        {
            try { GC.Collect(); GC.WaitForPendingFinalizers(); MessageBox.Show("Cache cleared.", "Info", MessageBoxButton.OK, MessageBoxImage.Information); }
            catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
        }
    }
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
            catch (Exception ex) { MessageBox.Show("Error loading image for editor: " + ex.Message); Close(); }
        }

        private void SetupUI()
        {
            Grid mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            Content = mainGrid;

            ScrollViewer scrollViewer = new ScrollViewer
            {
                Background = Brushes.Black,
                Margin = new Thickness(10),
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };
            mainGrid.Children.Add(scrollViewer);

            Grid centerGrid = new Grid { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
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
            // Gracias al Grid central y al LayoutTransform, el cálculo es directo sobre el ActualWidth de la imagen.
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
                byte targetB = originalPixels[index];
                byte targetG = originalPixels[index + 1];
                byte targetR = originalPixels[index + 2];
                byte targetA = originalPixels[index + 3];

                if (targetA == 0) return; 

                int tolerance = (int)sldTolerance.Value;

                Queue<IntPoint> queue = new Queue<IntPoint>();
                queue.Enqueue(new IntPoint(startX, startY));
                bool[,] visited = new bool[width, height];

                while (queue.Count > 0)
                {
                    IntPoint p = queue.Dequeue();
                    if (visited[p.X, p.Y]) continue;
                    visited[p.X, p.Y] = true;

                    int currentIndex = p.Y * stride + p.X * 4;
                    byte b = originalPixels[currentIndex];
                    byte g = originalPixels[currentIndex + 1];
                    byte r = originalPixels[currentIndex + 2];
                    byte a = originalPixels[currentIndex + 3];

                    if (Math.Abs(b - targetB) <= tolerance && 
                        Math.Abs(g - targetG) <= tolerance && 
                        Math.Abs(r - targetR) <= tolerance &&
                        a > 0)
                    {
                        currentPixels[currentIndex + 3] = 0; 

                        int[] dx = { 0, 0, -1, 1 };
                        int[] dy = { -1, 1, 0, 0 };

                        for (int i = 0; i < 4; i++)
                        {
                            int nx = p.X + dx[i];
                            int ny = p.Y + dy[i];

                            if (nx >= 0 && nx < width && ny >= 0 && ny < height && !visited[nx, ny])
                            {
                                queue.Enqueue(new IntPoint(nx, ny));
                            }
                        }
                    }
                }

                editableBitmap.WritePixels(new Int32Rect(0, 0, width, height), currentPixels, stride, 0);
            }
            catch (Exception ex) { MessageBox.Show("Error applying magic wand: " + ex.Message); }
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

                MessageBox.Show($"Clean image successfully saved!\nSaved into your Library as:\n{newFileName}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                Close();
            }
            catch (Exception ex) { MessageBox.Show("Error saving cleaned image: " + ex.Message); }
        }
    }
}