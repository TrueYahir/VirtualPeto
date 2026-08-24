using System.Collections.Generic;

namespace VirtualPeto
{
    public class AnimationData
    {
        public string FilePath { get; set; } = string.Empty;
        public string SoundPath { get; set; } = string.Empty;
        public bool IsSpriteSheet { get; set; } = false;
        public int Columns { get; set; } = 1;
        public int Rows { get; set; } = 1;
        public int TotalFrames { get; set; } = 1;
        public int Fps { get; set; } = 10;
        
        public int FrameWidth { get; set; }
        public int FrameHeight { get; set; }
    }

    public class PetMetadata
    {
        public string PetName { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public bool IsSmartPet {get; set;} = false;
        
        public AnimationData IdleAnimation { get; set; } = new AnimationData();
        public AnimationData SleepAnimation { get; set; } = new AnimationData();
        public AnimationData ClickedAnimation { get; set; } = new AnimationData();
        public AnimationData DraggedAnimation { get; set; } = new AnimationData();
        public AnimationData IntroAnimation { get; set; } = new AnimationData();
        public AnimationData OutroAnimation { get; set; } = new AnimationData();
        public AnimationData WakeUpAnimation {get; set;} = new AnimationData();
        public AnimationData ListeningAnimation {get; set;} = new AnimationData();
        public AnimationData NotificationAnimation{get; set;} = new AnimationData();
        public AnimationData MusicAnimation { get; set; } = new AnimationData();

        public AnimationData FoodAnimation { get; set; } = new AnimationData();
        public AnimationData FoodGrabbedAnimation { get; set; } = new AnimationData();
        public AnimationData EatingFoodAnimation { get; set; } = new AnimationData();

        public AnimationData ItemAnimation { get; set; } = new AnimationData();
        public AnimationData ItemGrabbedAnimation { get; set; } = new AnimationData();
        public AnimationData UsingItemAnimation { get; set; } = new AnimationData();

        public Dictionary<string, AnimationData> Movements { get; set; } = new Dictionary<string, AnimationData>()
        {
            { "Walk_Up", new AnimationData() },
            { "Walk_Down", new AnimationData() },
            { "Walk_Left", new AnimationData() },
            { "Walk_Right", new AnimationData() },
            { "Walk_UpLeft", new AnimationData() },
            { "Walk_UpRight", new AnimationData() },
            { "Walk_DownLeft", new AnimationData() },
            { "Walk_DownRight", new AnimationData() },

            { "Run_Up", new AnimationData() },
            { "Run_Down", new AnimationData() },
            { "Run_Left", new AnimationData() },
            { "Run_Right", new AnimationData() },
            { "Run_UpLeft", new AnimationData() },
            { "Run_UpRight", new AnimationData() },
            { "Run_DownLeft", new AnimationData() },
            { "Run_DownRight", new AnimationData() }
        };

        public List<RandomAction> RandomActions { get; set; } = new List<RandomAction>();
    }

    public class RandomAction
    {
        public string ActionName { get; set; } = string.Empty;
        public AnimationData Animation { get; set; } = new AnimationData();
        public double Probability { get; set; } = 0.1;
    }
}