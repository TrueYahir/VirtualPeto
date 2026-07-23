using System;
using System.Windows.Media;

namespace VirtualPeto.Objects
{
    public class ToyObject : PetInteractableObject
    {
        public TimeSpan PlayDuration { get; } = TimeSpan.FromSeconds(2.2);

        public ToyObject() : base(
            objectType: PetObjectType.Toy,
            size: 24,
            fill: new SolidColorBrush(Color.FromRgb(95, 150, 220)),
            border: new SolidColorBrush(Color.FromRgb(50, 80, 120)))
        {
            DetectionRadius = 240;
            PickupRadius = 34;
            LifeTime = TimeSpan.FromSeconds(60);
        }
    }
}
