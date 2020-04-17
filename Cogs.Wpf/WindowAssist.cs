using System;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace Cogs.Wpf
{
    public static class WindowAssist
    {
        public static readonly DependencyProperty AutoActivationProperty = DependencyProperty.RegisterAttached("AutoActivation", typeof(AutoActivationMode), typeof(WindowAssist), new PropertyMetadata(AutoActivationMode.Default, AutoActivationChanged));
        static readonly ConcurrentDictionary<Window, BlurBehindMode> blurBehindPendingLoadByWindow = new ConcurrentDictionary<Window, BlurBehindMode>();
        public static readonly DependencyProperty BlurBehindProperty = DependencyProperty.RegisterAttached("BlurBehind", typeof(BlurBehindMode), typeof(WindowAssist), new PropertyMetadata(BlurBehindMode.Off, BlurBehindChanged));
        static readonly DependencyPropertyKey isBlurredBehindKey = DependencyProperty.RegisterAttachedReadOnly("IsBlurredBehind", typeof(bool), typeof(WindowAssist), new PropertyMetadata(false));
        public static readonly DependencyProperty IsBlurredBehindProperty = isBlurredBehindKey.DependencyProperty;

        static void ActivateWindowContentRenderedHandler(object sender, EventArgs e)
        {
            if (sender is Window window)
            {
                window.Activate();
                window.ContentRendered -= ActivateWindowContentRenderedHandler;
            }
        }

        static void AutoActivationChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is Window window && !window.IsLoaded && e.NewValue is AutoActivationMode newValue)
            {
                window.ContentRendered -= ActivateWindowContentRenderedHandler;
                switch (newValue)
                {
                    case AutoActivationMode.Default:
                        break;
                    case AutoActivationMode.OnContentRendered:
                        window.ContentRendered += ActivateWindowContentRenderedHandler;
                        break;
                    default:
                        throw new NotSupportedException();
                }
            }
        }

        static void BlurBehindChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is Window window && e.NewValue is BlurBehindMode newValue)
            {
                if (window.IsLoaded)
                    EffectBlurBehindMode(window, newValue);
                else
                {
                    blurBehindPendingLoadByWindow.AddOrUpdate(window, newValue, (key, value) => newValue);
                    window.Loaded += PendingBlurBehindWindowLoadedHandler;
                }
            }
        }

        static void EffectBlurBehind(Window window, bool blurBehind)
        {
            if (blurBehind && !SystemParameters.HighContrast)
            {
                window.SetValue(isBlurredBehindKey, SetAccentPolicy(window, NativeInterop.Types.AccentState.ACCENT_ENABLE_BLURBEHIND));
                return;
            }
            SetAccentPolicy(window, NativeInterop.Types.AccentState.ACCENT_DISABLED);
            window.SetValue(isBlurredBehindKey, false);
        }

        static void EffectBlurBehindMode(Window window, BlurBehindMode mode)
        {
            window.Activated -= EffectBlurBehindOff;
            window.Activated -= EffectBlurBehindOn;
            window.Deactivated -= EffectBlurBehindOff;
            window.Deactivated -= EffectBlurBehindOn;
            switch (mode)
            {
                case BlurBehindMode.Off:
                    EffectBlurBehind(window, false);
                    break;
                case BlurBehindMode.On:
                    EffectBlurBehind(window, true);
                    break;
                case BlurBehindMode.OnActivated:
                    window.Activated += EffectBlurBehindOn;
                    window.Deactivated += EffectBlurBehindOff;
                    EffectBlurBehind(window, window.IsActive);
                    break;
                case BlurBehindMode.OnDeactivated:
                    window.Deactivated += EffectBlurBehindOn;
                    window.Activated += EffectBlurBehindOff;
                    EffectBlurBehind(window, !window.IsActive);
                    break;
            }
        }

        static void EffectBlurBehindOff(object sender, EventArgs e)
        {
            if (sender is Window window)
                EffectBlurBehind(window, false);
        }

        static void EffectBlurBehindOn(object sender, EventArgs e)
        {
            if (sender is Window window)
                EffectBlurBehind(window, true);
        }

        static NativeInterop.Types.AccentFlags GetAccentFlagsForTaskbarPosition() => NativeInterop.Types.AccentFlags.DrawAllBorders;

        public static AutoActivationMode GetAutoActivation(Window window) => (AutoActivationMode)window.GetValue(AutoActivationProperty);

        public static BlurBehindMode GetBlurBehind(Window window) => (BlurBehindMode)window.GetValue(BlurBehindProperty);

        public static bool GetIsBlurredBehind(Window window) => (bool)window.GetValue(IsBlurredBehindProperty);

        static void PendingBlurBehindWindowLoadedHandler(object sender, RoutedEventArgs e)
        {
            if (sender is Window window)
            {
                window.Loaded -= PendingBlurBehindWindowLoadedHandler;
                if (blurBehindPendingLoadByWindow.TryRemove(window, out var mode))
                    EffectBlurBehindMode(window, mode);
            }
        }

        static bool SetAccentPolicy(Window window, NativeInterop.Types.AccentState accentState)
        {
            var windowHelper = new WindowInteropHelper(window);
            var accent = new NativeInterop.Types.AccentPolicy
            {
                AccentState = accentState,
                AccentFlags = GetAccentFlagsForTaskbarPosition(),
                AnimationId = 3
            };
            var accentStructSize = Marshal.SizeOf(accent);
            var accentPtr = Marshal.AllocHGlobal(accentStructSize);
            Marshal.StructureToPtr(accent, accentPtr, false);
            var data = new NativeInterop.Types.WindowCompositionAttribData
            {
                Attribute = NativeInterop.Types.WindowCompositionAttribute.WCA_ACCENT_POLICY,
                SizeOfData = accentStructSize,
                Data = accentPtr
            };
            var result = NativeInterop.Methods.SetWindowCompositionAttribute(windowHelper.Handle, ref data);
            Marshal.FreeHGlobal(accentPtr);
            return result;
        }

        public static void SetAutoActivation(Window window, AutoActivationMode value) => window.SetValue(AutoActivationProperty, value);

        public static void SetBlurBehind(Window window, BlurBehindMode value) => window.SetValue(BlurBehindProperty, value);
    }
}
