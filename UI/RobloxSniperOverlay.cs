using System;
using System.Windows;
using System.Windows.Media;
using ZenithOptimizer.Models;
using ZenithOptimizer.Services;
using ZenithOptimizer.UI.Sniper;

namespace ZenithOptimizer.UI
{
    /// <summary>
    /// Slim orchestrator for the Roblox Sniper Overlay UI.
    /// Manages window lifecycles, hotkeys, and coordinate synchronization.
    /// </summary>
    public class RobloxSniperOverlay : IDisposable
    {
        private static RobloxSniperOverlay _instance;
        private static readonly object _syncRoot = new object();

        public static RobloxSniperOverlay Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_syncRoot)
                    {
                        if (_instance == null) _instance = new RobloxSniperOverlay();
                    }
                }
                return _instance;
            }
        }

        private readonly SniperHudWindow _hud = new SniperHudWindow();
        private readonly SniperReticleWindow _reticle = new SniperReticleWindow();
        private readonly SniperClickTargetWindow _clickTarget = new SniperClickTargetWindow();

        private bool _isReticleLocked = false;
        private bool _isCompactMode = false;
        private bool _areMarkersHidden = false;
        private bool _isVisible = false;

        public bool IsVisible { get { return _isVisible; } }
        public bool IsReticleLocked { get { return _isReticleLocked; } }
        public bool IsCompactMode { get { return _isCompactMode; } }
        public bool AreMarkersHidden { get { return _areMarkersHidden; } }

        public RobloxSniperOverlay()
        {
            try
            {
                var s = OptimizationSettings.Load();
                if (s != null)
                {
                    _isCompactMode = s.SniperIsCompactMode;
                    _areMarkersHidden = s.SniperMarkersHidden;
                }
            }
            catch { }
        }

        public void ToggleMarkersLock() { SetMarkersClickThrough(!_isReticleLocked); }

        public void ToggleCompactMode() { SetCompactMode(!_isCompactMode); }

        public void SetCompactMode(bool compact)
        {
            if (Application.Current == null) return;
            Application.Current.Dispatcher.Invoke((Action)(() =>
            {
                _isCompactMode = compact;
                _hud.ApplyCompactModeLayout(_isCompactMode);
                try
                {
                    var s = OptimizationSettings.Load();
                    if (s != null) { s.SniperIsCompactMode = _isCompactMode; s.Save(); }
                }
                catch { }
                FlashActionText(_isCompactMode ? "COMPACT HUD" : "EXPANDED HUD", Color.FromRgb(56, 189, 248));
            }));
        }

        public void ToggleMarkersVisibility() { SetMarkersVisibility(_areMarkersHidden); }

        public void SetMarkersVisibility(bool visible)
        {
            if (Application.Current == null) return;
            Application.Current.Dispatcher.Invoke((Action)(() =>
            {
                _areMarkersHidden = !visible;
                if (_areMarkersHidden)
                {
                    _reticle.Hide();
                    _clickTarget.Hide();
                }
                else if (_isVisible)
                {
                    _reticle.Show();
                    _clickTarget.Show();
                }
                _hud.UpdateMarkersVisuals(_areMarkersHidden);

                try
                {
                    var s = OptimizationSettings.Load();
                    if (s != null) { s.SniperMarkersHidden = _areMarkersHidden; s.Save(); }
                }
                catch { }
                FlashActionText(_areMarkersHidden ? "RINGS HIDDEN" : "RINGS VISIBLE", Color.FromRgb(250, 204, 21));
            }));
        }

        public void Show()
        {
            if (Application.Current == null) return;
            Application.Current.Dispatcher.Invoke((Action)(() =>
            {
                if (_hud.Window == null) InitializeWindows();
                _isVisible = true;
                _hud.Show();
                if (!_areMarkersHidden)
                {
                    _reticle.Show();
                    _clickTarget.Show();
                }
                HookServiceEvents();
            }));
        }

        public void Hide()
        {
            if (Application.Current == null) return;
            Application.Current.Dispatcher.Invoke((Action)(() =>
            {
                _isVisible = false;
                _hud.Hide();
                _reticle.Hide();
                _clickTarget.Hide();
                UnhookServiceEvents();
            }));
        }

        public void Toggle()
        {
            if (_isVisible) Hide();
            else Show();
        }

        public void SetMarkersClickThrough(bool clickThrough)
        {
            _isReticleLocked = clickThrough;
            if (Application.Current == null) return;
            Application.Current.Dispatcher.Invoke((Action)(() =>
            {
                _hud.UpdateLockVisuals(_isReticleLocked);
                _reticle.SetLockState(_isReticleLocked);
                _clickTarget.SetLockState(_isReticleLocked);
                FlashActionText(_isReticleLocked ? "LOCK ENGAGED" : "DRAGGABLE", Color.FromRgb(250, 204, 21));
            }));
        }

        public void FlashActionText(string text, Color color)
        {
            if (Application.Current == null) return;
            Application.Current.Dispatcher.Invoke((Action)(() => _hud.FlashActionText(text, color)));
        }

        private void InitializeWindows()
        {
            _hud.Initialize(
                _isCompactMode,
                _isReticleLocked,
                _areMarkersHidden,
                ToggleMarkersLock,
                ToggleCompactMode,
                ToggleMarkersVisibility,
                Hide);
            _reticle.Initialize(_isReticleLocked);
            _clickTarget.Initialize(_isReticleLocked);
        }

        private bool _isHooked = false;
        private void HookServiceEvents()
        {
            if (_isHooked) return;
            var sniper = RobloxBidSniperService.Instance;
            sniper.OnColorSampledEx += OnColorSampledEx;
            sniper.OnSensorPositionChanged += OnSensorPositionChanged;
            sniper.OnClickTargetChanged += OnClickTargetChanged;
            sniper.OnTrackPositionChanged += OnTrackPositionChanged;
            _isHooked = true;
        }

        private void UnhookServiceEvents()
        {
            if (!_isHooked) return;
            var sniper = RobloxBidSniperService.Instance;
            sniper.OnColorSampledEx -= OnColorSampledEx;
            sniper.OnSensorPositionChanged -= OnSensorPositionChanged;
            sniper.OnClickTargetChanged -= OnClickTargetChanged;
            sniper.OnTrackPositionChanged -= OnTrackPositionChanged;
            _isHooked = false;
        }

        private void OnColorSampledEx(int r, int g, int b, bool isMatch, string captureSource)
        {
            if (!_isVisible || Application.Current == null) return;
            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                _hud.UpdateTelemetry(r, g, b, isMatch, captureSource);
                _reticle.SetReticleColor(isMatch ? Color.FromRgb(52, 211, 153) : Color.FromRgb(34, 197, 94));
            });
        }

        private void OnSensorPositionChanged(int x, int y)
        {
            if (Application.Current == null) return;
            Application.Current.Dispatcher.InvokeAsync(() => _reticle.UpdatePosition(x, y));
        }

        private void OnClickTargetChanged(int? x, int? y)
        {
            if (Application.Current == null) return;
            Application.Current.Dispatcher.InvokeAsync(() => _clickTarget.UpdatePosition(x, y));
        }

        private void OnTrackPositionChanged(int trackX, int trackY, bool hasLock)
        {
            if (Application.Current == null) return;
            Application.Current.Dispatcher.InvokeAsync(() => _reticle.UpdateCorridorState(trackX, trackY, hasLock));
        }

        public void Dispose()
        {
            UnhookServiceEvents();
            if (Application.Current != null)
            {
                Application.Current.Dispatcher.Invoke((Action)(() =>
                {
                    _hud.Close();
                    _reticle.Close();
                    _clickTarget.Close();
                }));
            }
        }
    }
}
