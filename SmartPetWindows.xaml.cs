using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows.Interop;
using System.Windows.Threading;
using WpfAnimatedGif;
using VirtualPeto.Services;
using VirtualPeto.Objects;
using System.Threading;

namespace VirtualPeto
{
    public enum PetState
    {
        Idle,
        Walking,
        Running,
        Intro,
        Outro,
        Clicked,
        Dragged,
        Sleep,
        WakeUp,
        Listening,
        Notification,
        Eating,
        Playing,
        Satisfied
    }

    public partial class SmartPetWindow : PetWindowBase
    {
        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOACTIVATE = 0x0010;

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT { public int X; public int Y; }
        [StructLayout(LayoutKind.Sequential)]
        struct LASTINPUTINFO
        {
            public uint cbSize;
            public uint dwTime;
        }

        [DllImport("user32.dll")]
        static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

        static uint GetIdleTime()
        {
            LASTINPUTINFO lastInPut = new LASTINPUTINFO();
            lastInPut.cbSize = (uint)Marshal.SizeOf(lastInPut);
            GetLastInputInfo(ref lastInPut);
            return (uint)Environment.TickCount - lastInPut.dwTime;
        }

        private PetMetadata _metadata;
        private string _petDirectory;
        //private AudioDetector _audioDetector;
        //private NotificationDetector _notificationDetector;

        private BitmapImage? _spriteSheet;
        private DispatcherTimer _animationTimer;
        private DispatcherTimer _behaviorTimer;
        private DispatcherTimer _idleCheckTimer;

        private int _currentFrame = 0;
        private int _frameWidth;
        private int _frameHeight;
        private int _totalFrames;
        private int _columns;
        private PetState _currentState = PetState.Idle;
        private double _vx = 0;
        private double _vy = 0;
        private double _behaviorTicks = 0;
        private Random _random = new Random();
        private bool _isClosing = false;
        private DateTime? _listeningSinceUtc = null;
        private DateTime _lastBehaviorUpdateUtc = DateTime.UtcNow;
        private bool _hasActiveAnimationAudio = false;
        private static int _activeAnimationAudioSources = 0;
        private bool _isMouseDown = false;
        private bool _isDragging = false;
        private DateTime _mouseDownTime;
        private double _dragStartLeft;
        private double _dragStartTop;
        private const double DragThreshold = 8.0;
        private static readonly TimeSpan DragActivationDelay = TimeSpan.FromMilliseconds(120);
        private Point _mouseScreenOffset;
        private string? _currentAnimationPath;
        public bool AllowMultiMonitor {get; set;} = true;
        public int TargetMonitorIndex {get; set;} = 0;
        private const double BehaviorFrameMs = 16.0;
        private const double WalkSpeed = 1.1;
        private const double RunSpeed = 2.4;
        private bool _isMovementLocked = false;
        private bool _isFollowingMouse = false;
        private PetInteractableObject? _interactionTarget;
        private double _interactionTicks = 0;
        

        public SmartPetWindow(PetMetadata metadata, string petDirectory)
        {
            InitializeComponent();
            _metadata = metadata;
            _petDirectory = petDirectory;

            _animationTimer = new DispatcherTimer(DispatcherPriority.Render);
            _animationTimer.Tick += UpdateFrame;

            UseDefaultDrag = false;

            _behaviorTimer = new DispatcherTimer(DispatcherPriority.Render);
            _behaviorTimer.Interval = TimeSpan.FromMilliseconds(16);
            _behaviorTimer.Tick += UpdateBehavior;
            _behaviorTimer.Start();

            _idleCheckTimer = new DispatcherTimer();
            _idleCheckTimer.Interval = TimeSpan.FromSeconds(1);
            _idleCheckTimer.Tick += CheckSystemIdle;
            _idleCheckTimer.Start();
            _lastBehaviorUpdateUtc = DateTime.UtcNow;

            this.MouseLeftButtonDown += SmartPetWindow_MouseLeftButtonDown;
            this.MouseLeftButtonUp += SmartPetWindow_MouseLeftButtonUp;
            this.MouseMove += SmartPetWindow_MouseMove;

            SetState(PetState.Intro);

            VirtualPeto.Services.AudioDetector.Instance.AudioDetected += OnGlobalAudioDetected;
            VirtualPeto.Services.AudioDetector.Instance.AudioStopped += OnGlobalAudioStopped;
            VirtualPeto.Services.NotificationDetector.Instance.NotificationDetected += OnGlobalNotificationDetected;
            VirtualPeto.Services.AudioDetector.Instance.Start();
            VirtualPeto.Services.NotificationDetector.Instance.Start();
        }

