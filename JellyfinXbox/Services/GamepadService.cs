using Windows.UI.Xaml;
using System;
using Windows.Gaming.Input;
using Windows.System;
using System.Linq;

namespace JellyfinXbox.Services;

/// <summary>
/// Xbox gamepad input handler. Provides simplified events for common gamepad actions.
/// Uses polling rather than requiring each page to handle raw input.
/// </summary>
public class GamepadService : IDisposable
{
    private readonly DispatcherQueue _dispatcher;
    private DispatcherTimer? _pollTimer;
    private GamepadReading _lastReading;

    public event Action? OnAPressed;
    public event Action? OnBPressed;
    public event Action? OnXPressed;
    public event Action? OnYPressed;
    public event Action? OnMenuPressed;
    public event Action? OnViewPressed;
    public event Action<double>? OnLeftThumbstickX;  // -1.0 to 1.0
    public event Action<double>? OnLeftThumbstickY;  // -1.0 to 1.0
    public event Action<double>? OnRightTrigger;     // 0.0 to 1.0
    public event Action<double>? OnLeftTrigger;
    public event Action? OnDPadLeft;
    public event Action? OnDPadRight;
    public event Action? OnDPadUp;
    public event Action? OnDPadDown;
    public event Action? OnLeftBumper;
    public event Action? OnRightBumper;

    private const double ThumbstickDeadzone = 0.15;
    private const double TriggerThreshold = 0.1;

    public GamepadService()
    {
        _dispatcher = DispatcherQueue.GetForCurrentThread();
    }

    public void Start()
    {
        if (_pollTimer != null) return;

        _pollTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(16) // ~60Hz poll rate
        };
        _pollTimer.Tick += PollTimer_Tick;
        _pollTimer.Start();
    }

    public void Stop()
    {
        _pollTimer?.Stop();
        _pollTimer = null;
    }

    private void PollTimer_Tick(object? sender, object e)
    {
        var gamepads = Gamepad.Gamepads;
        if (gamepads.Count == 0) return;

        var reading = gamepads[0].GetCurrentReading();

        // Button pressed detection (rising edge)
        var buttons = reading.Buttons;
        var lastButtons = _lastReading.Buttons;

        if ((buttons & GamepadButtons.A) != 0 && (lastButtons & GamepadButtons.A) == 0)
            OnAPressed?.Invoke();
        if ((buttons & GamepadButtons.B) != 0 && (lastButtons & GamepadButtons.B) == 0)
            OnBPressed?.Invoke();
        if ((buttons & GamepadButtons.X) != 0 && (lastButtons & GamepadButtons.X) == 0)
            OnXPressed?.Invoke();
        if ((buttons & GamepadButtons.Y) != 0 && (lastButtons & GamepadButtons.Y) == 0)
            OnYPressed?.Invoke();
        if ((buttons & GamepadButtons.Menu) != 0 && (lastButtons & GamepadButtons.Menu) == 0)
            OnMenuPressed?.Invoke();
        if ((buttons & GamepadButtons.View) != 0 && (lastButtons & GamepadButtons.View) == 0)
            OnViewPressed?.Invoke();
        if ((buttons & GamepadButtons.LeftShoulder) != 0 && (lastButtons & GamepadButtons.LeftShoulder) == 0)
            OnLeftBumper?.Invoke();
        if ((buttons & GamepadButtons.RightShoulder) != 0 && (lastButtons & GamepadButtons.RightShoulder) == 0)
            OnRightBumper?.Invoke();

        // D-pad
        if ((buttons & GamepadButtons.DPadLeft) != 0 && (lastButtons & GamepadButtons.DPadLeft) == 0)
            OnDPadLeft?.Invoke();
        if ((buttons & GamepadButtons.DPadRight) != 0 && (lastButtons & GamepadButtons.DPadRight) == 0)
            OnDPadRight?.Invoke();
        if ((buttons & GamepadButtons.DPadUp) != 0 && (lastButtons & GamepadButtons.DPadUp) == 0)
            OnDPadUp?.Invoke();
        if ((buttons & GamepadButtons.DPadDown) != 0 && (lastButtons & GamepadButtons.DPadDown) == 0)
            OnDPadDown?.Invoke();

        // Thumbsticks (continuous)
        if (Math.Abs(reading.LeftThumbstickX) > ThumbstickDeadzone)
            OnLeftThumbstickX?.Invoke(reading.LeftThumbstickX);
        if (Math.Abs(reading.LeftThumbstickY) > ThumbstickDeadzone)
            OnLeftThumbstickY?.Invoke(reading.LeftThumbstickY);

        // Triggers (continuous)
        if (reading.RightTrigger > TriggerThreshold)
            OnRightTrigger?.Invoke(reading.RightTrigger);
        if (reading.LeftTrigger > TriggerThreshold)
            OnLeftTrigger?.Invoke(reading.LeftTrigger);

        _lastReading = reading;
    }

    public void Dispose()
    {
        Stop();
    }
}
