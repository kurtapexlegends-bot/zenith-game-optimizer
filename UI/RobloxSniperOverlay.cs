using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using ZenithOptimizer.Models;
using ZenithOptimizer.Services;
using ZenithOptimizer.UI.Sniper;

namespace ZenithOptimizer.UI
{
    /// <summary>
    /// Slim coordinator for the Roblox Sniper Overlay UI.
    /// Manages window lifecycles, hotkeys, and coordinate synchronization (<250 lines).
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

        private bool _isReticleLocked = true;
        private bool _isCompactMode = false;
        private bool _areMarkersHidden = true;
        private bool _isVisible = false;
        private bool _isHooked = false;

        public bool IsVisible { get { return _isVisible; } }
        public bool IsReticleLocked { get { return _isReticleLocked; } }
        public bool IsCompactMode { get { return _isCompactMode; } }
        public bool AreMarkersHidden { get { return _areMarkersHidden; } }

        public RobloxSniperOverlay()
        {
            try
            {
                var s = OptimizationSettings.Load();
                if (s != null) { _isCompactMode = s.SniperIsCompactMode; _areMarkersHidden = s.SniperMarkersHidden; }
            }
            catch (Exception ex) { Debug.WriteLine("[RobloxSniperOverlay] Settings load: " + ex.Message); }
        }

        public void Initialize()
        {
            if (Application.Current != null)
                Application.Current.Dispatcher.Invoke((Action)(() => { if (_hud.Window == null) InitializeWindows(); }));
        }

        public void ToggleMarkersLock()
        {
            SetMarkersClickThrough(!_isReticleLocked);
        }

        public void ToggleCompactMode()
        {
            SetCompactMode(!_isCompactMode);
        }

        public void ToggleMarkersVisibility()
        {
            SetMarkersVisibility(_areMarkersHidden);
        }

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
                catch (Exception ex) { Debug.WriteLine("[RobloxSniperOverlay] Compact mode save error: " + ex.Message); }
                FlashActionText(_isCompactMode ? "COMPACT HUD" : "EXPANDED HUD", Color.FromRgb(56, 189, 248));
            }));
        }

        public void SetMarkersVisibility(bool visible)
        {
            if (Application.Current == null) return;
            Application.Current.Dispatcher.Invoke((Action)(() =>
            {
                _areMarkersHidden = !visible;
                if (_areMarkersHidden) { _reticle.Hide(); _clickTarget.Hide(); }
                else if (_isVisible) { _reticle.Show(); _clickTarget.Show(); }
                _hud.UpdateMarkersVisuals(_areMarkersHidden);

                try
                {
                    var s = OptimizationSettings.Load();
                    if (s != null) { s.SniperMarkersHidden = _areMarkersHidden; s.Save(); }
                }
                catch (Exception ex) { Debug.WriteLine("[RobloxSniperOverlay] Markers visibility save error: " + ex.Message); }
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
                if (!_areMarkersHidden) { _reticle.Show(); _clickTarget.Show(); }
                HookServiceEvents();
                if (!RobloxBidSniperService.Instance.IsRunning)
                {
                    string msg;
                    RobloxBidSniperService.Instance.Start(out msg);
                }
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
                if (RobloxBidSniperService.Instance.IsRunning)
                {
                    string msg;
                    RobloxBidSniperService.Instance.Stop(out msg);
                }
            }));
        }

        public void Toggle() { if (_isVisible) Hide(); else Show(); }

        public void SetMarkersClickThrough(bool clickThrough)
        {
            _isReticleLocked = clickThrough;
            if (Application.Current == null) return;
            Application.Current.Dispatcher.Invoke((Action)(() =>
            {
                _hud.UpdateLockVisuals(_isReticleLocked);
                _reticle.SetLockState(_isReticleLocked);
                _clickTarget.SetLockState(_isReticleLocked);
                FlashActionText(_isReticleLocked ? "MARKERS LOCKED" : "REPOSITION ACTIVE", Color.FromRgb(250, 204, 21));
            }));
        }

        public void FlashActionText(string text, Color color)
        {
            if (Application.Current != null)
                Application.Current.Dispatcher.Invoke((Action)(() => _hud.FlashActionText(text, color)));
        }

        private void InitializeWindows()
        {
            _hud.Initialize(_isCompactMode, _isReticleLocked, _areMarkersHidden,
                ToggleMarkersLock, ToggleCompactMode, ToggleMarkersVisibility, Hide);
            _reticle.Initialize(_isReticleLocked);
            _clickTarget.Initialize(_isReticleLocked);
        }

        private void HookServiceEvents()
        {
            if (_isHooked) return;
            var sn = RobloxBidSniperService.Instance;
            sn.OnColorSampledEx += OnColorSampledEx;
            sn.OnSensorPositionChanged += OnSensorPositionChanged;
            sn.OnClickTargetChanged += OnClickTargetChanged;
            sn.OnTrackPositionChanged += OnTrackPositionChanged;
            sn.OnArmedChanged += OnArmedChanged;
            sn.OnSnipeFired += OnSnipeFired;
            _isHooked = true;
        }

        private void UnhookServiceEvents()
        {
            if (!_isHooked) return;
            var sn = RobloxBidSniperService.Instance;
            sn.OnColorSampledEx -= OnColorSampledEx;
            sn.OnSensorPositionChanged -= OnSensorPositionChanged;
            sn.OnClickTargetChanged -= OnClickTargetChanged;
            sn.OnTrackPositionChanged -= OnTrackPositionChanged;
            sn.OnArmedChanged -= OnArmedChanged;
            sn.OnSnipeFired -= OnSnipeFired;
            _isHooked = false;
        }

        private void OnArmedChanged(bool isArmed)
        {
            if (!_isVisible || Application.Current == null) return;
            Application.Current.Dispatcher.InvokeAsync(() => _hud.FlashActionText(isArmed ? "SNIPER ARMED" : "SNIPER STOPPED", isArmed ? Color.FromRgb(52, 211, 153) : Color.FromRgb(239, 68, 68)));
        }

        private void OnSnipeFired(int clickX, int clickY, int hitCount)
        {
            if (!_isVisible || Application.Current == null) return;
            Application.Current.Dispatcher.InvokeAsync(() => _hud.FlashActionText(string.Format("SNIPED #{0} ({1}, {2})", hitCount, clickX, clickY), Color.FromRgb(56, 189, 248)));
        }

        private void OnColorSampledEx(int r, int g, int b, bool isMatch, string src)
        {
            if (!_isVisible || Application.Current == null) return;
            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                _hud.UpdateTelemetry(r, g, b, isMatch, src);
                _reticle.SetReticleColor(isMatch ? Color.FromRgb(52, 211, 153) : Color.FromRgb(34, 197, 94));
            });
        }

        private void OnSensorPositionChanged(int x, int y)
        {
            if (Application.Current != null) Application.Current.Dispatcher.InvokeAsync(() => _reticle.UpdatePosition(x, y));
        }

        private void OnClickTargetChanged(int? x, int? y)
        {
            if (Application.Current != null) Application.Current.Dispatcher.InvokeAsync(() => _clickTarget.UpdatePosition(x, y));
        }

        private void OnTrackPositionChanged(int trackX, int trackY, bool hasLock)
        {
            if (Application.Current != null) Application.Current.Dispatcher.InvokeAsync(() => _reticle.UpdateCorridorState(trackX, trackY, hasLock));
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