        private void SetState(PetState newState)
        {
            if (_isClosing && newState != PetState.Outro) return;
            if (_currentState == newState) return;
            _currentState = newState;
            _behaviorTicks = _random.Next(120, 300);

            switch (newState)
            {
                case PetState.Intro:
                    _listeningSinceUtc = null;
                    _vx = 0; _vy = 0;
                    _behaviorTicks = GetOneShotTicks(_metadata.IntroAnimation, 1000);
                    ChangeAnimation(!string.IsNullOrEmpty(_metadata.IntroAnimation.FilePath) ? _metadata.IntroAnimation : _metadata.IdleAnimation);
                    break;

                case PetState.Clicked:
                    _listeningSinceUtc = null;
                    _vx = 0; _vy = 0;
                    _behaviorTicks = GetOneShotTicks(_metadata.ClickedAnimation, 700);
                    ChangeAnimation(!string.IsNullOrEmpty(_metadata.ClickedAnimation.FilePath) ? _metadata.ClickedAnimation : _metadata.IdleAnimation);
                    break;

                case PetState.Dragged:
                    _listeningSinceUtc = null;
                    _vx = 0; _vy = 0;
                    ChangeAnimation(!string.IsNullOrEmpty(_metadata.DraggedAnimation.FilePath) ? _metadata.DraggedAnimation : _metadata.IdleAnimation);
                    break;

                case PetState.Outro:
                    _listeningSinceUtc = null;
                    _vx = 0; _vy = 0;
                    _behaviorTicks = GetOneShotTicks(_metadata.OutroAnimation, 1000);
                    ChangeAnimation(!string.IsNullOrEmpty(_metadata.OutroAnimation.FilePath) ? _metadata.OutroAnimation : _metadata.IdleAnimation);
                    break;

                case PetState.Idle:
                    _listeningSinceUtc = null;
                    _vx = 0; _vy = 0;
                    ChangeAnimation(_metadata.IdleAnimation);
                    break;   

                case PetState.Sleep:
                    _listeningSinceUtc = null;
                    _vx = 0; _vy = 0;
                    _behaviorTicks = int.MaxValue;
                    ChangeAnimation(!string.IsNullOrEmpty(_metadata.SleepAnimation.FilePath) ? _metadata.SleepAnimation : _metadata.IdleAnimation);
                    break;           

                case PetState.Walking:
                    _listeningSinceUtc = null;
                    int dir = _random.Next(8);
                    double speed = WalkSpeed;
                    AnimationData animToPlay = _metadata.IdleAnimation;
                    switch (dir)
                    {
                        case 0: _vx = speed; _vy = speed; animToPlay = GetAnimOrDefault("Walk_DownRight"); break;
                        case 1: _vx = -speed; _vy = speed; animToPlay = GetAnimOrDefault("Walk_DownLeft"); break;
                        case 2: _vx = speed; _vy = -speed; animToPlay = GetAnimOrDefault("Walk_UpRight"); break;
                        case 3: _vx = -speed; _vy = -speed; animToPlay = GetAnimOrDefault("Walk_UpLeft"); break;
                        
                        case 4: _vx = 0; _vy = -speed; animToPlay = GetAnimOrDefault("Walk_Up"); break;
                        case 5: _vx = 0; _vy = speed; animToPlay = GetAnimOrDefault("Walk_Down"); break;
                        case 6: _vx = -speed; _vy = 0; animToPlay = GetAnimOrDefault("Walk_Left"); break;
                        case 7: _vx = speed; _vy = 0; animToPlay = GetAnimOrDefault("Walk_Right"); break;
                    }
                    ChangeAnimation(animToPlay);
                    break;

                case PetState.Running:
                    _listeningSinceUtc = null;
                    int runDir = _random.Next(8); 
                    double runSpeed = RunSpeed; 
                    AnimationData runAnim = _metadata.IdleAnimation; 
                    switch (runDir)
                    {
                        case 0: _vx = runSpeed; _vy = runSpeed; runAnim = GetAnimOrDefault("Run_DownRight"); break;
                        case 1: _vx = -runSpeed; _vy = runSpeed; runAnim = GetAnimOrDefault("Run_DownLeft"); break;
                        case 2: _vx = runSpeed; _vy = -runSpeed; runAnim = GetAnimOrDefault("Run_UpRight"); break;
                        case 3: _vx = -runSpeed; _vy = -runSpeed; runAnim = GetAnimOrDefault("Run_UpLeft"); break;

                        case 4: _vx = 0; _vy = -runSpeed; runAnim = GetAnimOrDefault("Run_Up"); break;
                        case 5: _vx = 0; _vy = runSpeed; runAnim = GetAnimOrDefault("Run_Down"); break;
                        case 6: _vx = -runSpeed; _vy = 0; runAnim = GetAnimOrDefault("Run_Left"); break;
                        case 7: _vx = runSpeed; _vy = 0; runAnim = GetAnimOrDefault("Run_Right"); break;
                    }
                    ChangeAnimation(runAnim);
                    break;
                case PetState.WakeUp:
                    _listeningSinceUtc = null;
                    _vx = 0; _vy = 0;
                    _behaviorTicks = GetOneShotTicks(_metadata.WakeUpAnimation, 1000);
                    ChangeAnimation(!string.IsNullOrEmpty(_metadata.WakeUpAnimation.FilePath) ? _metadata.WakeUpAnimation : _metadata.IdleAnimation);
                    break;

                case PetState.Listening:
                    _listeningSinceUtc = DateTime.UtcNow;
                    _vx = 0; _vy = 0;
                    ChangeAnimation(!string.IsNullOrEmpty(_metadata.ListeningAnimation.FilePath) ? _metadata.ListeningAnimation : _metadata.IdleAnimation);
                    break;

                case PetState.Notification:
                    _listeningSinceUtc = null;
                    _vx = 0; _vy = 0;
                    _behaviorTicks = GetOneShotTicks(_metadata.NotificationAnimation, 900);
                    ChangeAnimation(!string.IsNullOrEmpty(_metadata.NotificationAnimation.FilePath) ? _metadata.NotificationAnimation : _metadata.IdleAnimation);
                    break;

                case PetState.Eating:
                    _listeningSinceUtc = null;
                    _vx = 0; _vy = 0;
                    _behaviorTicks = Math.Max(20, _interactionTicks);
                    ChangeAnimation(GetCustomActionAnimation("Eat", _metadata.ClickedAnimation));
                    break;

                case PetState.Playing:
                    _listeningSinceUtc = null;
                    _vx = 0; _vy = 0;
                    _behaviorTicks = Math.Max(20, _interactionTicks);
                    ChangeAnimation(GetCustomActionAnimation("Play", _metadata.ClickedAnimation));
                    break;

                case PetState.Satisfied:
                    _listeningSinceUtc = null;
                    _vx = 0; _vy = 0;
                    _behaviorTicks = GetOneShotTicks(GetCustomActionAnimation("Satisfied", _metadata.NotificationAnimation), 900);
                    ChangeAnimation(GetCustomActionAnimation("Satisfied", _metadata.NotificationAnimation));
                    break;
            }
        }

