using System;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace VirtualPeto
{
    public partial class PetCreatorWindow : Window
    {
        public ObservableCollection<RandomAction> RandomActionsList { get; set; }
        private string _currentEditingFilePath = string.Empty;
        
        private PetMetadata metadata = new PetMetadata();

        public PetCreatorWindow()
        {
            InitializeComponent();
            
            RandomActionsList = new ObservableCollection<RandomAction>();
            LvwRandomActions.ItemsSource = RandomActionsList;
        }

        private void CmbFormat_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PanelSpriteSettings == null) return; 

            string selected = (CmbFormat.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";
            
            PanelResolution.Visibility = Visibility.Visible;
            PanelSpriteSettings.Visibility = Visibility.Collapsed;
            PanelFpsSettings.Visibility = Visibility.Collapsed;

            if (selected == "Sprite Sheet")
            {
                PanelSpriteSettings.Visibility = Visibility.Visible;
                PanelFpsSettings.Visibility = Visibility.Visible;
            }
            else if (selected == "GIF")
            {
                PanelFpsSettings.Visibility = Visibility.Visible;
            }
        }

        public void LoadPetDataForEditing(string filePath)
        {
            _currentEditingFilePath = filePath;
            
            try
            {
                using (ZipArchive archive = ZipFile.OpenRead(filePath))
                {
                    ZipArchiveEntry? jsonEntry = archive.GetEntry("config.json");
                    if (jsonEntry != null)
                    {
                        using (StreamReader reader = new StreamReader(jsonEntry.Open()))
                        {
                            string jsonContent = reader.ReadToEnd();
                            var options = new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true,
                                //NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString
                            };
                            options.Converters.Add(new SafeIntConverter());
                            options.Converters.Add(new SafeBoolConverter());
                            options.Converters.Add(new SafeDoubleConverter());

                            PetMetadata? loadedMetadata = JsonSerializer.Deserialize<PetMetadata>(jsonContent, options);
                            
                            if (loadedMetadata != null)
                            {
                                metadata = loadedMetadata; 
                                
                                if (metadata.Movements == null) metadata.Movements = new System.Collections.Generic.Dictionary<string, AnimationData>();
                                if (metadata.RandomActions == null) metadata.RandomActions = new System.Collections.Generic.List<RandomAction>();
                                if (metadata.IdleAnimation == null) metadata.IdleAnimation = new AnimationData();
                                if (metadata.SleepAnimation == null) metadata.SleepAnimation = new AnimationData();
                                if (metadata.IntroAnimation == null) metadata.IntroAnimation = new AnimationData();
                                if (metadata.OutroAnimation == null) metadata.OutroAnimation = new AnimationData();
                                if (metadata.ClickedAnimation == null) metadata.ClickedAnimation = new AnimationData();
                                if (metadata.DraggedAnimation == null) metadata.DraggedAnimation = new AnimationData();
                                if (metadata.WakeUpAnimation == null) metadata.WakeUpAnimation = new AnimationData();
                                if (metadata.ListeningAnimation == null) metadata.ListeningAnimation = new AnimationData();
                                if (metadata.NotificationAnimation == null) metadata.NotificationAnimation = new AnimationData();

                                string[] llavesMovimientos = { 
                                    "Walk_Up", "Walk_Down", "Walk_Left", "Walk_Right", "Walk_UpLeft", "Walk_UpRight", "Walk_DownLeft", "Walk_DownRight", 
                                    "Run_Up", "Run_Down", "Run_Left", "Run_Right", "Run_UpLeft", "Run_UpRight", "Run_DownLeft", "Run_DownRight" 
                                };
                                
                                foreach (string llave in llavesMovimientos)
                                {
                                    if (!metadata.Movements.ContainsKey(llave))
                                    {
                                        metadata.Movements[llave] = new AnimationData { FilePath = "" };
                                    }
                                }
                                
                                TxtPetName.Text = metadata.PetName;
                                TxtAuthor.Text = metadata.Author;
                                
                                TxtIdlePath.Text = metadata.IdleAnimation.FilePath;
                                TxtSleepPath.Text = metadata.SleepAnimation.FilePath;

                                if (metadata.Movements.ContainsKey("Walk_Up")) TxtWalkUp.Text = metadata.Movements["Walk_Up"].FilePath;
                                if (metadata.Movements.ContainsKey("Walk_Down")) TxtWalkDown.Text = metadata.Movements["Walk_Down"].FilePath;
                                if (metadata.Movements.ContainsKey("Walk_Left")) TxtWalkLeft.Text = metadata.Movements["Walk_Left"].FilePath;
                                if (metadata.Movements.ContainsKey("Walk_Right")) TxtWalkRight.Text = metadata.Movements["Walk_Right"].FilePath;
                                if (metadata.Movements.ContainsKey("Walk_UpLeft")) TxtWalkUpLeft.Text = metadata.Movements["Walk_UpLeft"].FilePath;
                                if (metadata.Movements.ContainsKey("Walk_UpRight")) TxtWalkUpRight.Text = metadata.Movements["Walk_UpRight"].FilePath;
                                if (metadata.Movements.ContainsKey("Walk_DownLeft")) TxtWalkDownLeft.Text = metadata.Movements["Walk_DownLeft"].FilePath;
                                if (metadata.Movements.ContainsKey("Walk_DownRight")) TxtWalkDownRight.Text = metadata.Movements["Walk_DownRight"].FilePath;

                                if (metadata.Movements.ContainsKey("Run_Up")) TxtRunUp.Text = metadata.Movements["Run_Up"].FilePath;
                                if (metadata.Movements.ContainsKey("Run_Down")) TxtRunDown.Text = metadata.Movements["Run_Down"].FilePath;
                                if (metadata.Movements.ContainsKey("Run_Left")) TxtRunLeft.Text = metadata.Movements["Run_Left"].FilePath;
                                if (metadata.Movements.ContainsKey("Run_Right")) TxtRunRight.Text = metadata.Movements["Run_Right"].FilePath;
                                if (metadata.Movements.ContainsKey("Run_UpLeft")) TxtRunUpLeft.Text = metadata.Movements["Run_UpLeft"].FilePath;
                                if (metadata.Movements.ContainsKey("Run_UpRight")) TxtRunUpRight.Text = metadata.Movements["Run_UpRight"].FilePath;
                                if (metadata.Movements.ContainsKey("Run_DownLeft")) TxtRunDownLeft.Text = metadata.Movements["Run_DownLeft"].FilePath;
                                if (metadata.Movements.ContainsKey("Run_DownRight")) TxtRunDownRight.Text = metadata.Movements["Run_DownRight"].FilePath;

                                TxtIntroPath.Text = metadata.IntroAnimation.FilePath;
                                TxtOutroPath.Text = metadata.OutroAnimation.FilePath;
                                TxtClickedPath.Text = metadata.ClickedAnimation.FilePath;
                                TxtDraggedPath.Text = metadata.DraggedAnimation.FilePath;
                                TxtListeningPath.Text = metadata.ListeningAnimation.FilePath;
                                TxtNotificationPath.Text = metadata.NotificationAnimation.FilePath;
                                TxtWakeUpPath.Text = metadata.WakeUpAnimation.FilePath;

                                TxtFrameWidth.Text = metadata.IdleAnimation.FrameWidth.ToString();
                                TxtFrameHeight.Text = metadata.IdleAnimation.FrameHeight.ToString();
                                TxtFps.Text = metadata.IdleAnimation.Fps.ToString();

                                if (metadata.IdleAnimation.IsSpriteSheet)
                                {
                                    CmbFormat.SelectedIndex = 0; 
                                    TxtColumns.Text = metadata.IdleAnimation.Columns.ToString();
                                    TxtRows.Text = metadata.IdleAnimation.Rows.ToString();
                                    TxtTotalFrames.Text = metadata.IdleAnimation.TotalFrames.ToString();
                                }
                                else
                                {
                                    string ext = Path.GetExtension(metadata.IdleAnimation.FilePath).ToLower();
                                    if (ext == ".gif") CmbFormat.SelectedIndex = 1;
                                    else if (ext == ".mp4") CmbFormat.SelectedIndex = 2;
                                    else CmbFormat.SelectedIndex = 3;
                                }

                                RandomActionsList.Clear();
                                foreach(var action in metadata.RandomActions)
                                {
                                    RandomActionsList.Add(action);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading .vpet file: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnBrowse_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string tag)
            {
                OpenFileDialog ofd = new OpenFileDialog
                {
                    Title = $"Select animation for: {tag}",
                    Filter = "Media Files (*.gif;*.png;*.mp4)|*.gif;*.png;*.mp4|All files (*.*)|*.*"
                };

                if (ofd.ShowDialog() == true)
                {
                    if (tag.StartsWith("Walk_") || tag.StartsWith("Run_"))
                    {
                        if (!metadata.Movements.ContainsKey(tag))
                        {
                            metadata.Movements[tag] = new AnimationData();
                        }
                    }

                    switch (tag)
                    {
                        case "Idle": TxtIdlePath.Text = ofd.FileName; AutoCalcularParaAnimacion(metadata.IdleAnimation, ofd.FileName, true); break;
                        case "Sleep": TxtSleepPath.Text = ofd.FileName; AutoCalcularParaAnimacion(metadata.SleepAnimation, ofd.FileName, false); break;
                        case "Intro": TxtIntroPath.Text = ofd.FileName; AutoCalcularParaAnimacion(metadata.IntroAnimation, ofd.FileName, false); break;
                        case "Outro": TxtOutroPath.Text = ofd.FileName; AutoCalcularParaAnimacion(metadata.OutroAnimation, ofd.FileName, false); break;
                        case "WakeUp": TxtWakeUpPath.Text = ofd.FileName; AutoCalcularParaAnimacion(metadata.WakeUpAnimation, ofd.FileName, false); break;
                        case "Clicked": TxtClickedPath.Text = ofd.FileName; AutoCalcularParaAnimacion(metadata.ClickedAnimation, ofd.FileName, false); break;
                        case "Dragged": TxtDraggedPath.Text = ofd.FileName; AutoCalcularParaAnimacion(metadata.DraggedAnimation, ofd.FileName, false); break;
                        case "Listening": TxtListeningPath.Text = ofd.FileName; AutoCalcularParaAnimacion(metadata.ListeningAnimation, ofd.FileName, false); break;
                        case "Notification": TxtNotificationPath.Text = ofd.FileName; AutoCalcularParaAnimacion(metadata.NotificationAnimation, ofd.FileName, false); break;
                        

                        case "Walk_Up": TxtWalkUp.Text = ofd.FileName; AutoCalcularParaAnimacion(metadata.Movements["Walk_Up"], ofd.FileName, false); break;
                        case "Walk_Down": TxtWalkDown.Text = ofd.FileName; AutoCalcularParaAnimacion(metadata.Movements["Walk_Down"], ofd.FileName, false); break;
                        case "Walk_Left": TxtWalkLeft.Text = ofd.FileName; AutoCalcularParaAnimacion(metadata.Movements["Walk_Left"], ofd.FileName, false); break;
                        case "Walk_Right": TxtWalkRight.Text = ofd.FileName; AutoCalcularParaAnimacion(metadata.Movements["Walk_Right"], ofd.FileName, false); break;
                        case "Walk_UpLeft": TxtWalkUpLeft.Text = ofd.FileName; AutoCalcularParaAnimacion(metadata.Movements["Walk_UpLeft"], ofd.FileName, false); break;
                        case "Walk_UpRight": TxtWalkUpRight.Text = ofd.FileName; AutoCalcularParaAnimacion(metadata.Movements["Walk_UpRight"], ofd.FileName, false); break;
                        case "Walk_DownLeft": TxtWalkDownLeft.Text = ofd.FileName; AutoCalcularParaAnimacion(metadata.Movements["Walk_DownLeft"], ofd.FileName, false); break;
                        case "Walk_DownRight": TxtWalkDownRight.Text = ofd.FileName; AutoCalcularParaAnimacion(metadata.Movements["Walk_DownRight"], ofd.FileName, false); break;
                        
                        case "Run_Up": TxtRunUp.Text = ofd.FileName; AutoCalcularParaAnimacion(metadata.Movements["Run_Up"], ofd.FileName, false); break;
                        case "Run_Down": TxtRunDown.Text = ofd.FileName; AutoCalcularParaAnimacion(metadata.Movements["Run_Down"], ofd.FileName, false); break;
                        case "Run_Left": TxtRunLeft.Text = ofd.FileName; AutoCalcularParaAnimacion(metadata.Movements["Run_Left"], ofd.FileName, false); break;
                        case "Run_Right": TxtRunRight.Text = ofd.FileName; AutoCalcularParaAnimacion(metadata.Movements["Run_Right"], ofd.FileName, false); break;
                        case "Run_UpLeft": TxtRunUpLeft.Text = ofd.FileName; AutoCalcularParaAnimacion(metadata.Movements["Run_UpLeft"], ofd.FileName, false); break;
                        case "Run_UpRight": TxtRunUpRight.Text = ofd.FileName; AutoCalcularParaAnimacion(metadata.Movements["Run_UpRight"], ofd.FileName, false); break;
                        case "Run_DownLeft": TxtRunDownLeft.Text = ofd.FileName; AutoCalcularParaAnimacion(metadata.Movements["Run_DownLeft"], ofd.FileName, false); break;
                        case "Run_DownRight": TxtRunDownRight.Text = ofd.FileName; AutoCalcularParaAnimacion(metadata.Movements["Run_DownRight"], ofd.FileName, false); break;
                    }
                }
            }
        }

        private void AutoCalcularParaAnimacion(AnimationData anim, string filePath, bool esPrincipal)
        {
            anim.FilePath = filePath;
            
            string formatoSeleccionado = (CmbFormat.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";
            if (formatoSeleccionado != "Sprite Sheet") return;

            int.TryParse(TxtFrameWidth.Text, out int baseW);
            int.TryParse(TxtFrameHeight.Text, out int baseH);
            if (baseW <= 0) baseW = 64;
            if (baseH <= 0) baseH = 64;

            try
            {
                BitmapImage img = new BitmapImage();
                img.BeginInit();
                img.CacheOption = BitmapCacheOption.OnLoad;
                img.UriSource = new Uri(filePath);
                img.EndInit();

                int exactWidth = img.PixelWidth;
                int exactHeight = img.PixelHeight;

                int cols = exactWidth / baseW;
                int rows = exactHeight / baseH;
                if (cols <= 0) cols = 1;
                if (rows <= 0) rows = 1;
                int totalFrames = cols * rows;

                anim.IsSpriteSheet = true;
                anim.FrameWidth = baseW;
                anim.FrameHeight = baseH;
                anim.Columns = cols;
                anim.Rows = rows;
                anim.TotalFrames = totalFrames;
                int.TryParse(TxtFps.Text, out int fps);
                anim.Fps = fps > 0 ? fps : 10;

                if (esPrincipal)
                {
                    TxtColumns.Text = cols.ToString();
                    TxtRows.Text = rows.ToString();
                    TxtTotalFrames.Text = totalFrames.ToString();
                }
            }
            catch { }
        }

        private void BtnAddRandomAction_Click(object sender, RoutedEventArgs e)
        {
            RandomActionsList.Add(new RandomAction 
            { 
                ActionName = "New Action", 
                Probability = 0.1, 
                Animation = new AnimationData() 
            });
        }

        private void BtnBrowseRandomAction_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.DataContext is RandomAction action)
            {
                OpenFileDialog ofd = new OpenFileDialog
                {
                    Title = "Select animation for random action",
                    Filter = "Media Files (*.gif;*.png;*.mp4)|*.gif;*.png;*.mp4|All files (*.*)|*.*"
                };

                if (ofd.ShowDialog() == true)
                {
                    AutoCalcularParaAnimacion(action.Animation, ofd.FileName, false);
                    LvwRandomActions.Items.Refresh();
                }
            }
        }

        private void BtnCompile_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtPetName.Text))
            {
                MessageBox.Show("You must provide a name for your pet.", "Missing Name", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(TxtIdlePath.Text) && string.IsNullOrEmpty(_currentEditingFilePath))
            {
                MessageBox.Show("The Idle animation is required.", "Missing Base Animation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            metadata.PetName = TxtPetName.Text.Trim();
            metadata.Author = TxtAuthor.Text.Trim();
            metadata.IsSmartPet = true;

            metadata.IdleAnimation.FilePath = TxtIdlePath.Text;
            metadata.SleepAnimation.FilePath = TxtSleepPath.Text;
            metadata.WakeUpAnimation.FilePath = TxtWakeUpPath.Text;
            
            metadata.Movements["Walk_DownLeft"].FilePath = TxtWalkDownLeft.Text;
            metadata.Movements["Walk_DownRight"].FilePath = TxtWalkDownRight.Text;
            metadata.Movements["Walk_UpLeft"].FilePath = TxtWalkUpLeft.Text;
            metadata.Movements["Walk_UpRight"].FilePath = TxtWalkUpRight.Text;
            metadata.Movements["Run_Down"].FilePath = TxtRunDown.Text;
            metadata.Movements["Run_Left"].FilePath = TxtRunLeft.Text;
            metadata.Movements["Run_Right"].FilePath = TxtRunRight.Text;
            metadata.Movements["Run_Up"].FilePath = TxtRunUp.Text;

            metadata.IntroAnimation.FilePath = TxtIntroPath.Text;
            metadata.OutroAnimation.FilePath = TxtOutroPath.Text;
            metadata.ClickedAnimation.FilePath = TxtClickedPath.Text;
            metadata.DraggedAnimation.FilePath = TxtDraggedPath.Text;
            metadata.ListeningAnimation.FilePath = TxtListeningPath.Text;
            metadata.NotificationAnimation.FilePath = TxtNotificationPath.Text;

            int.TryParse(TxtFrameWidth.Text, out int gw);
            int.TryParse(TxtFrameHeight.Text, out int gh);
            int.TryParse(TxtFps.Text, out int gFps);

            ActualizarValoresDefault(metadata.IdleAnimation, gw, gh, gFps, true);
            ActualizarValoresDefault(metadata.SleepAnimation, gw, gh, gFps, false);
            ActualizarValoresDefault(metadata.WakeUpAnimation, gw, gh, gFps, false);

            ActualizarValoresDefault(metadata.IntroAnimation, gw, gh, gFps, false);
            ActualizarValoresDefault(metadata.OutroAnimation, gw, gh, gFps, false);
            ActualizarValoresDefault(metadata.ClickedAnimation, gw, gh, gFps, false);
            ActualizarValoresDefault(metadata.DraggedAnimation, gw, gh, gFps, false);
            ActualizarValoresDefault(metadata.ListeningAnimation, gw, gh, gFps, false);
            ActualizarValoresDefault(metadata.NotificationAnimation, gw, gh, gFps, false);

            foreach (var kvp in metadata.Movements)
            {
                ActualizarValoresDefault(kvp.Value, gw, gh, gFps, false);
            }

            metadata.RandomActions.Clear();
            foreach (var action in RandomActionsList)
            {
                if (!string.IsNullOrWhiteSpace(action.ActionName) && !string.IsNullOrWhiteSpace(action.Animation.FilePath))
                {
                    ActualizarValoresDefault(action.Animation, gw, gh, gFps, false);
                    metadata.RandomActions.Add(action);
                }
            }

            SaveFileDialog sfd = new SaveFileDialog
            {
                Title = "Save Smart Pet",
                Filter = "Virtual Pet File (*.vpet)|*.vpet",
                FileName = $"{metadata.PetName}.vpet"
            };

            if (sfd.ShowDialog() == true)
            {
                try
                {
                    PetPacker.CreatePetPackage(metadata, sfd.FileName, _currentEditingFilePath);
                    InstallCompiledPetToDocuments(sfd.FileName, metadata.PetName);
                    MessageBox.Show($"The smart pet '{metadata.PetName}' was compiled successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    this.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"An error occurred while compiling the pet:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void InstallCompiledPetToDocuments(string packagePath, string petName)
        {
            string baseDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "VirtualPeto");
            string petsPath = Path.Combine(baseDataPath, "Pets");
            Directory.CreateDirectory(petsPath);

            string safePetName = string.Concat((petName ?? string.Empty).Select(ch => Path.GetInvalidFileNameChars().Contains(ch) ? '_' : ch)).Trim();
            if (string.IsNullOrWhiteSpace(safePetName)) safePetName = Path.GetFileNameWithoutExtension(packagePath);

            string targetPath = Path.Combine(petsPath, safePetName);
            if (Directory.Exists(targetPath)) Directory.Delete(targetPath, true);
            ZipFile.ExtractToDirectory(packagePath, targetPath);
        }

        private void ActualizarValoresDefault(AnimationData anim, int gw, int gh, int gFps, bool esIdle)
        {
            if (string.IsNullOrWhiteSpace(anim.FilePath)) return;

            if (anim.FrameWidth == 0) anim.FrameWidth = gw > 0 ? gw : 64;
            if (anim.FrameHeight == 0) anim.FrameHeight = gh > 0 ? gh : 64;
            if (anim.Fps == 0) anim.Fps = gFps > 0 ? gFps : 10;

            string selected = (CmbFormat.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";
            if (selected == "Sprite Sheet")
            {
                if (esIdle)
                {
                    anim.IsSpriteSheet = true;
                    int.TryParse(TxtColumns.Text, out int c);
                    int.TryParse(TxtRows.Text, out int r);
                    int.TryParse(TxtTotalFrames.Text, out int tf);
                    anim.Columns = c > 0 ? c : 1;
                    anim.Rows = r > 0 ? r : 1;
                    anim.TotalFrames = tf > 0 ? tf : 1;
                }
                else if (anim.TotalFrames == 0 || anim.Columns == 0)
                {
                    anim.IsSpriteSheet = true;
                    anim.Columns = 1;
                    anim.Rows = 1;
                    anim.TotalFrames = 1;
                }
            }
            else
            {
                anim.IsSpriteSheet = false;
                anim.Columns = 1;
                anim.Rows = 1;
                anim.TotalFrames = 1;
            }
        }

        private void BtnAnimSettings_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string key)
            {
                AnimationData targetAnim;

                if (key == "Idle") targetAnim = metadata.IdleAnimation;
                else if (key == "Sleep") targetAnim = metadata.SleepAnimation;
                else if (key == "Intro") targetAnim = metadata.IntroAnimation;
                else if (key == "Outro") targetAnim = metadata.OutroAnimation;
                else if (key == "WakeUp") targetAnim = metadata.WakeUpAnimation;
                else if (key == "Clicked") targetAnim = metadata.ClickedAnimation;
                else if (key == "Dragged") targetAnim = metadata.DraggedAnimation;
                else if (key == "Listening") targetAnim = metadata.ListeningAnimation;
                else if (key == "Notification") targetAnim = metadata.NotificationAnimation;
                else if (metadata.Movements.ContainsKey(key)) targetAnim = metadata.Movements[key];
                else return;

                if (string.IsNullOrWhiteSpace(targetAnim.FilePath))
                {
                    MessageBox.Show("Please browse and select a file first before configuring custom parameters.", "No File Selected", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                AnimationSettingsWindow settingsWin = new AnimationSettingsWindow(targetAnim, System.IO.Path.GetDirectoryName(_currentEditingFilePath) ?? "");
                settingsWin.Owner = this;
                settingsWin.ShowDialog();
            }
        }
    }
    public class SafeIntConverter : JsonConverter<int>
    {
        public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        { 
            if(reader.TokenType == JsonTokenType.Number)
            {
                return reader.GetInt32();
            }
            if(reader.TokenType == JsonTokenType.String)
            {
                string? value = reader.GetString();
                if(int.TryParse(value, out int result)) return result;
                return 0;
            }
            return 0;
        }
        public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
        {
            writer.WriteNumberValue(value);
        }
    }

    public class SafeBoolConverter : JsonConverter<bool>
    {
        public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.True) return true;
            if (reader.TokenType == JsonTokenType.False) return false;
            
            if (reader.TokenType == JsonTokenType.String)
            {
                string? value = reader.GetString();
                if (bool.TryParse(value, out bool result)) return result;
                if (value == "1") return true;
                return false; 
            }
            return false;
        }

        public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options)
        {
            writer.WriteBooleanValue(value);
        }
    }
    public class SafeDoubleConverter : JsonConverter<double>
    {
        public override double Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Number)
            {
                return reader.GetDouble();
            }
            if (reader.TokenType == JsonTokenType.String)
            {
                string? value = reader.GetString();
                if (value != null)
                {
                    value = value.Replace(",", ".");
                    if (double.TryParse(value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double result))
                    {
                        return result;
                    }
                }
                return 0.0; 
            }
            return 0.0;
        }

        public override void Write(Utf8JsonWriter writer, double value, JsonSerializerOptions options)
        {
            writer.WriteNumberValue(value);
        }
    }
}