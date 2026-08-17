using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Input;
using System.Windows.Ink;
using System.Windows.Interop;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
using System.Windows.Resources;
using Microsoft.Win32;
using WpfAnimatedGif;
using ImageMagick;
using System.Runtime.InteropServices;

using VirtualPeto.Tools;
using VirtualPeto.Objects;


namespace VirtualPeto
{
    // === MODELS ===

    public class LibraryItem : INotifyPropertyChanged
    {
        public string Name { get; set; } = string.Empty;
        public string FullPath { get; set; } = string.Empty;
        public string DisplayName => System.IO.Path.GetFileNameWithoutExtension(Name);
        public string FileType
        {
            get
            {
                if(FullPath.Contains("Extracted_VPets") || FullPath.EndsWith(".vpet")) return "VPET";
                if(IsVideo) return "VID";
                string ext = System.IO.Path.GetExtension(FullPath).ToUpper().Replace(".", "");
                return string.IsNullOrEmpty(ext) ? "UNKNOWN" : ext;
            }
        }
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
        public bool HasSound {get; set;}
        public string? SoundPath {get; set;}
        private double _volume = 0.5;
        public double Volume
        {
            get => _volume;
            set{_volume = value; OnPropertyChanged(); }
        }
        private bool _isFavorite;
        public bool IsFavorite
        {
            get {return _isFavorite;}
            set { _isFavorite = value; OnPropertyChanged(); }
        }
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

    public class VPetConfigData
    {
        public string? Name{get; set;}
        public string? GifFile{get; set;}
        public string? SoundFile{get; set;}
        public double Volume{get; set;} = 0.5;
        public bool IsSmartPet {get; set;} = false;
    }


    public class PetItem : INotifyPropertyChanged
    {
        public string DirectoryPath { get; set; } = string.Empty;
        public PetConfig Config { get; set; } = new PetConfig();
        public PetMetadata? SmartConfig {get; set;}
        public ImageSource? Thumbnail { get; set; }
        public string Name => SmartConfig != null && SmartConfig.IsSmartPet ? SmartConfig.PetName : Config.Name;

        private bool _isActive;
        public bool IsActive
        {
            get { return _isActive; }
            set { _isActive = value; OnPropertyChanged(); OnPropertyChanged(nameof(VisibilityIndicator)); }
        }
        public Visibility VisibilityIndicator => _isActive ? Visibility.Visible : Visibility.Collapsed;

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        private bool _isFavorite;
        public bool IsFavorite
        {
            get {return _isFavorite;}
            set { _isFavorite = value; OnPropertyChanged(); }
        }
    }
    internal struct IntPoint
    {
        public int X, Y;
        public IntPoint(int x, int y) { X = x; Y = y; }
    }

    // === MAIN WINDOW CLASS ===

    public partial class ConfigWindow : Window
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern IntPtr GetDesktopWindow();

        [DllImport("user32.dll")]
        private static extern IntPtr GetShellWindow();

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hwnd, out RECT lpRect);

        private string libraryPath;
        private string petsPath;
        private List<LibraryItem> fullLibraryList = new List<LibraryItem>();
        private List<PetItem> fullPetsList = new List<PetItem>();
        
        private Dictionary<string, MainWindow> activeLibraryWindows = new Dictionary<string, MainWindow>(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, Window> activePetsWindows = new Dictionary<string, Window>(StringComparer.OrdinalIgnoreCase);

        private int TotalActiveDesktopWindows => activeLibraryWindows.Count + activePetsWindows.Count;

        private readonly string[] validImages = { ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".webp", ".ico", ".tiff" };
        private readonly string[] validVideos = { ".mp4", ".avi", ".mkv", ".webm", ".mov", ".wmv" };
        private string[] selectedToolsGifImages = Array.Empty<string>();
        private string selectedToolsBgImage = string.Empty;
        private string selectedToolsGifBackgroundPath = string.Empty;
        private string selectedSpriteSheetPath = string.Empty;
        private bool autoClearCache = false;
        private bool isLibraryGridView = false;
        private bool isPetGridView = false;
        private int petLimit = 5;
        private const int MinPetLimit = 1;
        private const int MaxPetLimit = 20;
        private bool runOnStartup = false;
        private System.Windows.Forms.NotifyIcon _notifyIcon;
        private bool _isClosingFR = false;
        private System.Windows.Media.MediaPlayer _librarySoundPlayer = new System.Windows.Media.MediaPlayer();
        private MediaPlayer _previewAudioPlayer = new MediaPlayer();
        private bool _isPreviewPlaying = false;
        private string _currentPreviewPath = string.Empty;
        public static bool IsOverlappingEnabled = false;
        public static bool IsPetLocked {get; set;} = false;
        private DispatcherTimer _fullScreenCheckTimer = new DispatcherTimer();
        private DispatcherTimer _previewAnimTimer = new DispatcherTimer();
        private int _previewCurrentFrame = 0;
        private AnimationData? _currentPreviewData;
        private BitmapImage? _previewSheet;
        private string favoritesFilePath;
        private List<string> favoritePaths = new List<string>();


        public ConfigWindow()
        {
            InitializeComponent();

            LoadConfig();
            
            _fullScreenCheckTimer.Interval = TimeSpan.FromSeconds(2);
            _fullScreenCheckTimer.Tick += CheckFullScreenApp;
            _fullScreenCheckTimer.Start();

            _notifyIcon = new System.Windows.Forms.NotifyIcon();
            _notifyIcon.Icon = LoadEmbeddedIcon();
            _notifyIcon.Text = "VirtualPeto";
            _notifyIcon.Visible = true;
            _notifyIcon.DoubleClick += (s, args) => { this.Show(); this.WindowState = WindowState.Normal; };
            var contextMenu = new System.Windows.Forms.ContextMenuStrip();
            contextMenu.Items.Add("Exit", null, (s, args) =>
            {
               _isClosingFR = true;
               _notifyIcon.Dispose();
               System.Windows.Application.Current.Shutdown(); 
            });
            _notifyIcon.ContextMenuStrip = contextMenu;


            string baseDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "VirtualPeto");
            
            libraryPath = Path.Combine(baseDataPath, "Library");
            petsPath = Path.Combine(baseDataPath, "Pets");
            
            if (!Directory.Exists(libraryPath)) Directory.CreateDirectory(libraryPath);
            if (!Directory.Exists(petsPath)) Directory.CreateDirectory(petsPath);
            favoritesFilePath = Path.Combine(baseDataPath, "favorites.json");
            
            LoadFavorites();
            LoadLibrary();
            LoadPets();
            TxtPetLimit.Text = petLimit.ToString();
            ChkRunOnStartup.IsChecked = runOnStartup;
            if (SettingsManager.Current.StartFavoritesOnStartup)
            {
                StartFavoritePetsAutomatically();
            }

            _previewAnimTimer.Tick += (s, e) =>
            {
                if(_previewSheet == null || _currentPreviewData == null) return;
                int fw = _currentPreviewData.FrameWidth;
                int fh = _currentPreviewData.FrameHeight;
                int cols = _previewSheet.PixelWidth / fw;
                int x = (_previewCurrentFrame % cols) * fw;
                int y = (_previewCurrentFrame / cols) * fh;
                ImgPetPreview.Source = new CroppedBitmap(_previewSheet, new Int32Rect(x, y, fw, fh));
                _previewCurrentFrame = (_previewCurrentFrame + 1) % _currentPreviewData.TotalFrames;
            };
        }

