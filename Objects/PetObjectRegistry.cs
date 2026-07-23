using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace VirtualPeto.Objects
{
    public static class PetObjectRegistry
    {
        private static readonly List<PetInteractableObject> _objects = new List<PetInteractableObject>();
        private static readonly object _sync = new object();

        public static void Register(PetInteractableObject obj)
        {
            lock (_sync)
            {
                if (_objects.Contains(obj)) return;
                _objects.Add(obj);
            }
        }

        public static void Unregister(PetInteractableObject obj)
        {
            lock (_sync)
            {
                _objects.Remove(obj);
            }
        }

        public static PetInteractableObject? FindNearestAvailable(Point from, double maxDistance)
        {
            lock (_sync)
            {
                return _objects
                    .Where(o => o.CanBeInteracted())
                    .Select(o => new { Object = o, Distance = o.DistanceTo(from) })
                    .Where(x => x.Distance <= maxDistance)
                    .OrderBy(x => x.Distance)
                    .Select(x => x.Object)
                    .FirstOrDefault();
            }
        }

        public static FoodObject SpawnFood(Rect allowedArea, Random random, Point nearPoint)
        {
            FoodObject food = new FoodObject();
            PlaceObject(food, allowedArea, random, nearPoint);
            Register(food);
            food.Show();
            return food;
        }

        public static ToyObject SpawnToy(Rect allowedArea, Random random, Point nearPoint)
        {
            ToyObject toy = new ToyObject();
            PlaceObject(toy, allowedArea, random, nearPoint);
            Register(toy);
            toy.Show();
            return toy;
        }

        private static void PlaceObject(PetInteractableObject obj, Rect allowedArea, Random random, Point nearPoint)
        {
            double xOffset = random.NextDouble() * 140 - 70;
            double yOffset = random.NextDouble() * 90 + 24;
            double targetX = nearPoint.X + xOffset;
            double targetY = nearPoint.Y + yOffset;

            obj.Left = Math.Max(allowedArea.Left, Math.Min(allowedArea.Right - obj.Width, targetX));
            obj.Top = Math.Max(allowedArea.Top, Math.Min(allowedArea.Bottom - obj.Height, targetY));
        }
    }
}
