using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace VirtualPeto.Objects
{
    public enum PetObjectType
    {
        Food,
        Toy
    }

    public abstract class PetInteractableObject : PetWindowBase
    {
        private readonly DispatcherTimer _lifeTimer;

        public Guid ObjectId { get; } = Guid.NewGuid();
        public PetObjectType ObjectType { get; }
        public bool IsAvailable { get; private set; } = true;
        public bool IsCarried { get; private set; } = false;
        public SmartPetWindow? CarrierPet { get; private set; }
        public double DetectionRadius { get; protected set; } = 240;
        public double PickupRadius { get; protected set; } = 34;
        public TimeSpan LifeTime { get; protected set; } = TimeSpan.FromSeconds(30);

        protected PetInteractableObject(PetObjectType objectType, double size, Brush fill, Brush border)
        {
            ObjectType = objectType;
            Width = size;
            Height = size;

            Border shape = new Border
            {
                Width = size - 4,
                Height = size - 4,
                CornerRadius = new CornerRadius((size - 4) / 2.0),
                Background = fill,
                BorderBrush = border,
                BorderThickness = new Thickness(1),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            Grid container = new Grid();
            container.Children.Add(shape);
            Content = container;

            _lifeTimer = new DispatcherTimer { Interval = LifeTime };
            _lifeTimer.Tick += (s, e) =>
            {
                _lifeTimer.Stop();
                if (IsAvailable && !IsCarried)
                {
                    TryConsume();
                }
            };
            _lifeTimer.Start();
        }

        public Point GetCenter()
        {
            return new Point(Left + Width / 2.0, Top + Height / 2.0);
        }

        public double DistanceTo(Point point)
        {
            Point center = GetCenter();
            double dx = point.X - center.X;
            double dy = point.Y - center.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        public bool CanBeInteracted()
        {
            return IsAvailable;
        }

        public void AttachToPet(SmartPetWindow pet)
        {
            if (!IsAvailable) return;
            CarrierPet = pet;
            IsCarried = true;
        }

        public void UpdateCarriedPosition(Point anchorPoint)
        {
            if (!IsAvailable || !IsCarried) return;
            Left = anchorPoint.X - (Width / 2.0);
            Top = anchorPoint.Y - (Height / 2.0);
        }

        public void DropAt(Point centerPoint)
        {
            if (!IsAvailable) return;
            IsCarried = false;
            CarrierPet = null;
            Left = centerPoint.X - (Width / 2.0);
            Top = centerPoint.Y - (Height / 2.0);
        }

        public void TryConsume()
        {
            if (!IsAvailable) return;
            IsAvailable = false;
            IsCarried = false;
            CarrierPet = null;
            Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            _lifeTimer.Stop();
            PetObjectRegistry.Unregister(this);
            base.OnClosed(e);
        }
    }
}
