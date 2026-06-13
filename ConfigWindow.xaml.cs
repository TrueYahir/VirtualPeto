using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using WpfAnimatedGif;

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

        public ConfigWindow()
        {
            InitializeComponent();
            
            // Persistent Path in Documents
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
                
                if (!path.ToLower().EndsWith(".gif"))
                {
                    image.Freeze(); 
                }
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

        // === LIBRARY LOGIC (IMAGES & VIDEOS) ===

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
            
            UpdateLibraryList(fullLibraryList);
        }

        private void UpdateLibraryList(List<LibraryItem> list)
        {
            LstLibrary.ItemsSource = null;
            LstLibrary.ItemsSource = list;
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
                    TxtSelectedLibraryName.Text = "Ninguna seleccionada";
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
                if (MessageBox.Show($"¿Eliminar {selected.Name}?", "Confirmar", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
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

                newWindow.Closed += (s, args) => { selected.IsActive = false; activeLibraryWindows.Remove(selected); };
                activeLibraryWindows.Add(selected, newWindow);
                selected.IsActive = true;
                newWindow.Show();
            }
        }

        private void TxtSearchLibrary_TextChanged(object sender, TextChangedEventArgs e)
        {
            string txt = TxtSearchLibrary.Text.ToLower().Trim();
            UpdateLibraryList(string.IsNullOrEmpty(txt) ? fullLibraryList : fullLibraryList.Where(m => m.Name.ToLower().Contains(txt)).ToList());
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
                    TxtSelectedPetName.Text = "Ninguna seleccionada";
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
                if (MessageBox.Show($"¿Eliminar mascota {selected.Name}?", "Confirmar", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
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

                newWindow.Closed += (s, args) => { selected.IsActive = false; activePetsWindows.Remove(selected); };
                activePetsWindows.Add(selected, newWindow);
                selected.IsActive = true;
                newWindow.Show();
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

        // === SETTINGS ===

        private void BtnClearCache_Click(object sender, RoutedEventArgs e)
        {
            try { GC.Collect(); GC.WaitForPendingFinalizers(); MessageBox.Show("Cache cleared.", "Info", MessageBoxButton.OK, MessageBoxImage.Information); }
            catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
        }
    }
}