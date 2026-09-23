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
        private static JukeboxObject? _activeJukebox;

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

        public static FoodObject SpawnFood(Rect allowedArea, Random random, Point nearPoint, AnimationData? anim = null, string petDir = "")
        {
            FoodObject food = new FoodObject();
            if (anim != null) food.SetAnimation(anim, petDir);
            PlaceObject(food, allowedArea, random, nearPoint);
            Register(food);
            food.Show();
            return food;
        }

        public static ToyObject SpawnToy(Rect allowedArea, Random random, Point nearPoint, AnimationData? anim = null, string petDir = "")
        {
            ToyObject toy = new ToyObject();
            if (anim != null) toy.SetAnimation(anim, petDir);
            PlaceObject(toy, allowedArea, random, nearPoint);
            Register(toy);
            toy.Show();
            return toy;
        }

        public static JukeboxObject SpawnJukebox(Rect allowedArea, Random random, Point nearPoint)
        {
            if (_activeJukebox != null && _activeJukebox.IsLoaded)
            {
                _activeJukebox.Activate();
                return _activeJukebox;
            }

            _activeJukebox = new JukeboxObject();
            PlaceObject(_activeJukebox, allowedArea, random, nearPoint);
            _activeJukebox.Closed += (_, __) => _activeJukebox = null;
            _activeJukebox.Show();
            return _activeJukebox;
        }

        private static void PlaceObject(Window obj, Rect allowedArea, Random random, Point nearPoint)
        {
            double targetX = allowedArea.Left + random.NextDouble() * (allowedArea.Width - obj.Width);
            double targetY = allowedArea.Top + random.NextDouble() * (allowedArea.Height - obj.Height);

            obj.Left = targetX;
            obj.Top = targetY;
        }
    }
}
