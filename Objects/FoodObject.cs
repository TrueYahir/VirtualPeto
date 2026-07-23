using System;
using System.Windows.Media;

namespace VirtualPeto.Objects
{
    public class FoodObject : PetInteractableObject
    {
        public TimeSpan ConsumeDuration { get; } = TimeSpan.FromSeconds(1.8);

        public FoodObject() : base(
            objectType: PetObjectType.Food,
            size: 24,
            fill: new SolidColorBrush(Color.FromRgb(210, 170, 95)),
            border: new SolidColorBrush(Color.FromRgb(110, 80, 40)))
        {
            DetectionRadius = 260;
            PickupRadius = 34;
            LifeTime = TimeSpan.FromSeconds(45);
        }
    }
}
