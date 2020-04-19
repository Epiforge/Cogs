using Cogs.Wpf.NativeInterop;
using Cogs.Wpf.NativeInterop.Types;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;

namespace Cogs.Wpf
{
    public static class WindowAssist
    {
        #region AutoActivation

        public static readonly DependencyProperty AutoActivationProperty = DependencyProperty.RegisterAttached("AutoActivation", typeof(AutoActivationMode), typeof(WindowAssist), new PropertyMetadata(AutoActivationMode.Default, AutoActivationChanged));

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

        public static AutoActivationMode GetAutoActivation(Window window) => (AutoActivationMode)window.GetValue(AutoActivationProperty);

        public static void SetAutoActivation(Window window, AutoActivationMode value) => window.SetValue(AutoActivationProperty, value);

        #endregion AutoActivation

        #region BlurBehind

        static readonly ConcurrentDictionary<Window, BlurBehindMode> blurBehindPendingLoadByWindow = new ConcurrentDictionary<Window, BlurBehindMode>();
        public static readonly DependencyProperty BlurBehindProperty = DependencyProperty.RegisterAttached("BlurBehind", typeof(BlurBehindMode), typeof(WindowAssist), new PropertyMetadata(BlurBehindMode.Off, BlurBehindChanged));

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
                window.SetValue(isBlurredBehindKey, SetAccentPolicy(window, AccentState.ACCENT_ENABLE_BLURBEHIND));
                return;
            }
            SetAccentPolicy(window, AccentState.ACCENT_DISABLED);
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

        static AccentFlags GetAccentFlagsForTaskbarPosition() => AccentFlags.DrawAllBorders;

        public static BlurBehindMode GetBlurBehind(Window window) => (BlurBehindMode)window.GetValue(BlurBehindProperty);

        static void PendingBlurBehindWindowLoadedHandler(object sender, RoutedEventArgs e)
        {
            if (sender is Window window)
            {
                window.Loaded -= PendingBlurBehindWindowLoadedHandler;
                if (blurBehindPendingLoadByWindow.TryRemove(window, out var mode))
                    EffectBlurBehindMode(window, mode);
            }
        }

        static bool SetAccentPolicy(Window window, AccentState accentState)
        {
            var windowHelper = new WindowInteropHelper(window);
            var accent = new AccentPolicy
            {
                AccentState = accentState,
                AccentFlags = GetAccentFlagsForTaskbarPosition(),
                AnimationId = 3
            };
            var accentStructSize = Marshal.SizeOf(accent);
            var accentPtr = Marshal.AllocHGlobal(accentStructSize);
            Marshal.StructureToPtr(accent, accentPtr, false);
            var data = new WindowCompositionAttribData
            {
                Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY,
                SizeOfData = accentStructSize,
                Data = accentPtr
            };
            var result = Methods.SetWindowCompositionAttribute(windowHelper.Handle, ref data);
            Marshal.FreeHGlobal(accentPtr);
            return result;
        }

        public static void SetBlurBehind(Window window, BlurBehindMode value) => window.SetValue(BlurBehindProperty, value);

        #endregion BlurBehind

        #region IsBlurredBehind

        static readonly DependencyPropertyKey isBlurredBehindKey = DependencyProperty.RegisterAttachedReadOnly("IsBlurredBehind", typeof(bool), typeof(WindowAssist), new PropertyMetadata(false));
        public static readonly DependencyProperty IsBlurredBehindProperty = isBlurredBehindKey.DependencyProperty;

        public static bool GetIsBlurredBehind(Window window) => (bool)window.GetValue(IsBlurredBehindProperty);

        #endregion IsBlurredBehind

        #region IsCaption

        public static readonly DependencyProperty IsCaptionProperty = DependencyProperty.RegisterAttached("IsCaption", typeof(bool), typeof(WindowAssist), new PropertyMetadata(false, IsCaptionChanged));

        public static bool GetIsCaption(FrameworkElement frameworkElement) => (bool)frameworkElement.GetValue(IsCaptionProperty);

