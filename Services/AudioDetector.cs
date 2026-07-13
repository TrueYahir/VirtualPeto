// VirtualPeto.Services.AudioDetector (modificado)
using System;
using NAudio.Wave;
using System.Threading;

namespace VirtualPeto.Services
{
    public class AudioDetector : IDisposable
    {
        private WasapiLoopbackCapture? _audioCapture;
        private readonly object _lock = new object();
        private bool _isRunning = false;
        private int _silenceMsThreshold = 300;
        private int _detectMsThreshold = 150;
        private bool _isAudioActive = false;
        private const int FrameDurationMs = 50;

        public static readonly AudioDetector Instance = new AudioDetector();

        public event Action? AudioDetected;
        public event Action? AudioStopped;

        private AudioDetector() { }

        public void Start()
        {
            lock (_lock)
            {
                if (_isRunning) return;
                _isRunning = true;
                _isAudioActive = false;

                _audioCapture = new WasapiLoopbackCapture();
                int consecutiveDetect = 0;
                int consecutiveSilence = 0;
                _audioCapture.DataAvailable += (s, e) =>
                {
                    float max = 0f;
                    int step = 4;
                    int limit = e.BytesRecorded - (e.BytesRecorded % step);
                    for (int index = 0; index < limit; index += step)
                    {
                        float sample = BitConverter.ToSingle(e.Buffer, index);
                        if (Math.Abs(sample) > max) max = Math.Abs(sample);
                    }

                    if (max > 0.08f)
                    {
                        consecutiveDetect++;
                        consecutiveSilence = 0;
                        int detectFrameThreshold = Math.Max(1, _detectMsThreshold / FrameDurationMs);
                        if (!_isAudioActive && consecutiveDetect >= detectFrameThreshold)
                        {
                            _isAudioActive = true;
                            AudioDetected?.Invoke();
                        }
                    }
                    else
                    {
                        consecutiveSilence++;
                        consecutiveDetect = 0;
                        int silenceFrameThreshold = Math.Max(1, _silenceMsThreshold / FrameDurationMs);
                        if (_isAudioActive && consecutiveSilence >= silenceFrameThreshold)
                        {
                            _isAudioActive = false;
                            AudioStopped?.Invoke();
                        }
                    }
                };
                _audioCapture.StartRecording();
            }
        }

        public void Stop()
        {
            lock (_lock)
            {
                if (!_isRunning) return;
                _isRunning = false;
                try { _audioCapture?.StopRecording(); } catch { }
                try { _audioCapture?.Dispose(); } catch { }
                _audioCapture = null;
            }
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