        private double GetOneShotTicks(AnimationData anim, int fallbackMs)
        {
            if (anim != null && anim.TotalFrames > 0)
            {
                int fps = anim.Fps > 0 ? anim.Fps : 10;
                double durationMs = (anim.TotalFrames * 1000.0) / fps;
                return Math.Max(1.0, durationMs / BehaviorFrameMs);
            }
            return Math.Max(1.0, fallbackMs / BehaviorFrameMs);
        }

        private AnimationData GetAnimOrDefault(string key)
        {
            if (_metadata.Movements.ContainsKey(key) && !string.IsNullOrEmpty(_metadata.Movements[key].FilePath))
                return _metadata.Movements[key];
            return _metadata.IdleAnimation;
        }

        private AnimationData GetCustomActionAnimation(string actionName, AnimationData fallback)
        {
            RandomAction? action = _metadata.RandomActions?.FirstOrDefault(a =>
                string.Equals(a.ActionName, actionName, StringComparison.OrdinalIgnoreCase) &&
                a.Animation != null &&
                !string.IsNullOrWhiteSpace(a.Animation.FilePath));

            if (action?.Animation != null) return action.Animation;
            return !string.IsNullOrWhiteSpace(fallback.FilePath) ? fallback : _metadata.IdleAnimation;
        }