        static void IsCaptionChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is FrameworkElement frameworkElement && e.NewValue is bool newValue)
            {
                if (newValue)
                    frameworkElement.MouseLeftButtonDown += IsCaptionMouseLeftButtonDownHandler;
                else
                    frameworkElement.MouseLeftButtonDown -= IsCaptionMouseLeftButtonDownHandler;
            }
        }

        static void IsCaptionMouseLeftButtonDownHandler(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement frameworkElement)
            {
                var window = Window.GetWindow(frameworkElement);
                if (e.ClickCount == 2)
                {
                    if (window.WindowState == WindowState.Maximized)
                        SendSystemCommand(frameworkElement, SystemCommand.Restore);
                    else
                        SendSystemCommand(frameworkElement, SystemCommand.Maximize);
                }
                else
                    window.DragMove();
            }
        }

        public static void SetIsCaption(FrameworkElement frameworkElement, bool value) => frameworkElement.SetValue(IsCaptionProperty, value);

        #endregion IsCaption

        #region SendSystemCommand

        public static readonly DependencyProperty SendSystemCommandProperty = DependencyProperty.RegisterAttached("SendSystemCommand", typeof(SystemCommand), typeof(WindowAssist), new PropertyMetadata(SystemCommand.None, SendSystemCommandChanged));

        public static SystemCommand GetSendSystemCommand(FrameworkElement frameworkElement) => (SystemCommand)frameworkElement.GetValue(SendSystemCommandProperty);

        static void SendSystemCommandChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is SystemCommand systemCommand)
            {
                if (sender is ButtonBase buttonBase)
                {
                    if (systemCommand != SystemCommand.None)
                        buttonBase.Click += SendSystemCommandClickHandler;
                    else
                        buttonBase.Click -= SendSystemCommandClickHandler;
                }
                else if (sender is FrameworkElement frameworkElement)
                {
                    if (systemCommand != SystemCommand.None)
                    {
                        frameworkElement.MouseLeftButtonDown += SendSystemCommandMouseButtonDownHandler;
                        frameworkElement.MouseRightButtonDown += SendSystemCommandMouseButtonDownHandler;
                    }
                    else
                    {
                        frameworkElement.MouseLeftButtonDown -= SendSystemCommandMouseButtonDownHandler;
                        frameworkElement.MouseRightButtonDown -= SendSystemCommandMouseButtonDownHandler;
                    }
                }
            }
        }

        static void SendSystemCommandClickHandler(object sender, RoutedEventArgs e)
        {
            if (sender is ButtonBase buttonBase)
                SendSystemCommand(buttonBase);
        }

        static void SendSystemCommandMouseButtonDownHandler(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 1 && sender is FrameworkElement frameworkElement)
                SendSystemCommand(frameworkElement);
        }

        static void SendSystemCommand(FrameworkElement frameworkElement, SystemCommand? systemCommand = null) =>
            Methods.SendMessage(new WindowInteropHelper(Window.GetWindow(frameworkElement)).Handle, WindowMessage.SYSCOMMAND, new IntPtr((int)((systemCommand ?? GetSendSystemCommand(frameworkElement)) switch
            {
                SystemCommand.Maximize => NativeInterop.Types.SystemCommand.MAXIMIZE,
                SystemCommand.Minimize => NativeInterop.Types.SystemCommand.MINIMIZE,
                SystemCommand.Restore => NativeInterop.Types.SystemCommand.RESTORE,
                _ => throw new NotSupportedException()
            })), IntPtr.Zero);

        public static void SetSendSystemCommand(FrameworkElement frameworkElement, SystemCommand systemCommand) => frameworkElement.SetValue(ShowSystemMenuProperty, systemCommand);

        #endregion SendSystemCommand

        #region SetDefaultWindowStyleOnSystemCommands

        static readonly ConcurrentDictionary<Window, bool> setDefaultWindowStyleOnSystemCommandsPendingLoadByWindow = new ConcurrentDictionary<Window, bool>();
        public static readonly DependencyProperty SetDefaultWindowStyleOnSystemCommandsProperty = DependencyProperty.RegisterAttached("SetDefaultWindowStyleOnSystemCommands", typeof(bool), typeof(WindowAssist), new PropertyMetadata(false, SetDefaultWindowStyleOnSystemCommandsChanged));

        public static bool GetSetDefaultWindowStyleOnSystemCommands(Window window) => (bool)window.GetValue(SetDefaultWindowStyleOnSystemCommandsProperty);

        static void EffectSetDefaultWindowStyleOnSystemCommands(Window window, bool setDefaultWindowStyleOnSystemCommands)
        {
            var hwndSource = HwndSource.FromHwnd(new WindowInteropHelper(window).Handle);
            if (setDefaultWindowStyleOnSystemCommands)
                hwndSource.AddHook(SetDefaultWindowStyleOnSystemCommandsHook);
            else
                hwndSource.RemoveHook(SetDefaultWindowStyleOnSystemCommandsHook);
        }

        static void PendingSetDefaultWindowStyleOnSystemCommandsWindowLoadedHandler(object sender, RoutedEventArgs e)
        {
            if (sender is Window window)
            {
                window.Loaded -= PendingSetDefaultWindowStyleOnSystemCommandsWindowLoadedHandler;
                if (setDefaultWindowStyleOnSystemCommandsPendingLoadByWindow.TryRemove(window, out var setDefaultWindowStyleOnSystemCommands))
                    EffectSetDefaultWindowStyleOnSystemCommands(window, setDefaultWindowStyleOnSystemCommands);
            }
        }

        static void SetDefaultWindowStyleOnSystemCommandsChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is Window window && e.NewValue is bool newValue)
            {
                if (window.IsLoaded)
                    EffectSetDefaultWindowStyleOnSystemCommands(window, newValue);
                else
                {
                    setDefaultWindowStyleOnSystemCommandsPendingLoadByWindow.AddOrUpdate(window, newValue, (key, value) => newValue);
                    window.Loaded += PendingSetDefaultWindowStyleOnSystemCommandsWindowLoadedHandler;
                }
            }
        }

        static IntPtr SetDefaultWindowStyleOnSystemCommandsHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            var message = (WindowMessage)msg;
            if (HwndSource.FromHwnd(hwnd).RootVisual is Window window && message == WindowMessage.SYSCOMMAND)
            {
                var systemCommand = (NativeInterop.Types.SystemCommand)wParam.ToInt32();
                if (new NativeInterop.Types.SystemCommand[] { NativeInterop.Types.SystemCommand.MAXIMIZE, NativeInterop.Types.SystemCommand.MINIMIZE, NativeInterop.Types.SystemCommand.RESTORE }.Contains(systemCommand))
                {
                    var currentWindowStyle = window.WindowStyle;
                    window.WindowStyle = WindowStyle.SingleBorderWindow;
                    window.WindowState = systemCommand switch
                    {
                        NativeInterop.Types.SystemCommand.MAXIMIZE => WindowState.Maximized,
                        NativeInterop.Types.SystemCommand.MINIMIZE => WindowState.Minimized,
                        NativeInterop.Types.SystemCommand.RESTORE => WindowState.Normal,
                        _ => throw new NotSupportedException()
                    };
                    window.WindowStyle = currentWindowStyle;
                    handled = true;
                }
            }
            return IntPtr.Zero;
        }

        public static void SetSetDefaultWindowStyleOnSystemCommands(Window window, bool value) => window.SetValue(SetDefaultWindowStyleOnSystemCommandsProperty, value);

        #endregion SetDefaultWindowStyleOnSystemCommands

        #region ShowSystemMenu

        public static readonly DependencyProperty ShowSystemMenuProperty = DependencyProperty.RegisterAttached("ShowSystemMenu", typeof(bool), typeof(WindowAssist), new PropertyMetadata(false, ShowSystemMenuChanged));

        public static bool GetShowSystemMenu(FrameworkElement frameworkElement) => (bool)frameworkElement.GetValue(ShowSystemMenuProperty);

        public static void SetShowSystemMenu(FrameworkElement frameworkElement, bool value) => frameworkElement.SetValue(ShowSystemMenuProperty, value);

        static void ShowSystemMenu(FrameworkElement frameworkElement)
        {
            var window = Window.GetWindow(frameworkElement);
            var frameworkElementTopLeft = frameworkElement.PointToScreen(new Point(0, 0));
            var frameworkElementBottomRight = frameworkElement.PointToScreen(new Point(frameworkElement.ActualWidth, frameworkElement.ActualHeight));
            var hWnd = new WindowInteropHelper(window).Handle;
            var hMenu = Methods.GetSystemMenu(hWnd, false);
            Methods.EnableMenuItem(hMenu, NativeInterop.Types.SystemCommand.MAXIMIZE, window.WindowState == WindowState.Maximized ? MenuStatus.GRAYED : MenuStatus.ENABLED);
            var command = Methods.TrackPopupMenuEx(hMenu, TrackPopupMenuFlags.LEFTALIGN | TrackPopupMenuFlags.RETURNCMD, (int)frameworkElementTopLeft.X, (int)frameworkElementBottomRight.Y, hWnd, IntPtr.Zero);
            if (command != 0)
                Methods.PostMessage(hWnd, WindowMessage.SYSCOMMAND, new IntPtr(command), IntPtr.Zero);
        }

        static void ShowSystemMenuChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is bool boolean)
            {
                if (sender is ButtonBase buttonBase)
                {
                    if (boolean)
                        buttonBase.Click += ShowSystemMenuClickHandler;
                    else
                        buttonBase.Click -= ShowSystemMenuClickHandler;
                }
                else if (sender is FrameworkElement frameworkElement)
                {
                    if (boolean)
                    {
                        frameworkElement.MouseLeftButtonDown += ShowSystemMenuMouseButtonDownHandler;
                        frameworkElement.MouseRightButtonDown += ShowSystemMenuMouseButtonDownHandler;
                    }
                    else
                    {
                        frameworkElement.MouseLeftButtonDown -= ShowSystemMenuMouseButtonDownHandler;
                        frameworkElement.MouseRightButtonDown -= ShowSystemMenuMouseButtonDownHandler;
                    }
                }
            }
        }

        static void ShowSystemMenuClickHandler(object sender, RoutedEventArgs e)
        {
            if (sender is ButtonBase buttonBase)
                ShowSystemMenu(buttonBase);
        }

        static void ShowSystemMenuMouseButtonDownHandler(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 1 && sender is FrameworkElement frameworkElement)
                ShowSystemMenu(frameworkElement);
        }

        #endregion ShowSystemMenu
    }
}
