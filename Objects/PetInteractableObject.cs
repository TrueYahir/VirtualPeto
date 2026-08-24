using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using WpfAnimatedGif;

namespace VirtualPeto.Objects
{
    public enum PetObjectType
    {
        Food,
        Toy,
        Jukebox
    }

    public abstract class PetInteractableObject : PetWindowBase
    {
        private readonly DispatcherTimer _lifeTimer;
        private Image? _spriteImage;
        private BitmapImage? _spriteSheet;
        private DispatcherTimer? _animationTimer;
        private int _currentFrame = 0;
        private int _totalFrames;
        private int _frameWidth;
        private int _frameHeight;

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

        public void SetAnimation(AnimationData anim, string petDirectory)
        {
            if (anim == null || string.IsNullOrWhiteSpace(anim.FilePath)) return;

            string fullPath = Path.Combine(petDirectory, anim.FilePath);
            if (!File.Exists(fullPath)) return;

            _frameWidth = anim.FrameWidth > 0 ? anim.FrameWidth : 64;
            _frameHeight = anim.FrameHeight > 0 ? anim.FrameHeight : 64;

            Width = _frameWidth;
            Height = _frameHeight;

            _spriteImage = new Image
            {
                Width = _frameWidth,
                Height = _frameHeight,
                Stretch = Stretch.None,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top
            };

            Content = _spriteImage;

            if (anim.IsSpriteSheet)
            {
                _totalFrames = anim.TotalFrames > 0 ? anim.TotalFrames : 1;
                int fps = anim.Fps > 0 ? anim.Fps : 10;

                _spriteSheet = new BitmapImage();
                _spriteSheet.BeginInit();
                _spriteSheet.UriSource = new Uri(fullPath, UriKind.Absolute);
                _spriteSheet.CacheOption = BitmapCacheOption.OnLoad;
                _spriteSheet.EndInit();

                _currentFrame = 0;
                
                _animationTimer = new DispatcherTimer(DispatcherPriority.Render);
                _animationTimer.Interval = TimeSpan.FromMilliseconds(1000.0 / fps);
                _animationTimer.Tick += (s, e) =>
                {
                    if (_spriteSheet == null || _totalFrames <= 0) return;
                    int pw = _spriteSheet.PixelWidth;
                    int ph = _spriteSheet.PixelHeight;
                    if (pw == 0 || ph == 0) return;

                    int columns = Math.Max(1, pw / _frameWidth);
                    int x = (_currentFrame % columns) * _frameWidth;
                    int y = (_currentFrame / columns) * _frameHeight;

                    int cropX = Math.Min(x, pw - 1);
                    int cropY = Math.Min(y, ph - 1);
                    int cropW = Math.Min(_frameWidth, pw - cropX);
                    int cropH = Math.Min(_frameHeight, ph - cropY);

                    if (cropW > 0 && cropH > 0 && _spriteImage != null)
                    {
                        _spriteImage.Source = new CroppedBitmap(_spriteSheet, new Int32Rect(cropX, cropY, cropW, cropH));
                    }
                    _currentFrame = (_currentFrame + 1) % _totalFrames;
                };
                _animationTimer.Start();
                
                int pwStart = _spriteSheet.PixelWidth;
                int phStart = _spriteSheet.PixelHeight;
                if (pwStart > 0 && phStart > 0 && _spriteImage != null)
                {
                    int startW = Math.Min(_frameWidth, pwStart);
                    int startH = Math.Min(_frameHeight, phStart);
                    _spriteImage.Source = new CroppedBitmap(_spriteSheet, new Int32Rect(0, 0, startW, startH));
                }
            }
            else
            {
                BitmapImage img = new BitmapImage();
                img.BeginInit();
                img.UriSource = new Uri(fullPath, UriKind.Absolute);
                img.CacheOption = BitmapCacheOption.OnLoad;
                img.EndInit();

                if (fullPath.ToLower().EndsWith(".gif") && _spriteImage != null)
                {
                    ImageBehavior.SetAnimatedSource(_spriteImage, img);
                }
                else if (_spriteImage != null)
                {
                    _spriteImage.Source = img;
                }
            }
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
            _animationTimer?.Stop();
            PetObjectRegistry.Unregister(this);
            base.OnClosed(e);
        }
    }
}
