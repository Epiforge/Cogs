using Cogs.Disposal;
using Microsoft.Win32;
using System;
using System.Drawing;
using System.Management;
using System.Security.Principal;
using System.Threading;

namespace Cogs.Windows
{
    public class Theme : SyncDisposable
    {
        public Theme()
        {
            synchronizationContext = SynchronizationContext.Current;

            colorKey = colorHive.OpenSubKey(colorKeyName);
            color = FetchColor();
            try
            {
                colorKeyWatcher = new ManagementEventWatcher(new WqlEventQuery("RegistryValueChangeEvent") { Condition = $"Hive = '{Sanitize(colorHive.Name)}' AND KeyPath = '{Sanitize(colorKeyName)}' AND ValueName = '{Sanitize(colorValueName)}'" });
                colorKeyWatcher.EventArrived += ColorKeyWatcherEventArrived;
                colorKeyWatcher.Start();
            }
            catch (ManagementException)
            {
                // Oh, I see how it is
            }

            isDarkKey = isDarkHive.OpenSubKey(isDarkKeyName);
            isDark = FetchIsDark();
            try
            {
                isDarkKeyWatcher = new ManagementEventWatcher(new WqlEventQuery("RegistryValueChangeEvent") { Condition = $"Hive = '{Sanitize(isDarkHive.Name)}' AND KeyPath = '{Sanitize(isDarkKeyName)}' AND ValueName = '{Sanitize(isDarkValueName)}'" });
                isDarkKeyWatcher.EventArrived += IsDarkKeyWatcherEventArrived;
                isDarkKeyWatcher.Start();
            }
            catch (ManagementException)
            {
                // Fine then
            }
        }

        protected readonly RegistryKey colorHive = Registry.Users;
        private Color color;
        protected RegistryKey colorKey;
        protected readonly string colorKeyName = $@"{WindowsIdentity.GetCurrent().User}\Software\Microsoft\Windows\DWM";
        protected ManagementEventWatcher? colorKeyWatcher;
        protected readonly string colorValueName = "ColorizationColor";
        protected readonly int defaultColorValue = unchecked((int)0xc42947cc);
        protected readonly int defaultIsDarkValue = 1;
        protected readonly RegistryKey isDarkHive = Registry.Users;
        bool isDark;
        protected RegistryKey isDarkKey;
        protected ManagementEventWatcher? isDarkKeyWatcher;
        protected readonly string isDarkKeyName = $@"{WindowsIdentity.GetCurrent().User}\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
        protected readonly string isDarkValueName = "AppsUseLightTheme";
        protected SynchronizationContext synchronizationContext;

        protected static string Sanitize(string value) => value.Replace(@"\", @"\\").Replace("'", @"\'");

        public Color Color
        {
            get => color;
            protected set => SetBackedProperty(ref color, in value);
        }

        public bool IsDark
        {
            get => isDark;
            protected set => SetBackedProperty(ref isDark, in value);
        }

        void ColorKeyWatcherEventArrived(object sender, EventArrivedEventArgs e) => UsingContext(() => Color = FetchColor());

        protected override bool Dispose(bool disposing)
        {
            if (disposing)
            {
                colorKey?.Dispose();
                isDarkKey?.Dispose();
            }
            return true;
        }

        protected Color FetchColor() => Color.FromArgb((int)(colorKey?.GetValue(colorValueName) ?? defaultColorValue));

        protected bool FetchIsDark() => (int)(isDarkKey?.GetValue(isDarkValueName) ?? defaultIsDarkValue) == 0;

        void IsDarkKeyWatcherEventArrived(object sender, EventArrivedEventArgs e) => UsingContext(() => IsDark = FetchIsDark());

        protected void UsingContext(Action action)
        {
            if (synchronizationContext != null)
                synchronizationContext.Post(state => action(), null);
            else
                action();
        }
    }
}