        private void ChangeAnimation(AnimationData anim)
        {
            if (anim == null || string.IsNullOrWhiteSpace(anim.FilePath)) return;

            string fullPath = Path.Combine(_petDirectory, anim.FilePath);
            if (!File.Exists(fullPath)) return;
            if (string.Equals(_currentAnimationPath, fullPath, StringComparison.OrdinalIgnoreCase)) return;

            if (!string.IsNullOrEmpty(anim.SoundPath))
            {
                string soundFullPath = Path.Combine(_petDirectory, anim.SoundPath);
                bool shouldLoop = _currentState == PetState.Idle || 
                                  _currentState == PetState.Sleep || 
                                  _currentState == PetState.Walking;

                PlayAudio(soundFullPath, 1.0, shouldLoop);
                if (!_hasActiveAnimationAudio)
                {
                    _hasActiveAnimationAudio = true;
                    Interlocked.Increment(ref _activeAnimationAudioSources);
                }
            }
            else
            {
                StopAudio();
                if (_hasActiveAnimationAudio)
                {
                    _hasActiveAnimationAudio = false;
                    Interlocked.Decrement(ref _activeAnimationAudioSources);
                }
            }

            _animationTimer.Stop();
            _spriteSheet = null;
            ImageBehavior.SetAnimatedSource(PetSprite, null);
            PetSprite.Source = null;

            _frameWidth = anim.FrameWidth > 0 ? anim.FrameWidth : 64;
            _frameHeight = anim.FrameHeight > 0 ? anim.FrameHeight : 64;

            this.SizeToContent = SizeToContent.Manual;
            this.Width = _frameWidth;
            this.Height = _frameHeight;

            PetSprite.Width = _frameWidth;
            PetSprite.Height = _frameHeight;
            PetSprite.Stretch = System.Windows.Media.Stretch.None;
            PetSprite.Margin = new Thickness(0);
            PetSprite.HorizontalAlignment = HorizontalAlignment.Left;
            PetSprite.VerticalAlignment = VerticalAlignment.Top;
            PetSprite.ClipToBounds = false;

            _currentAnimationPath = fullPath;

            if (anim.IsSpriteSheet)
            {
                _totalFrames = anim.TotalFrames > 0 ? anim.TotalFrames : 1;
                int fps = anim.Fps > 0 ? anim.Fps : 10;

                _spriteSheet = new BitmapImage();
                _spriteSheet.BeginInit();
                _spriteSheet.UriSource = new Uri(fullPath, UriKind.Absolute);
                _spriteSheet.CacheOption = BitmapCacheOption.OnLoad;
                _spriteSheet.EndInit();

                int maxColumns = Math.Max(1, _spriteSheet.PixelWidth / _frameWidth);
                int maxRows = Math.Max(1, _spriteSheet.PixelHeight / _frameHeight);
                int maxFrames = maxColumns * maxRows;
                if(_totalFrames > maxFrames)
                {
                    _totalFrames = maxFrames;
                }
                _currentFrame = 0;
                _animationTimer.Interval = TimeSpan.FromMilliseconds(1000.0 / fps);
                _animationTimer.Start();
                UpdateFrame(null, EventArgs.Empty);
            }
            else
            {
                BitmapImage img = new BitmapImage();
                img.BeginInit();
                img.UriSource = new Uri(fullPath, UriKind.Absolute);
                img.CacheOption = BitmapCacheOption.OnLoad;
                img.EndInit();

                if (fullPath.ToLower().EndsWith(".gif"))
                {
                    ImageBehavior.SetAnimatedSource(PetSprite, img);
                }
                else
                {
                    PetSprite.Source = img;
                }
            }
        }

        private void UpdateFrame(object? sender, EventArgs e)
        {
            if (_spriteSheet == null || _totalFrames <= 0) return;
            if (_spriteSheet.PixelWidth == 0 || _spriteSheet.PixelHeight == 0) return;
            _columns = Math.Max(1, _spriteSheet.PixelWidth / _frameWidth);
            int x = (_currentFrame % _columns) * _frameWidth;
            int y = (_currentFrame / _columns) * _frameHeight;
            Int32Rect rect = new Int32Rect(x, y, _frameWidth, _frameHeight);
            PetSprite.Source = new CroppedBitmap(_spriteSheet, rect);
            _currentFrame = (_currentFrame + 1) % _totalFrames;
        }

