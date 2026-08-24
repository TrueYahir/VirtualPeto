using System.Text.Json.Serialization;

namespace VirtualPeto
{
    public class AppSettings
    {
        public bool RunOnStartup { get; set; } = false;
        public bool AutoClearCache { get; set; } = true;
        public bool AllowOverlay { get; set; } = true;
        public bool LockPetPosition { get; set; } = false;
        public bool AllowSounds { get; set; } = true;
        public bool AllowSecondMonitor { get; set; } = false;
        public int DesktopPetLimit { get; set; } = 5;
        public int SleepTimeMinutes { get; set; } = 15;
        public bool StartFavoritesOnStartup { get; set; } = false;
        public string DefaultSaveFolder{get; set;} = string.Empty;
        public string JukeboxMusicFolder { get; set; } = string.Empty;
    }
}