        private System.Drawing.Icon LoadEmbeddedIcon()
        {
            Uri uri = new Uri("pack://application:,,,/Assets/icon.ico", UriKind.Absolute);
            StreamResourceInfo resource = Application.GetResourceStream(uri);
            if(resource == null)
            {
                throw new FileNotFoundException("Embedded icon not found");
            }
            using MemoryStream ms = new MemoryStream();
            resource.Stream.CopyTo(ms);
            ms.Position = 0;
            return new System.Drawing.Icon(ms);
        }
        private void LoadConfig()
        {
            ChkRunOnStartup.IsChecked = SettingsManager.Current.RunOnStartup;
            ChkStartFavorites.IsChecked = SettingsManager.Current.StartFavoritesOnStartup;
            ChkAutoClearCache.IsChecked = SettingsManager.Current.AutoClearCache;
            ChkOverlapping.IsChecked = SettingsManager.Current.AllowOverlay;
            ChkLockPet.IsChecked = SettingsManager.Current.LockPetPosition;
            ChkPlaySounds.IsChecked = SettingsManager.Current.AllowSounds;
            ChkSecondMonitor.IsChecked = SettingsManager.Current.AllowSecondMonitor;
            
            TxtPetLimit.Text = SettingsManager.Current.DesktopPetLimit.ToString();
            TxtSleepTime.Text = SettingsManager.Current.SleepTimeMinutes.ToString();
            if (TxtDefaultFolder != null && !string.IsNullOrEmpty(SettingsManager.Current.DefaultSaveFolder))
            {
                TxtDefaultFolder.Text = SettingsManager.Current.DefaultSaveFolder;
            }
        }
        protected override void OnClosing(CancelEventArgs e)
        {
            int activePets = System.Windows.Application.Current.Windows.Count - 1;
            if(activePets > 0 && !_isClosingFR)
            {
                e.Cancel = true;
                this.Hide();
                _notifyIcon.ShowBalloonTip(3000, "VirtualPeto", "VirtualPeto still running", System.Windows.Forms.ToolTipIcon.Info);
            }
            else
            {
                _notifyIcon.Dispose();
            }
            base.OnClosing(e);
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
        private void SldSize_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            int size = (int)e.NewValue;
            if (TxtSizeLabel != null) 
            {
                TxtSizeLabel.Text = $"Size (Pixels): ({size}x{size})";
            }
            
            if (LstLibrary.SelectedItem is LibraryItem selected && selected.IsActive)
            {
                string libraryKey = NormalizePath(selected.FullPath);
                if (activeLibraryWindows.TryGetValue(libraryKey, out MainWindow? openWindow))
                {
                    if (openWindow != null)
                    {
                        openWindow.Width = size;
                        openWindow.Height = size;
                    }
                }
            }
        }