        private void UpdateBehavior(object? sender, EventArgs e)
        {
            DateTime nowUtc = DateTime.UtcNow;
            double deltaMs = (nowUtc - _lastBehaviorUpdateUtc).TotalMilliseconds;
            _lastBehaviorUpdateUtc = nowUtc;
            if (deltaMs <= 0 || deltaMs > 250) deltaMs = 16;
            double frameScale = deltaMs / 16.0;

            if (_isDragging && Mouse.LeftButton == MouseButtonState.Released)
            {
                _isDragging = false;
                _isMouseDown = false;
                try { this.ReleaseMouseCapture(); } catch { }
                SetState(PetState.Idle);
            }

            if (_currentState == PetState.Dragged) return;

            if (ProcessObjectInteraction(frameScale))
            {
                return;
            }

            if (_isFollowingMouse && !_isMovementLocked && !_isDragging && !_isMouseDown &&
                _currentState != PetState.Intro && _currentState != PetState.Outro &&
                _currentState != PetState.Clicked && _currentState != PetState.Sleep &&
                _currentState != PetState.Listening && _currentState != PetState.Notification)
            {
                GetCursorPos(out POINT cursorPoint);
                var source = PresentationSource.FromVisual(this);
                Point cursorDip = source != null
                    ? source.CompositionTarget.TransformFromDevice.Transform(new Point(cursorPoint.X, cursorPoint.Y))
                    : new Point(cursorPoint.X, cursorPoint.Y);

                double targetLeft = cursorDip.X - (this.Width / 2.0);
                double targetTop = cursorDip.Y - (this.Height / 2.0);
                double dx = targetLeft - this.Left;
                double dy = targetTop - this.Top;
                double distance = Math.Sqrt(dx * dx + dy * dy);

                if (distance > 2)
                {
                    double step = Math.Min(distance, RunSpeed * 1.5 * frameScale);
                    this.Left += (dx / distance) * step;
                    this.Top += (dy / distance) * step;

                    if (_currentState != PetState.Walking && _currentState != PetState.Running)
                    {
                        SetState(PetState.Walking);
                    }
                }
                else if (_currentState == PetState.Walking || _currentState == PetState.Running)
                {
                    SetState(PetState.Idle);
                }
            }

            if (_currentState == PetState.Intro || _currentState == PetState.Clicked || _currentState == PetState.Outro || _currentState == PetState.WakeUp || _currentState == PetState.Notification || _currentState == PetState.Satisfied)
            {
                _behaviorTicks -= frameScale;
                if (_behaviorTicks <= 0)
                {
                    if (_currentState == PetState.Outro) base.Close();
                    else SetState(PetState.Idle);
                }
                return;
            }
            if (_currentState == PetState.Listening)
            {
                if (_listeningSinceUtc.HasValue && DateTime.UtcNow - _listeningSinceUtc.Value > TimeSpan.FromSeconds(8))
                {
                    SetState(PetState.Idle);
                }
                return;
            }

            if (_currentState == PetState.Walking)
            {
                this.Left += _vx * frameScale;
                this.Top += _vy * frameScale;
                var workArea = GetAllowedArea();
                bool bounced = false;

                if (this.Left < workArea.Left) { this.Left = workArea.Left; _vx = -_vx; bounced = true; }
                else if (this.Left + this.Width > workArea.Right) { this.Left = workArea.Right - this.Width; _vx = -_vx; bounced = true; }

                if (this.Top < workArea.Top) { this.Top = workArea.Top; _vy = -_vy; bounced = true; }
                else if (this.Top + this.Height > workArea.Bottom) { this.Top = workArea.Bottom - this.Height; _vy = -_vy; bounced = true; }

                if (bounced)
                {
                    if (_vx > 0 && _vy > 0) ChangeAnimation(GetAnimOrDefault("Walk_DownRight"));
                    else if (_vx < 0 && _vy > 0) ChangeAnimation(GetAnimOrDefault("Walk_DownLeft"));
                    else if (_vx > 0 && _vy < 0) ChangeAnimation(GetAnimOrDefault("Walk_UpRight"));
                    else if (_vx < 0 && _vy < 0) ChangeAnimation(GetAnimOrDefault("Walk_UpLeft"));
                    else if (_vx == 0 && _vy < 0) ChangeAnimation(GetAnimOrDefault("Walk_Up"));
                    else if (_vx == 0 && _vy > 0) ChangeAnimation(GetAnimOrDefault("Walk_Down"));
                    else if (_vx < 0 && _vy == 0) ChangeAnimation(GetAnimOrDefault("Walk_Left"));
                    else if (_vx > 0 && _vy == 0) ChangeAnimation(GetAnimOrDefault("Walk_Right"));
                }
            }
            else if (_currentState == PetState.Running)
            {
                this.Left += _vx * frameScale;
                this.Top += _vy * frameScale;
                var workArea = GetAllowedArea();
                bool bounced = false;

                if (this.Left < workArea.Left) { this.Left = workArea.Left; _vx = -_vx; bounced = true; }
                else if (this.Left + this.Width > workArea.Right) { this.Left = workArea.Right - this.Width; _vx = -_vx; bounced = true; }

                if (this.Top < workArea.Top) { this.Top = workArea.Top; _vy = -_vy; bounced = true; }
                else if (this.Top + this.Height > workArea.Bottom) { this.Top = workArea.Bottom - this.Height; _vy = -_vy; bounced = true; }
                if (bounced)
                {
                    if (_vx > 0 && _vy > 0) ChangeAnimation(GetAnimOrDefault("Run_DownRight"));
                    else if (_vx < 0 && _vy > 0) ChangeAnimation(GetAnimOrDefault("Run_DownLeft"));
                    else if (_vx > 0 && _vy < 0) ChangeAnimation(GetAnimOrDefault("Run_UpRight"));
                    else if (_vx < 0 && _vy < 0) ChangeAnimation(GetAnimOrDefault("Run_UpLeft"));
                        
                    else if (_vx == 0 && _vy < 0) ChangeAnimation(GetAnimOrDefault("Run_Up"));
                    else if (_vx == 0 && _vy > 0) ChangeAnimation(GetAnimOrDefault("Run_Down"));
                    else if (_vx < 0 && _vy == 0) ChangeAnimation(GetAnimOrDefault("Run_Left"));
                    else if (_vx > 0 && _vy == 0) ChangeAnimation(GetAnimOrDefault("Run_Right"));
                }
            }

            _behaviorTicks -= frameScale;
            if (_behaviorTicks <= 0)
            {
                if (_currentState == PetState.Idle || _currentState == PetState.Walking || _currentState == PetState.Running)
                {
                    if (_isMovementLocked)
                    {
                        SetState(PetState.Idle);
                        _behaviorTicks = _random.Next(120, 260);
                    }
                    else
                    {
                        int r = _random.Next(100);
                        if (r < 5) 
                        {
                            SetState(PetState.Running);
                            _behaviorTicks = _random.Next(20, 36); 
                        }
                        else if (r < 20) 
                        {
                            SetState(PetState.Walking);
                            _behaviorTicks = _random.Next(24, 52);
                        }
                        else 
                        {
                            SetState(PetState.Idle);
                            _behaviorTicks = _random.Next(120, 260);
                        }
                    }
                    
                }
            }
        }

