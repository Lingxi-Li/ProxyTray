using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Management;
using System.Security.Principal;
using System.Windows.Forms;

namespace ProxyTray
{
    internal static class Program
    {
        const string KeyPath = @"Software\Microsoft\Windows\CurrentVersion\Internet Settings";
        static readonly string StatusChangeEventQuery = (
            "SELECT * FROM RegistryValueChangeEvent WHERE " +
            $"Hive = 'HKEY_USERS' AND KeyPath = '{WindowsIdentity.GetCurrent().User.Value}\\{KeyPath}' " +
            "AND (ValueName = 'ProxyEnable' or ValueName = 'ProxyServer')"
            ).Replace(@"\", @"\\");
        const string None = "<None>";

        static Icon ProxyOnIcon;
        static Icon ProxyOffIcon;
        static NotifyIcon Tray;

        static bool ProxyEnable;
        static string ProxyServer;

        static void Initialize()
        {
            ProxyOnIcon = new Icon("ProxyOn.ico");
            ProxyOffIcon = new Icon("ProxyOff.ico");

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
        }

        static void CreateTray()
        {
            var settingItem = new MenuItem
            {
                Index = 0,
                Text = "Proxy Setting"
            };
            settingItem.Click += (sender, e) => Process.Start("ms-settings:network-proxy");
            var menuItem = new MenuItem
            {
                Index = 1,
                Text = "Quit"
            };
            menuItem.Click += (sender, e) => Application.Exit();
            Tray = new NotifyIcon
            {
                ContextMenu = new ContextMenu(new[] { settingItem, menuItem }),
                Visible = true
            };
            Tray.MouseClick += (sender, e) =>
            {
                if (e.Button != MouseButtons.Left) return;
                if (ProxyServer == None)
                {
                    MessageBox.Show("'ProxyServer' not configured");
                    return;
                }
                using (var key = Registry.CurrentUser.OpenSubKey(KeyPath, true))
                {
                    key.SetValue("ProxyEnable", ProxyEnable ? 0 : 1);
                }
            };
        }

        static void RefreshTray()
        {
            using (var key = Registry.CurrentUser.OpenSubKey(KeyPath))
            {
                ProxyEnable = (int)(key.GetValue("ProxyEnable", 0)) != 0;
                ProxyServer = (string)(key.GetValue("ProxyServer", None));
            }
            Tray.Icon = ProxyEnable ? ProxyOnIcon : ProxyOffIcon;
            Tray.Text = ProxyEnable ? ProxyServer : "Direct";
        }

        [STAThread]
        static void Main()
        {
            try
            {
                Initialize();
                CreateTray();
                RefreshTray();

                using (var watcher = new ManagementEventWatcher(StatusChangeEventQuery))
                {
                    watcher.EventArrived += (sender, e) => RefreshTray();
                    watcher.Start();
                    Application.Run();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }
    }
}