        private void SldSize_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (sender is Slider slider)
            {
                slider.Value += e.Delta > 0 ? 5 : -5;
                e.Handled = true; 
            }
        }
        

        // === LIBRARY LOGIC ===

        private void LoadLibrary()
        {
            if (!Directory.Exists(libraryPath)) Directory.CreateDirectory(libraryPath);
            HashSet<string> favoritesSet = new HashSet<string>();
            if (File.Exists("favorites.json"))
            {
                try 
                { 
                    var favs = JsonSerializer.Deserialize<List<string>>(File.ReadAllText("favorites.json"));
                    if (favs != null) favoritesSet = new HashSet<string>(favs.Select(p => p.ToLowerInvariant()));
                } 
                catch { System.Diagnostics.Debug.WriteLine("Error reading favorites.json"); }
            }
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
                        IsActive = activeLibraryWindows.ContainsKey(NormalizePath(path)),
                        IsFavorite = favoritePaths.Contains(path)
                    };
                }).ToList();

            var vpetFiles = Directory.GetFiles(libraryPath, "*.vpet");
            string vpetExtractDir = Path.Combine(libraryPath, "Extracted_VPets");
            
            foreach(var vpetPath in vpetFiles)
            {
                try
                {
                    string petName = Path.GetFileNameWithoutExtension(vpetPath);
                    string targetExtractPath = Path.Combine(vpetExtractDir, petName);
                    
                    if(!Directory.Exists(targetExtractPath))
                    {
                        System.IO.Compression.ZipFile.ExtractToDirectory(vpetPath, targetExtractPath);
                    }
                    
                    string configJsonPath = Path.Combine(targetExtractPath, "config.json");
                    if (File.Exists(configJsonPath))
                    {
                        string jsonContent = File.ReadAllText(configJsonPath); 
                        var vpetData = JsonSerializer.Deserialize<VPetConfigData>(jsonContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        
                        if (vpetData != null)
                        {
                            string gifFile = string.IsNullOrEmpty(vpetData.GifFile) ? "pet.gif" : vpetData.GifFile;
                            string fullGifPath = Path.Combine(targetExtractPath, gifFile);
                            string? soundFile = vpetData.SoundFile;
                            
                            var item = new LibraryItem
                            {
                                Name = string.IsNullOrEmpty(vpetData.Name) ? petName : vpetData.Name,
                                FullPath = fullGifPath,
                                IsVideo = false,
                                HasSound = !string.IsNullOrEmpty(soundFile),
                                SoundPath = !string.IsNullOrEmpty(soundFile) ? Path.Combine(targetExtractPath, soundFile) : null,
                                Volume = vpetData.Volume,
                                Thumbnail = LoadImageToMemory(fullGifPath), 
                                IsActive = activeLibraryWindows.ContainsKey(NormalizePath(fullGifPath)),
                                IsFavorite = favoritePaths.Contains(fullGifPath)
                            };
                            
                            fullLibraryList.Add(item);
                        }
                    }
                }
                catch(Exception ex)
                {
                    Debug.WriteLine($"Error loading .vpet: {ex.Message}");
                }
            }
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
            if (ChkOnlyFavorites != null && ChkOnlyFavorites.IsChecked == true)
            {
                filteredList = filteredList.Where(m => m.IsFavorite);
            }

            LstLibrary.ItemsSource = null;
            LstLibrary.ItemsSource = filteredList.ToList();
            UpdateLibraryViewMode();
        }

        private void UpdateLibraryViewMode()
        {
            if (LstLibrary == null) return;

            if (isLibraryGridView)
            {
                LstLibrary.ItemTemplate = (DataTemplate)Resources["LibraryGridTemplate"];
                LstLibrary.ItemsPanel = (ItemsPanelTemplate)Resources["LibraryGridItemsPanel"];
                BtnToggleLibraryView.Content = "List view";
            }
            else
            {
                LstLibrary.ItemTemplate = (DataTemplate)Resources["LibraryListTemplate"];
                LstLibrary.ItemsPanel = (ItemsPanelTemplate)Resources["LibraryListItemsPanel"];
                BtnToggleLibraryView.Content = "Grid view";
            }
        }
        

        private void BtnToggleLibraryView_Click(object sender, RoutedEventArgs e)
        {
            isLibraryGridView = !isLibraryGridView;
            UpdateLibraryViewMode();
        }
        

        private void Filters_Changed(object sender, RoutedEventArgs e)
        {
            ApplyLibraryFilters();
            if (fullPetsList != null && fullPetsList.Count > 0) 
            {
                ApplyPetFilters(); 
            }
        }

        private void BtnAddLibrary_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog { 
                Filter = "Media Files|*.png;*.jpg;*.jpeg;*.gif;*.bmp;*.webp;*.mp4;*.avi;*.mkv;*.webm;*.mov;*.vpet|All Files|*.*" 
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
                    if(LibraryActionButtonsPanel != null) LibraryActionButtonsPanel.Visibility = Visibility.Collapsed;
                    TxtSelectedLibraryName.Text = "None selected";
                    ImageBehavior.SetAnimatedSource(ImgLibraryPreview, null);
                    ImgLibraryPreview.Source = null;
                    ImgLibraryPreview.Visibility = Visibility.Visible;
                    VidLibraryPreview.Visibility = Visibility.Collapsed;
                    TxtEmptyLibraryPreview.Visibility = Visibility.Visible;
                    PnlSize.Visibility = Visibility.Collapsed;
                    PnlFps.Visibility = Visibility.Collapsed;
                    
                    if (PnlAudio != null) PnlAudio.Visibility = Visibility.Collapsed;
                    return;
                }
                if(LibraryActionButtonsPanel != null) LibraryActionButtonsPanel.Visibility = Visibility.Visible;
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

                    if (selected.FullPath.ToLower().EndsWith(".gif"))
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

                if (PnlAudio != null)
                {
                    PnlAudio.Visibility = selected.HasSound ? Visibility.Visible : Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading preview: {ex.Message}");
                TxtSelectedLibraryName.Text = "Error loading preview";
                TxtEmptyLibraryPreview.Visibility = Visibility.Visible;
                ImageBehavior.SetAnimatedSource(ImgLibraryPreview, null);
                ImgLibraryPreview.Source = null;
                VidLibraryPreview.Stop();
                VidLibraryPreview.Visibility = Visibility.Collapsed;
                PnlSize.Visibility = Visibility.Collapsed;
                PnlFps.Visibility = Visibility.Collapsed;
                if (PnlAudio != null) PnlAudio.Visibility = Visibility.Collapsed;
            }
        }
        private void List_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ActionButtonsPanel != null)
            {
                var lista = sender as System.Windows.Controls.ListBox;

                if (lista?.SelectedItem == null)
                {
                    ActionButtonsPanel.Visibility = Visibility.Collapsed;
                }
                else
                {
                    ActionButtonsPanel.Visibility = Visibility.Visible;
                }
            }
        }

        private void VidLibraryPreview_MediaEnded(object sender, RoutedEventArgs e)
        {
            VidLibraryPreview.Position = TimeSpan.Zero;
            VidLibraryPreview.Play();
        }

        private void BtnIncreaseFps_Click(object sender, RoutedEventArgs e) 
        { 
            if (int.TryParse(TxtFps.Text, out int current) && current < 60) 
            {
                int newFps = current + 1;
                TxtFps.Text = newFps.ToString(); 
                UpdateActivePetFps(newFps); 
            }
        }

        private void BtnDecreaseFps_Click(object sender, RoutedEventArgs e) 
        { 
            if (int.TryParse(TxtFps.Text, out int current) && current > 1) 
            {
                int newFps = current - 1;
                TxtFps.Text = newFps.ToString(); 
                UpdateActivePetFps(newFps); 
            }
        }
        private void UpdateActivePetFps(int fps)
        {
            if (LstLibrary.SelectedItem is LibraryItem selected && selected.IsActive && !selected.IsVideo && selected.FullPath.ToLower().EndsWith(".gif"))
            {
                string libraryKey = NormalizePath(selected.FullPath);
                if (activeLibraryWindows.TryGetValue(libraryKey, out MainWindow? openWindow))
                {
                    if (openWindow != null)
                    {
                        var controller = ImageBehavior.GetAnimationController(openWindow.PetImage);
                        if (controller != null && fps > 0)
                        {
                            ImageBehavior.SetAnimationDuration(openWindow.PetImage, new Duration(TimeSpan.FromMilliseconds((1000.0 / fps) * controller.FrameCount)));
                        }
                    }
                }
            }
        }

        private void SldVolume_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            int volumePercentage = (int)e.NewValue;
            
            if (TxtVolumeLabel != null) 
            {
                TxtVolumeLabel.Text = $"Volume: {volumePercentage}%";
            }
            
            if (LstLibrary.SelectedItem is LibraryItem selected && selected.IsActive)
            {
                string libraryKey = NormalizePath(selected.FullPath);
                if (activeLibraryWindows.TryGetValue(libraryKey, out MainWindow? openWindow))
                {
                    if (openWindow?.PetVideo != null) 
                    {
                        openWindow.PetVideo.Volume = volumePercentage / 100.0;
                    }
                }
            }
        }
        private void SldVolume_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (sender is Slider slider)
            {
                slider.Value += e.Delta > 0 ? 5 : -5;
                e.Handled = true; 
            }
        }

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

                        string selectedKey = NormalizePath(selected.FullPath);
                        if (activeLibraryWindows.TryGetValue(selectedKey, out MainWindow? openWindow))
                        {
                            openWindow.Close();
                        }

                        fullLibraryList.Remove(selected);
                        GC.Collect(); 
                        GC.WaitForPendingFinalizers();

                        string fileToDelete = selected.FullPath;

                        if (fileToDelete.Contains("Extracted_VPets"))
                        {
                            string folderName = Path.GetFileName(Path.GetDirectoryName(fileToDelete)!);
                            string vpetOriginalFile = Path.Combine(libraryPath, folderName + ".vpet");
                            string targetExtractPath = Path.Combine(libraryPath, "Extracted_VPets", folderName);

                            if (File.Exists(vpetOriginalFile)) File.Delete(vpetOriginalFile);
                            if (Directory.Exists(targetExtractPath)) Directory.Delete(targetExtractPath, true);
                        }
                        else
                        {
                            if (File.Exists(fileToDelete)) File.Delete(fileToDelete);
                        }
                        
                        LoadLibrary();
                    }
                    catch (Exception ex) 
                    { 
                        MessageBox.Show("Error deleting: " + ex.Message); 
                        LoadLibrary(); 
                    }
                }
            }
        }

        private void BtnLaunchLibrary_Click(object sender, RoutedEventArgs e)
        {
            if (LstLibrary.SelectedItem is LibraryItem selected)
            {
                string libraryKey = NormalizePath(selected.FullPath);
                if (activeLibraryWindows.ContainsKey(libraryKey)) { activeLibraryWindows[libraryKey].Activate(); return; }

                if (TotalActiveDesktopWindows >= petLimit) return;

                string soundPath = selected.HasSound && !string.IsNullOrEmpty(selected.SoundPath) ? selected.SoundPath : string.Empty;
                string mediaPath = selected.FullPath;
                string? directory = System.IO.Path.GetDirectoryName(mediaPath);
                if (!string.IsNullOrEmpty(directory))
                {
                    string configPath = System.IO.Path.Combine(directory, "config.json");
                    try
                    {
                        string jsonString = System.IO.File.ReadAllText(configPath);
                        PetMetadata? metadata = System.Text.Json.JsonSerializer.Deserialize<PetMetadata>(jsonString);
                        if(metadata != null)
                        {
                            if (!string.IsNullOrEmpty(metadata.IdleAnimation.FilePath))
                            {
                                mediaPath = System.IO.Path.Combine(directory, metadata.IdleAnimation.FilePath);
                            }
                            if (!string.IsNullOrEmpty(metadata.IdleAnimation.SoundPath))
                            {
                                soundPath = System.IO.Path.Combine(directory, metadata.IdleAnimation.SoundPath);
                            }
                        }
                    }catch(Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[ConfigWindow] Error reading config.json: {ex.Message}");
                    }
                }
                
                MainWindow newWindow = new MainWindow(
                    mediaPath: mediaPath,
                    isVideo: selected.IsVideo,
                    size: SldSize.Value,
                    soundPath: soundPath,
                    volume: selected.Volume
                );
                
                newWindow.ShowInTaskbar = false;
                
                if (!selected.IsVideo && selected.FullPath.ToLower().EndsWith(".gif"))
                {
                    if (int.TryParse(TxtFps.Text, out int fps) && fps > 0)
                    {
                        var controller = ImageBehavior.GetAnimationController(newWindow.PetImage);
                        if (controller != null) ImageBehavior.SetAnimationDuration(newWindow.PetImage, new Duration(TimeSpan.FromMilliseconds((1000 / fps) * controller.FrameCount)));
                    }
                }

                newWindow.Closed += (s, args) => 
                { 
                    selected.IsActive = false; 
                    activeLibraryWindows.Remove(libraryKey); 
                    ApplyLibraryFilters();
                    if (autoClearCache) ClearApplicationCache(includeActiveWindows: false);
                };
                
                activeLibraryWindows.Add(libraryKey, newWindow);
                selected.IsActive = true;
                newWindow.Show();
                ApplyLibraryFilters(); 
            }
        }
        private void BtnEditGifPackage_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Virtual Pet Files (*.vpet)|*.vpet", Title = "Select a GIF package to edit"
            };
            
            if(openFileDialog.ShowDialog() == true)
            {
                try
                {
                    bool isSmartPet = false;
                    using (ZipArchive archive = ZipFile.OpenRead(openFileDialog.FileName))
                    {
                        ZipArchiveEntry? jsonEntry = archive.GetEntry("config.json");
                        if (jsonEntry != null)
                        {
                            using (StreamReader reader = new StreamReader(jsonEntry.Open()))
                            {
                                string jsonContent = reader.ReadToEnd();
                                using (JsonDocument doc = JsonDocument.Parse(jsonContent))
                                {
                                    if (doc.RootElement.TryGetProperty("IsSmartPet", out JsonElement isSmartElement))
                                    {
                                        if (isSmartElement.ValueKind == JsonValueKind.True) isSmartPet = true;
                                        else if (isSmartElement.ValueKind == JsonValueKind.String && isSmartElement.GetString()?.ToLower() == "true") isSmartPet = true;
                                        else if (isSmartElement.ValueKind == JsonValueKind.Number && isSmartElement.GetInt32() == 1) isSmartPet = true;
                                    }
                                }
                            }
                        }
                    }
                    if (isSmartPet)
                    {
                        MessageBox.Show("This is a Smart Pet. Please use the Smart Pet editor instead.", "Incompatible File", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }
                    GifPackageWindow editorWindow = new GifPackageWindow();
                    editorWindow.Owner = this;
                    editorWindow.LoadPackageForEditing(openFileDialog.FileName);
                    editorWindow.ShowDialog();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error reading file: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnCloseLibrary_Click(object sender, RoutedEventArgs e)
        {
            if (LstLibrary.SelectedItem is LibraryItem selected)
            {
                string libraryKey = NormalizePath(selected.FullPath);
                if (activeLibraryWindows.TryGetValue(libraryKey, out MainWindow? openWindow))
                {
                    openWindow.Close();
                }
            }
        }

        private void BtnTestLibrarySound_Click(object sender, RoutedEventArgs e)
        {
            if (LstLibrary.SelectedItem is LibraryItem selected && selected.HasSound && !string.IsNullOrEmpty(selected.SoundPath))
            {
                if (_currentPreviewPath != selected.SoundPath)
                {
                    _librarySoundPlayer.Open(new Uri(selected.SoundPath));
                    _librarySoundPlayer.Volume = selected.Volume;
                    
                    _librarySoundPlayer.MediaEnded += (s, args) => 
                    {
                        _isPreviewPlaying = false;
                        _librarySoundPlayer.Position = TimeSpan.Zero;
                    };
                    
                    _currentPreviewPath = selected.SoundPath;
                    _isPreviewPlaying = false;
                }

                if (_isPreviewPlaying)
                {
                    _librarySoundPlayer.Pause();
                    _isPreviewPlaying = false;
                }
                else
                {
                    _librarySoundPlayer.Play();
                    _isPreviewPlaying = true;
                }
            }
        }

        // === SMART PETS LOGIC ===

        private void LoadPets()
        {
            if (!Directory.Exists(petsPath)) return;
            fullPetsList.Clear();
            HashSet<string> favoritesSet = new HashSet<string>();
            if (File.Exists("favorites.json"))
            {
                try 
                { 
                    var favs = JsonSerializer.Deserialize<List<string>>(File.ReadAllText("favorites.json"));
                    if (favs != null) favoritesSet = new HashSet<string>(favs.Select(p => p.ToLowerInvariant()));
                } 
                catch { System.Diagnostics.Debug.WriteLine("Error reading favorites.json"); }
            }

            foreach (string folder in Directory.GetDirectories(petsPath))
            {
                string jsonFile = Path.Combine(folder, "config.json");
                if (!File.Exists(jsonFile)) continue;

                try
                {
                    string jsonString = File.ReadAllText(jsonFile);
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    PetMetadata? smartConfig = null;
                    PetConfig? oldConfig = null;
                    string idlePath = string.Empty;

                    using (JsonDocument doc = JsonDocument.Parse(jsonString))
                    {
                        if (doc.RootElement.TryGetProperty("IsSmartPet", out var isSmart) && isSmart.GetBoolean())
                        {
                            smartConfig = JsonSerializer.Deserialize<PetMetadata>(jsonString, options);
                            if (smartConfig?.IdleAnimation != null) 
                                idlePath = Path.Combine(folder, smartConfig.IdleAnimation.FilePath ?? string.Empty);
                        }
                        else
                        {
                            oldConfig = JsonSerializer.Deserialize<PetConfig>(jsonString, options);
                            if (oldConfig?.Animations != null) 
                                idlePath = Path.Combine(folder, oldConfig.Animations.Idle ?? string.Empty);
                        }
                    }

                    if (smartConfig != null || oldConfig != null)
                    {
                        string normalizedFolder = Path.GetFullPath(folder);
                        ImageSource? thumbnail = null;

                        if (!string.IsNullOrEmpty(idlePath) && File.Exists(idlePath))
                        {
                            var bmp = LoadImageToMemory(idlePath);
                            if (smartConfig != null && bmp != null)
                            {
                                int fw = smartConfig.IdleAnimation?.FrameWidth > 0 ? smartConfig.IdleAnimation.FrameWidth : bmp.PixelWidth;
                                int fh = smartConfig.IdleAnimation?.FrameHeight > 0 ? smartConfig.IdleAnimation.FrameHeight : bmp.PixelHeight;
                                
                                if (fw <= bmp.PixelWidth && fh <= bmp.PixelHeight)
                                    thumbnail = new CroppedBitmap(bmp, new Int32Rect(0, 0, fw, fh));
                                else
                                    thumbnail = bmp;
                            }
                            else
                            {
                                thumbnail = bmp;
                            }
                        }

                        string petName = smartConfig?.PetName ?? oldConfig?.Name ?? Path.GetFileName(normalizedFolder);
                        fullPetsList.Add(new PetItem
                        {
                            DirectoryPath = normalizedFolder,
                            Config = oldConfig ?? new PetConfig(),
                            SmartConfig = smartConfig,
                            Thumbnail = thumbnail,
                            IsActive = activePetsWindows.ContainsKey(normalizedFolder),
                            IsFavorite = favoritePaths.Contains(normalizedFolder)
                        });
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Error reading pet JSON: " + ex.Message);
                }
            }

            ApplyPetFilters();
        }

        private void ApplyPetFilters()
        {
            if (fullPetsList == null || LstPets == null) return;

            var filteredList = fullPetsList.AsEnumerable();

            string searchTxt = TxtSearchPet?.Text?.ToLower().Trim() ?? "";
            if (!string.IsNullOrEmpty(searchTxt))
            {
                filteredList = filteredList.Where(p => p.Name != null && p.Name.ToLower().Contains(searchTxt));
            }

            if (ChkFilterActivePets != null && ChkFilterActivePets.IsChecked == true)
            {
                filteredList = filteredList.Where(p => p.IsActive);
            }

            UpdatePetsList(filteredList.ToList());
            UpdatePetViewMode(); 
        }

        private void UpdatePetsList(List<PetItem> list)
        {
            LstPets.ItemsSource = null;
            LstPets.ItemsSource = list;
        }

        private void UpdatePetViewMode()
        {
            if (LstPets == null) return;

            if (isPetGridView)
            {
                var gridTemplate = Resources["PetGridTemplate"] as DataTemplate;
                var gridPanel = Resources["PetGridItemsPanel"] as ItemsPanelTemplate;
                
                if (gridTemplate != null) LstPets.ItemTemplate = gridTemplate;
                if (gridPanel != null) LstPets.ItemsPanel = gridPanel;
                
                if (BtnTogglePetView != null) BtnTogglePetView.ToolTip = "List view";
            }
            else
            {
                var listTemplate = Resources["PetListTemplate"] as DataTemplate;
                var listPanel = Resources["PetListItemsPanel"] as ItemsPanelTemplate;
                
                if (listTemplate != null) LstPets.ItemTemplate = listTemplate;
                if (listPanel != null) LstPets.ItemsPanel = listPanel;
                
                if (BtnTogglePetView != null) BtnTogglePetView.ToolTip = "Grid view";
                if(BtnTogglePetView != null) BtnTogglePetView.IsChecked = false;
            }
        }

        private void BtnTogglePetView_Click(object sender, RoutedEventArgs e)
        {
            isPetGridView = !isPetGridView;
            UpdatePetViewMode();
        }

        private void TxtSearchPet_TextChanged(object sender, RoutedEventArgs e)
        {
            ApplyPetFilters();
        }

        private void ChkFilterActivePets_CheckedChanged(object sender, RoutedEventArgs e)
        {
            ApplyPetFilters();
        }

        private void BtnAddPet_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog { Title = "Select a smart pet", Filter = "Virtual Pet (*.vpet)|*.vpet" };
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    string FileNameWithoutExt = Path.GetFileNameWithoutExtension(dialog.FileName);
                    string targetPath = Path.Combine(petsPath, FileNameWithoutExt);

                    if (!Directory.Exists(targetPath))
                    {
                        System.IO.Compression.ZipFile.ExtractToDirectory(dialog.FileName, targetPath);
                        LoadPets(); 
                    }
                    else
                    {
                        MessageBox.Show("A pet with this name already exists.", "Import error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                catch (Exception ex) { MessageBox.Show("Error adding pet: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error); }
            }
        }

        private void LstPets_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try 
            {
                _previewAnimTimer.Stop();
                if (ActionButtonsPanel != null)
                {
                    ActionButtonsPanel.Visibility = LstPets.SelectedItem == null ? Visibility.Collapsed : Visibility.Visible;
                }

                if (LstPets.SelectedItem == null || !(LstPets.SelectedItem is PetItem))
                {
                    TxtSelectedPetName.Text = "None selected";
                    ImageBehavior.SetAnimatedSource(ImgPetPreview, null);
                    ImgPetPreview.Source = null;
                    TxtEmptyPetPreview.Visibility = Visibility.Visible;
                    TxtEmptyPetPreview.Text = "Select a pet to preview";
                    return;
                }
                PetItem selected = (PetItem)LstPets.SelectedItem;

                TxtEmptyPetPreview.Visibility = Visibility.Collapsed;
                TxtSelectedPetName.Text = selected.Name; 

                if (selected.SmartConfig != null && selected.SmartConfig.IdleAnimation != null && selected.SmartConfig.IdleAnimation.IsSpriteSheet)
                {
                    string spritePath = Path.Combine(selected.DirectoryPath, selected.SmartConfig.IdleAnimation.FilePath ?? string.Empty);
                    
                    if (File.Exists(spritePath))
                    {
                        _currentPreviewData = selected.SmartConfig.IdleAnimation;
                        _previewSheet = LoadImageToMemory(spritePath);
                        
                        if (_previewSheet != null)
                        {
                            ImageBehavior.SetAnimatedSource(ImgPetPreview, null);
                            _previewCurrentFrame = 0;
                            int fps = _currentPreviewData.Fps > 0 ? _currentPreviewData.Fps : 10;
                            _previewAnimTimer.Interval = TimeSpan.FromMilliseconds(1000.0 / fps);
                            _previewAnimTimer.Start();
                            return;
                        }
                    }
                }

                string idleAnim = string.Empty;
                if (selected.SmartConfig != null && selected.SmartConfig.IdleAnimation != null)
                {
                    idleAnim = selected.SmartConfig.IdleAnimation.FilePath ?? string.Empty;
                }
                else if (selected.Config != null && selected.Config.Animations != null)
                {
                    idleAnim = selected.Config.Animations.Idle ?? string.Empty;
                }

                if (string.IsNullOrEmpty(idleAnim))
                {
                    ImageBehavior.SetAnimatedSource(ImgPetPreview, null);
                    ImgPetPreview.Source = null;
                    TxtEmptyPetPreview.Visibility = Visibility.Visible;
                    TxtEmptyPetPreview.Text = "No preview available";
                    return;
                }

                string imgPath = Path.Combine(selected.DirectoryPath, idleAnim);

                if (!File.Exists(imgPath))
                {
                    ImageBehavior.SetAnimatedSource(ImgPetPreview, null);
                    ImgPetPreview.Source = null;
                    TxtEmptyPetPreview.Visibility = Visibility.Visible;
                    TxtEmptyPetPreview.Text = "Preview file not found";
                    return;
                }

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
            catch (Exception ex) 
            { 
                System.Diagnostics.Debug.WriteLine("Preview Error: " + ex.Message);
                ImageBehavior.SetAnimatedSource(ImgPetPreview, null);
                ImgPetPreview.Source = null;
                TxtEmptyPetPreview.Visibility = Visibility.Visible;
                TxtEmptyPetPreview.Text = "Error loading preview";
                _previewAnimTimer.Stop();
            }
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

                        if (!string.IsNullOrEmpty(selected.DirectoryPath))
                        {
                            string normalizedPath = Path.GetFullPath(selected.DirectoryPath);
                            if (activePetsWindows.TryGetValue(normalizedPath, out Window? openWindow))
                            {
                            openWindow.Close();
                            }
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
                string petKey = NormalizePath(selected.DirectoryPath);

                if (activePetsWindows.ContainsKey(petKey))
                {
                    activePetsWindows[petKey].Activate();
                    return;
                }

                if (TotalActiveDesktopWindows >= petLimit) return;

                Window newWindow;

                if (selected.SmartConfig != null && selected.SmartConfig.IsSmartPet)
                {
                    newWindow = new SmartPetWindow(selected.SmartConfig, selected.DirectoryPath);
                }
                else
                {
                    string idleAnim = selected.Config?.Animations?.Idle ?? string.Empty;
                    string idlePath = string.IsNullOrEmpty(idleAnim) ? string.Empty : Path.Combine(selected.DirectoryPath, idleAnim);
                    double scale = selected.Config?.Scale ?? 1.0;
                    
                    newWindow = new MainWindow(idlePath, false, 150 * scale, string.Empty, 0.5);
                }

                newWindow.ShowInTaskbar = false;
                newWindow.Closed += (s, args) =>
                {
                    selected.IsActive = false;
                    activePetsWindows.Remove(petKey);
                    UpdateActivePetStatus();
                    if (autoClearCache) ClearApplicationCache(includeActiveWindows: false);
                };

                activePetsWindows.Add(petKey, newWindow);
                selected.IsActive = true;
                newWindow.Show();
                UpdateActivePetStatus();
            }
        }

        private void BtnClosePet_Click(object sender, RoutedEventArgs e)
        {
            if (LstPets.SelectedItem is PetItem selected)
            {
                string petKey = NormalizePath(selected.DirectoryPath);
                if (activePetsWindows.TryGetValue(petKey, out Window? openWindow))
                {
                    openWindow.Close();
                }
            }
        }

        private void TxtSearchPet_TextChanged(object sender, TextChangedEventArgs e)
        {
            string txt = TxtSearchPet.Text.ToLower().Trim();
            UpdatePetsList(string.IsNullOrEmpty(txt) ? fullPetsList : fullPetsList.Where(m => m.Name.ToLower().Contains(txt)).ToList());
        }
        //CREATE VIRTUAL PET
        private void BtnOpenPetCreator_Click(object sender, RoutedEventArgs e)
        {
            PetCreatorWindow PetCreator = new PetCreatorWindow();
            PetCreator.ShowDialog();
            LoadPets();
        }
        private void BtnEditPetCreator_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Virtual Pet Files (*.vpet)|*.vpet", Title = "Select a pet to edit"
            };
            if(openFileDialog.ShowDialog() == true)
            {
                try
                {
                    bool isSmartPet = false;
                    using (ZipArchive archive = ZipFile.OpenRead(openFileDialog.FileName))
                    {
                        ZipArchiveEntry? jsonEntry = archive.GetEntry("config.json");
                        if (jsonEntry != null)
                        {
                            using (StreamReader reader = new StreamReader(jsonEntry.Open()))
                            {
                                string jsonContent = reader.ReadToEnd();
                                using (JsonDocument doc = JsonDocument.Parse(jsonContent))
                                {
                                    if (doc.RootElement.TryGetProperty("IsSmartPet", out JsonElement isSmartElement))
                                    {
                                        if (isSmartElement.ValueKind == JsonValueKind.True) isSmartPet = true;
                                        else if (isSmartElement.ValueKind == JsonValueKind.String && isSmartElement.GetString()?.ToLower() == "true") isSmartPet = true;
                                        else if (isSmartElement.ValueKind == JsonValueKind.Number && isSmartElement.GetInt32() == 1) isSmartPet = true;
                                    }
                                }
                            }
                        }
                    }
                    if (!isSmartPet)
                    {
                        MessageBox.Show("This is a GIF Package, not a Smart Pet.", "Incompatible File", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }
                    PetCreatorWindow editorWindow = new PetCreatorWindow();
                    editorWindow.Owner = this;
                    editorWindow.LoadPetDataForEditing(openFileDialog.FileName);
                    editorWindow.ShowDialog();
                    LoadPets();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error reading file: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void LoadFavorites()
        {
            favoritesFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "VirtualPeto", "favorites.json");
            if (File.Exists(favoritesFilePath))
            {
                try
                {
                    string json = File.ReadAllText(favoritesFilePath);
                    favoritePaths = JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
                }catch{favoritePaths = new List<string>();}
            }
        }

        private void SaveFavorites()
        {
            try
            {
                string json = JsonSerializer.Serialize(favoritePaths);
                string directory = Path.GetDirectoryName(favoritesFilePath) ?? string.Empty;
                if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
                File.WriteAllText(favoritesFilePath, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error saving favorites: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void FavoriteToggle_Click(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleButton tb)
            {
                if (tb.DataContext is LibraryItem libItem)
                {
                    if (libItem.IsFavorite && !favoritePaths.Contains(libItem.FullPath))
                        favoritePaths.Add(libItem.FullPath);
                    else if (!libItem.IsFavorite)
                        favoritePaths.Remove(libItem.FullPath);
                }
                else if (tb.DataContext is PetItem petItem)
                {
                    if (petItem.IsFavorite && !favoritePaths.Contains(petItem.DirectoryPath))
                        favoritePaths.Add(petItem.DirectoryPath);
                    else if (!petItem.IsFavorite)
                        favoritePaths.Remove(petItem.DirectoryPath);
                }
                SaveFavorites();
            }
        }


        // === CREATE VPET ===

        private string NormalizePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return string.Empty;
            return Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        private void UpdateActivePetStatus()
        {
            if (TxtPetLimit == null) return;
            TxtPetLimit.Text = petLimit.ToString();
        }


        private void BtnCreateGifPackage_Click(object sender, RoutedEventArgs e)
        {
            GifPackageWindow packager = new GifPackageWindow();
            packager.Owner = this;
            packager.ShowDialog();
            LoadLibrary();
        }

        //Items Logic

        private void BtnOpenFoodCreator_Click(object sender, RoutedEventArgs e)
        {
            FoodCreatorWindow foodCreator = new FoodCreatorWindow();
            bool? result = foodCreator.ShowDialog();
        }

        private void BtnEditFoodCreator_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Virtual Pet Food Files (*.vfood)|*.vfood", 
                Title = "Select a food file to edit"
            };
            
            if(openFileDialog.ShowDialog() == true)
            {
                MessageBox.Show($"Editando el archivo de comida: {openFileDialog.FileName}", "En Construcción", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        // === EVENTOS DE OBJETOS ===

        private void BtnOpenObjectCreator_Click(object sender, RoutedEventArgs e)
        {
            VirtualPeto.Objects.ObjectCreatorWindow objectCreator = new VirtualPeto.Objects.ObjectCreatorWindow();
    
            bool? result = objectCreator.ShowDialog();
        }

        private void BtnEditObjectCreator_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Virtual Pet Object Files (*.vobj)|*.vobj", 
                Title = "Select an object file to edit"
            };
            
            if(openFileDialog.ShowDialog() == true)
            {
                // Lógica de edición de objetos
                MessageBox.Show($"Editando el archivo de objeto: {openFileDialog.FileName}", "En Construcción", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        // Tools logic

        private void BtnLaunchGifRemover_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = "Select GIF to Remove Background",
                Filter = "GIF Files|*.gif"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                GifBgRemoverWindow removerWindow = new GifBgRemoverWindow(openFileDialog.FileName, libraryPath);
                removerWindow.Owner = this;
                removerWindow.ShowDialog();
                LoadLibrary(); 
            }
        }
        private void BtnLaunchGifCreator_Click(object sender, RoutedEventArgs e)
        {
            VirtualPeto.Tools.GifCreatorWindow creatorWindow = new VirtualPeto.Tools.GifCreatorWindow();
            creatorWindow.Owner = this;
            creatorWindow.ShowDialog();
        }

        private void BtnLaunchBgRemover_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Images (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp",
                Title = "Select Image for Background Removal"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                VirtualPeto.Tools.BackgroundEditorWindow editorWindow = new VirtualPeto.Tools.BackgroundEditorWindow(openFileDialog.FileName, libraryPath);
                editorWindow.Owner = this;
                editorWindow.ShowDialog();
            }
        }
        private void BtnLaunchSpriteExtractor_Click(object sender, RoutedEventArgs e)
        {
            VirtualPeto.Tools.SpriteExtractorWindow spriteWindow = new VirtualPeto.Tools.SpriteExtractorWindow();
            spriteWindow.Owner = this;
            spriteWindow.ShowDialog();
        }

        private void BtnToolsGenerateGif_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Multiselect = true,
                Title = "Select images for GIF",
                Filter = "Image Files (*.png;*.jpg;*.jpeg;*.bmp;*.webp)|*.png;*.jpg;*.jpeg;*.bmp;*.webp"
            };

            if (openFileDialog.ShowDialog() != true) return;

            selectedToolsGifImages = openFileDialog.FileNames;
            if (selectedToolsGifImages.Length < 2)
            {
                MessageBox.Show("Please select at least 2 images.", "GIF Creator", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            VirtualPeto.Tools.GifCreatorWindow creatorWindow = new VirtualPeto.Tools.GifCreatorWindow(selectedToolsGifImages);
            creatorWindow.Owner = this;
            creatorWindow.ShowDialog();
        }

        private void BtnLaunchVideoBgRemover_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Video Files (*.mp4;*.mov;*.avi;*.mkv)|*.mp4;*.mov;*.avi;*.mkv",
                Title = "Select Video for Background Removal"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                VirtualPeto.Tools.VideoBgRemoverWindow removerWindow = new VirtualPeto.Tools.VideoBgRemoverWindow(openFileDialog.FileName, libraryPath);
                removerWindow.Owner = this;
                removerWindow.ShowDialog();
                LoadLibrary(); 
            }
        }
        private void BtnLaunchAudioConverter_Click(object sender, RoutedEventArgs e)
        {
            VirtualPeto.Tools.AudioConverterWindow audioWindow = new VirtualPeto.Tools.AudioConverterWindow();
            audioWindow.Owner = this;
            audioWindow.ShowDialog();
        }
        private void BtnLaunchFrameExtractor_Click(object sender, RoutedEventArgs e)
        {
            
            VirtualPeto.Tools.FrameExtractorWindow extractorWindow = new VirtualPeto.Tools.FrameExtractorWindow();
            extractorWindow.Owner = this;
            extractorWindow.ShowDialog();
            
        }

        // === SETTINGS ===
        private void ChkAutoClearCache_Checked(object sender, RoutedEventArgs e) => autoClearCache = true;
        private void ChkAutoClearCache_Unchecked(object sender, RoutedEventArgs e) => autoClearCache = false;
        private void BtnDecreasePetLimit_Click(object sender, RoutedEventArgs e) => TrySetPetLimit(petLimit - 1);
        private void BtnIncreasePetLimit_Click(object sender, RoutedEventArgs e) => TrySetPetLimit(petLimit + 1);
        private void ChkPlaySounds_Checked(object sender, RoutedEventArgs e)
        {
            SettingsManager.Current.AllowSounds = true;
            SettingsManager.Save();
        }

        private void ChkPlaySounds_Unchecked(object sender, RoutedEventArgs e)
        {
            SettingsManager.Current.AllowSounds = false;
            SettingsManager.Save();
        }

        private void ChkSecondMonitor_Checked(object sender, RoutedEventArgs e)
        {
            SettingsManager.Current.AllowSecondMonitor = true;
            SettingsManager.Save();
        }

        private void ChkSecondMonitor_Unchecked(object sender, RoutedEventArgs e)
        {
            SettingsManager.Current.AllowSecondMonitor = false;
            SettingsManager.Save();
        }

        private void BtnDecreaseSleep_Click(object sender, RoutedEventArgs e)
        {
            if (SettingsManager.Current.SleepTimeMinutes > 0)
            {
                SettingsManager.Current.SleepTimeMinutes--;
                TxtSleepTime.Text = SettingsManager.Current.SleepTimeMinutes.ToString();
                SettingsManager.Save();
            }
        }

        private void BtnIncreaseSleep_Click(object sender, RoutedEventArgs e)
        {
            SettingsManager.Current.SleepTimeMinutes++;
            TxtSleepTime.Text = SettingsManager.Current.SleepTimeMinutes.ToString();
            SettingsManager.Save();
        }

        private bool TrySetPetLimit(int limit)
        {
            int clampedLimit = Math.Max(MinPetLimit, Math.Min(MaxPetLimit, limit));
            if (clampedLimit < TotalActiveDesktopWindows)
            {
                MessageBox.Show($"You currently have {TotalActiveDesktopWindows} active desktop pets. Close one or more windows before lowering the limit to {clampedLimit}.", "Cannot lower limit", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            SetPetLimit(clampedLimit);
            return true;
        }

        private void SetPetLimit(int limit)
        {
            petLimit = Math.Max(MinPetLimit, Math.Min(MaxPetLimit, limit));
            TxtPetLimit.Text = petLimit.ToString();
        }

        private void ChkRunOnStartup_Checked(object sender, RoutedEventArgs e)
        {
            runOnStartup = true;
            UpdateRunOnStartup(true);
        }

        private void ChkRunOnStartup_Unchecked(object sender, RoutedEventArgs e)
        {
            runOnStartup = false;
            UpdateRunOnStartup(false);
        }

        private void UpdateRunOnStartup(bool enable)
        {
            try
            {
                const string registryKeyPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Run";
                const string appName = "VirtualPeto";
                using var runKey = Registry.CurrentUser.OpenSubKey(registryKeyPath, true) ?? Registry.CurrentUser.CreateSubKey(registryKeyPath);
                if (runKey == null) return;

                if (enable)
                {
                    string? exePath = GetExecutablePath();
                    if (!string.IsNullOrEmpty(exePath))
                    {
                        runKey.SetValue(appName, $"\"{exePath}\"");
                    }
                }
                else
                {
                    runKey.DeleteValue(appName, false);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not update startup setting: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void ChkStartFavorites_Checked(object sender, RoutedEventArgs e)
        {
            SettingsManager.Current.StartFavoritesOnStartup = true;
            SettingsManager.Save();
        }

        private void ChkStartFavorites_Unchecked(object sender, RoutedEventArgs e)
        {
            SettingsManager.Current.StartFavoritesOnStartup = false;
            SettingsManager.Save();
        }

        private void StartFavoritePetsAutomatically()
        {
            foreach(var item in fullLibraryList.Where(x => x.IsFavorite))
            {
                if(TotalActiveDesktopWindows >= petLimit) break;
                string libraryKey = NormalizePath(item.FullPath);
                if(activeLibraryWindows.ContainsKey(libraryKey)) continue;
                
                string soundPath = item.HasSound && !string.IsNullOrEmpty(item.SoundPath) ? item.SoundPath : string.Empty;
                MainWindow newWindow = new MainWindow(
                    mediaPath: item.FullPath,
                    isVideo: item.IsVideo,
                    size: 150,
                    soundPath: soundPath,
                    volume: item.Volume
                );
                
                newWindow.ShowInTaskbar = false;
                newWindow.Closed += (s, args) =>
                {
                    item.IsActive = false;
                    activeLibraryWindows.Remove(libraryKey);
                    ApplyLibraryFilters();
                    if(autoClearCache) ClearApplicationCache(includeActiveWindows: false);
                };
                
                activeLibraryWindows.Add(libraryKey, newWindow);
                item.IsActive = true;
                newWindow.Show();
            } 
            foreach (var item in fullPetsList.Where(x => x.IsFavorite))
            {
                if (TotalActiveDesktopWindows >= petLimit) break;
                
                string petKey = NormalizePath(item.DirectoryPath);
                if (activePetsWindows.ContainsKey(petKey)) continue;

                Window newWindow;
                if (item.SmartConfig != null && item.SmartConfig.IsSmartPet)
                {
                    newWindow = new SmartPetWindow(item.SmartConfig, item.DirectoryPath);
                }
                else
                {
                    string idleAnim = item.Config?.Animations?.Idle ?? string.Empty;
                    string idlePath = string.IsNullOrEmpty(idleAnim) ? string.Empty : Path.Combine(item.DirectoryPath, idleAnim);
                    double scale = item.Config?.Scale ?? 1.0;
                    newWindow = new MainWindow(idlePath, false, 150 * scale, string.Empty, 0.5);
                }

                newWindow.ShowInTaskbar = false;
                newWindow.Closed += (s, args) =>
                {
                    item.IsActive = false;
                    activePetsWindows.Remove(petKey);
                    UpdateActivePetStatus();
                    if (autoClearCache) ClearApplicationCache(includeActiveWindows: false);
                };

                activePetsWindows.Add(petKey, newWindow);
                item.IsActive = true;
                newWindow.Show();
            }
            
            ApplyLibraryFilters();
            UpdateActivePetStatus();
        }

        private string? GetExecutablePath()
        {
            return Assembly.GetEntryAssembly()?.Location ?? Process.GetCurrentProcess().MainModule?.FileName;
        }

        private void ClearApplicationCache(bool includeActiveWindows = true)
        {
            try
            {
                ImgLibraryPreview.Source = null;
                ImageBehavior.SetAnimatedSource(ImgLibraryPreview, null);
                ImgPetPreview.Source = null;
                ImageBehavior.SetAnimatedSource(ImgPetPreview, null);

                VidLibraryPreview.Stop();
                VidLibraryPreview.Source = null;

                if (includeActiveWindows)
                {
                    foreach (var window in activeLibraryWindows.Values.Concat(activePetsWindows.Values).ToList())
                    {
                        if (window is MainWindow mainWindow)
                        {
                            mainWindow.PetImage.Source = null;
                            ImageBehavior.SetAnimatedSource(mainWindow.PetImage, null);
                            mainWindow.PetVideo.Stop();
                            mainWindow.PetVideo.Source = null;
                        }
                        else if (window is SmartPetWindow smartWindow)
                        {
                            smartWindow.PetSprite.Source = null;
                        }
                    }
                }

                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error clearing cache: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnClearCache_Click(object sender, RoutedEventArgs e)
        {
            ClearApplicationCache();
            MessageBox.Show("Cache cleared.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ChkOverlapping_Checked(object sender, RoutedEventArgs e)
        {
            IsOverlappingEnabled = true;
            //UpdateAllPetsClickThrough(true);
        } 
        private void ChkOverlapping_Unchecked(object sender, RoutedEventArgs e)
        {
            IsOverlappingEnabled = false;
            //UpdateAllPetsClickThrough(false);
        }

        private void ChkLockPet_Checked(object sender, RoutedEventArgs e)
        {
            IsPetLocked = true;
            UpdateAllPetsClickThrough(true);
        }
        private void ChkLockPet_Unchecked(object sender, RoutedEventArgs e)
        {
            IsPetLocked = false;
            UpdateAllPetsClickThrough(false);
        }
        private void UpdateAllPetsClickThrough(bool isClickThroughValue)
        {
            foreach (Window window in Application.Current.Windows)
            {
                if (window is MainWindow petWindow)
                {
                    petWindow.SetClickThrough(isClickThroughValue);
                }
            }
        }
        private void CheckFullScreenApp(object? sender, EventArgs e)
        {
            IntPtr foreground = GetForegroundWindow();
            IntPtr desktop = GetDesktopWindow();
            IntPtr shell = GetShellWindow();

            bool isFullScreenAppActive = false;

            if (foreground != desktop && foreground != shell && foreground != IntPtr.Zero)
            {
                GetWindowRect(foreground, out RECT rect);
                int width = rect.right - rect.left;
                int height = rect.bottom - rect.top;

                if (width >= SystemParameters.PrimaryScreenWidth && height >= SystemParameters.PrimaryScreenHeight)
                {
                    isFullScreenAppActive = true;
                }
            }
            bool hidePets = isFullScreenAppActive && !IsOverlappingEnabled;

            foreach (Window window in Application.Current.Windows)
            {
                if (window is MainWindow petWindow)
                {
                    if (IsOverlappingEnabled && isFullScreenAppActive && petWindow.Visibility == Visibility.Visible)
                    {
                        petWindow.Topmost = false;
                        petWindow.Topmost = true;
                    }

                    petWindow.Visibility = hidePets ? Visibility.Hidden : Visibility.Visible;
                }
            }
        }
        private void BtnBrowseFolder_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                dialog.Description = "Select the default folder to save files";
                dialog.UseDescriptionForTitle = true;
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    TxtDefaultFolder.Text = dialog.SelectedPath;
                    SettingsManager.Current.DefaultSaveFolder = dialog.SelectedPath;
                    SettingsManager.Save();
                }
            }
        }

        //REDIRECTIONS
        private void BtnOpenItch_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://trueeuphoria.itch.io/virtualpeto", 
                UseShellExecute = true
            });
        }

        private void BtnOpenKofi_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://ko-fi.com/trueyahir", 
                UseShellExecute = true
            });
        }
        private void BtnOpenDiscord_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://discord.gg/8mWSueKqS", 
                UseShellExecute = true
            });
        }


    }
}