        private bool ProcessObjectInteraction(double frameScale)
        {
            if (_isClosing || _isDragging || _isMouseDown) return false;
            if (_currentState == PetState.Intro || _currentState == PetState.Outro || _currentState == PetState.Sleep || _currentState == PetState.Listening || _currentState == PetState.Notification) return false;
            if (_isMovementLocked && _interactionTarget == null) return false;

            Point petCenter = GetPetCenter();

            if (_interactionTarget == null || !_interactionTarget.CanBeInteracted())
            {
                _interactionTarget = PetObjectRegistry.FindNearestAvailable(petCenter, 260);
                _interactionTicks = 0;
                if (_interactionTarget == null) return false;
            }

            if (!_interactionTarget.IsCarried)
            {
                double distance = _interactionTarget.DistanceTo(petCenter);
                if (distance > _interactionTarget.PickupRadius)
                {
                    Point targetCenter = _interactionTarget.GetCenter();
                    double dx = targetCenter.X - petCenter.X;
                    double dy = targetCenter.Y - petCenter.Y;
                    double step = Math.Min(distance, RunSpeed * 1.7 * frameScale);
                    if (distance > 0)
                    {
                        Left += (dx / distance) * step;
                        Top += (dy / distance) * step;
                    }
                    if (_currentState != PetState.Walking && _currentState != PetState.Running)
                    {
                        SetState(PetState.Walking);
                    }
                    return true;
                }

                _interactionTarget.AttachToPet(this);
                if (_interactionTarget is FoodObject food)
                {
                    _interactionTicks = Math.Max(15, food.ConsumeDuration.TotalMilliseconds / BehaviorFrameMs);
                    SetState(PetState.Eating);
                }
                else if (_interactionTarget is ToyObject toy)
                {
                    _interactionTicks = Math.Max(15, toy.PlayDuration.TotalMilliseconds / BehaviorFrameMs);
                    SetState(PetState.Playing);
                }
                else
                {
                    _interactionTicks = 30;
                    SetState(PetState.Playing);
                }
            }

            Point carryAnchor = new Point(Left + Width / 2.0, Top + Height * 0.72);
            _interactionTarget.UpdateCarriedPosition(carryAnchor);
            _interactionTicks -= frameScale;

            if (_interactionTicks <= 0)
            {
                _interactionTarget.TryConsume();
                _interactionTarget = null;
                _interactionTicks = 0;
                SetState(PetState.Satisfied);
            }

            return true;
        }

