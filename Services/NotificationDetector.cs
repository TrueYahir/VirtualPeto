// VirtualPeto.Services.NotificationDetector (modificado)
using System;
using System.Windows.Automation;

namespace VirtualPeto.Services
{
    public class NotificationDetector : IDisposable
    {
        private AutomationElement? _desktop;
        private AutomationEventHandler? _handler;
        public static readonly NotificationDetector Instance = new NotificationDetector();

        public event Action? NotificationDetected;

        private NotificationDetector() { }

        public void Start()
        {
            if (_handler != null) return; 
            _desktop = AutomationElement.RootElement;
            _handler = new AutomationEventHandler((sender, e) =>
            {
                if (sender is AutomationElement element)
                {
                    try
                    {
                        string className = element.Current.ClassName ?? "";
                        string name = element.Current.Name ?? "";
                        if (className == "Windows.UI.Core.CoreWindow" || name.IndexOf("Notification", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            NotificationDetected?.Invoke();
                        }
                    }
                    catch { }
                }
            });

            Automation.AddAutomationEventHandler(WindowPattern.WindowOpenedEvent, _desktop, TreeScope.Children, _handler);
        }

        public void Stop()
        {
            if (_handler != null && _desktop != null)
            {
                try { Automation.RemoveAutomationEventHandler(WindowPattern.WindowOpenedEvent, _desktop, _handler); }
                catch { }
                _handler = null;
            }
        }

        public void Dispose() => Stop();
    }
}