        private Point GetPetCenter()
        {
            return new Point(Left + Width / 2.0, Top + Height / 2.0);
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (!_isClosing)
            {
                e.Cancel = true;
                _isClosing = true;
                SetState(PetState.Outro);
            }
            else if(_currentState == PetState.Outro && _behaviorTicks > 0){
                e.Cancel = true;
            }
            else
            {
                base.OnClosing(e);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            VirtualPeto.Services.AudioDetector.Instance.AudioDetected -= OnGlobalAudioDetected;
            VirtualPeto.Services.AudioDetector.Instance.AudioStopped -= OnGlobalAudioStopped;
            VirtualPeto.Services.NotificationDetector.Instance.NotificationDetected -= OnGlobalNotificationDetected;

            _animationTimer?.Stop();
            _behaviorTimer?.Stop();
            _idleCheckTimer?.Stop();
            if (_hasActiveAnimationAudio)
            {
                _hasActiveAnimationAudio = false;
                Interlocked.Decrement(ref _activeAnimationAudioSources);
            }
            _interactionTarget = null;
            _interactionTicks = 0;
            base.OnClosed(e);
        }

        private void CheckSystemIdle(object? sender, EventArgs e)
        {
            if (_isClosing || _isDragging || _isMouseDown || _currentState == PetState.Intro || _currentState == PetState.Outro || _currentState == PetState.Clicked || _currentState == PetState.Eating || _currentState == PetState.Playing) return;

            int sleepMinutes = Math.Max(0, SettingsManager.Current.SleepTimeMinutes);
            if (sleepMinutes == 0)
            {
                if (_currentState == PetState.Sleep) SetState(PetState.Idle);
                return;
            }

            uint idleThresholdMs = (uint)Math.Min(int.MaxValue, sleepMinutes * 60_000);
            if (GetIdleTime() > idleThresholdMs)
            {
                if (_currentState != PetState.Sleep) SetState(PetState.Sleep);
            }
            else
            {
                if (_currentState == PetState.Sleep) SetState(PetState.Idle);
            }
        }

        private Rect GetAllowedArea()
        {
            if (AllowMultiMonitor)
            {
                return new Rect(
                    SystemParameters.VirtualScreenLeft,
                    SystemParameters.VirtualScreenTop,
                    SystemParameters.VirtualScreenWidth,
                    SystemParameters.VirtualScreenHeight
                );
            }
            else
            {
                var screens = System.Windows.Forms.Screen.AllScreens;
                if(TargetMonitorIndex >= 0 && TargetMonitorIndex < screens.Length)
                {
                    var bounds = screens[TargetMonitorIndex].WorkingArea;
                    return new Rect(bounds.Left, bounds.Top, bounds.Width, bounds.Height);
                }
                return SystemParameters.WorkArea;
            }
        }

        //BUTTONS LOGIC

        private void SmartPetWindow_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _mouseDownTime = DateTime.Now;
            _isMouseDown = true;
            _isDragging = false;

            GetCursorPos(out POINT p);
            var source = PresentationSource.FromVisual(this);
            Point dip = source != null ? source.CompositionTarget.TransformFromDevice.Transform(new Point(p.X, p.Y)) : new Point(p.X, p.Y);

            _mouseScreenOffset = new Point(dip.X - this.Left, dip.Y - this.Top);
            _dragStartLeft = this.Left;
            _dragStartTop = this.Top;

            this.CaptureMouse();
        }
        private void PetContextMenu_Opened(object sender, RoutedEventArgs e)
        {
            if (sender is ContextMenu menu)
            {
                menu.PlacementTarget = this;
                menu.Placement = PlacementMode.Right;
                menu.HorizontalOffset = 8;
                menu.VerticalOffset = 0;

                if (PresentationSource.FromVisual(menu) is HwndSource hwndSource)
                {
                    SetWindowPos(hwndSource.Handle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
                }
            }

            MenuPinPet.Header = _isMovementLocked ? "Unpin Pet" : "Pin Pet";
            MenuFollowMouse.Header = _isFollowingMouse ? "Stop Following Mouse" : "Follow Mouse";
        }

        private void MenuClosePet_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void MenuPinPet_Click(object sender, RoutedEventArgs e)
        {
            _isMovementLocked = !_isMovementLocked;
            if (_isMovementLocked && (_currentState == PetState.Walking || _currentState == PetState.Running))
            {
                SetState(PetState.Idle);
            }
        }

        private void MenuFollowMouse_Click(object sender, RoutedEventArgs e)
        {
            _isFollowingMouse = !_isFollowingMouse;
            if (_isFollowingMouse)
            {
                _isMovementLocked = false;
            }
            else if (_currentState == PetState.Walking || _currentState == PetState.Running)
            {
                SetState(PetState.Idle);
            }
        }

        private void MenuGenerateFood_Click(object sender, RoutedEventArgs e)
        {
            Rect workArea = GetAllowedArea();
            Point spawnAnchor = new Point(Left + Width / 2.0, Top + Height / 2.0);
            PetObjectRegistry.SpawnFood(workArea, _random, spawnAnchor);
        }

        private void MenuGenerateToy_Click(object sender, RoutedEventArgs e)
        {
            Rect workArea = GetAllowedArea();
            Point spawnAnchor = new Point(Left + Width / 2.0, Top + Height / 2.0);
            PetObjectRegistry.SpawnToy(workArea, _random, spawnAnchor);
        }

        private void SmartPetWindow_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isMouseDown) return;

            GetCursorPos(out POINT p);
            var source = PresentationSource.FromVisual(this);
            Point dip = source != null ? source.CompositionTarget.TransformFromDevice.Transform(new Point(p.X, p.Y)) : new Point(p.X, p.Y);

            double targetLeft = dip.X - _mouseScreenOffset.X;
            double targetTop = dip.Y - _mouseScreenOffset.Y;

            double dx = targetLeft - _dragStartLeft;
            double dy = targetTop - _dragStartTop;
            TimeSpan heldTime = DateTime.Now - _mouseDownTime;

            if (!_isDragging && heldTime >= DragActivationDelay && (Math.Abs(dx) > DragThreshold || Math.Abs(dy) > DragThreshold))
            {
                _isDragging = true;
                if (_currentState != PetState.Dragged) SetState(PetState.Dragged);
            }

            if (_isDragging)
            {
                this.Left = targetLeft;
                this.Top = targetTop;

                var workArea = GetAllowedArea();
                if (this.Left < workArea.Left) this.Left = workArea.Left;
                if (this.Top < workArea.Top) this.Top = workArea.Top;
                if (this.Left + this.Width > workArea.Right) this.Left = workArea.Right - this.Width;
                if (this.Top + this.Height > workArea.Bottom) this.Top = workArea.Bottom - this.Height;
            }
        }

        private void SmartPetWindow_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!_isMouseDown) return;

            _isMouseDown = false;
            try { this.ReleaseMouseCapture(); } catch { }

            if (_isDragging)
            {
                _isDragging = false;
                SetState(PetState.Idle);
            }
            else
            {
                SetState(PetState.Clicked);
                _behaviorTicks = 40;
            }
        }
        private void OnGlobalAudioDetected()
        {
            if (Volatile.Read(ref _activeAnimationAudioSources) > 0) return;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (_currentState != PetState.Listening && _currentState != PetState.Sleep)
                    SetState(PetState.Listening);
            }));
        }

        private void OnGlobalAudioStopped()
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (_currentState == PetState.Listening)
                    SetState(PetState.Idle);
            }));
        }

        private void OnGlobalNotificationDetected()
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (_currentState != PetState.Sleep)
                    SetState(PetState.Notification);
            }));
        }

        public new void Close()
        {
            if (_isClosing) return;
            _isClosing = true;
            SetState(PetState.Outro);
        }
    }
}
