using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;

using ZXing;
using ZXing.Common;
using ZXing.QrCode;

using Shadowsocks.Controller;
using Shadowsocks.Model;
using Shadowsocks.Properties;
using Shadowsocks.Util;
using System.Linq;

namespace Shadowsocks.View
{
    public class MenuViewController
    {
        // yes this is just a menu view controller
        // when config form is closed, it moves away from RAM
        // and it should just do anything related to the config form

        private ShadowsocksController controller;

        private NotifyIcon _notifyIcon;
        private Bitmap icon_baseBitmap;
        private Icon icon_base, icon_in, icon_out, icon_both, targetIcon;
        private ContextMenu contextMenu1;

        private bool _isFirstRun;
        private MenuItem AutoStartupItem;
        private MenuItem SeperatorItem;
        private MenuItem ConfigItem;
        private MenuItem ServersItem;
        private ConfigForm configForm;

        public MenuViewController(ShadowsocksController controller)
        {
            this.controller = controller;

            LoadMenu();
            controller.Errored += controller_Errored;

            _notifyIcon = new NotifyIcon();
            UpdateTrayIcon();
            _notifyIcon.Visible = true;
            _notifyIcon.ContextMenu = contextMenu1;
            _notifyIcon.BalloonTipClicked += notifyIcon1_BalloonTipClicked;
            _notifyIcon.MouseClick += notifyIcon1_Click;
            _notifyIcon.MouseDoubleClick += notifyIcon1_DoubleClick;
            _notifyIcon.BalloonTipClosed += _notifyIcon_BalloonTipClosed;

            LoadCurrentConfiguration();

            Configuration config = controller.GetConfigurationCopy();

            //if (config.isDefault)
            //{
            //    _isFirstRun = true;
            //    ShowConfigForm();
            //}
        }
        void controller_Errored(object sender, System.IO.ErrorEventArgs e)
        {
            MessageBox.Show(e.GetException().ToString(), String.Format(I18N.GetString("Shadowsocks Error: {0}"), e.GetException().Message));
        }

        #region Tray Icon

        private void UpdateTrayIcon()
        {
            int dpi;
            Graphics graphics = Graphics.FromHwnd(IntPtr.Zero);
            dpi = (int)graphics.DpiX;
            graphics.Dispose();
            icon_baseBitmap = null;
            if (dpi < 97)
            {
                // dpi = 96;
                icon_baseBitmap = Resources.ss16;
            }
            else if (dpi < 121)
            {
                // dpi = 120;
                icon_baseBitmap = Resources.ss20;
            }
            else
            {
                icon_baseBitmap = Resources.ss24;
            }
            Configuration config = controller.GetConfigurationCopy();
            //icon_baseBitmap = getTrayIconByState(icon_baseBitmap, enabled, global);

            icon_base = Icon.FromHandle(icon_baseBitmap.GetHicon());
            targetIcon = icon_base;
            icon_in = Icon.FromHandle(AddBitmapOverlay(icon_baseBitmap, Resources.ssIn24).GetHicon());
            icon_out = Icon.FromHandle(AddBitmapOverlay(icon_baseBitmap, Resources.ssOut24).GetHicon());
            icon_both = Icon.FromHandle(AddBitmapOverlay(icon_baseBitmap, Resources.ssIn24, Resources.ssOut24).GetHicon());
            _notifyIcon.Icon = targetIcon;
        }

        private Bitmap getTrayIconByState(Bitmap originIcon, bool enabled, bool global)
        {
            Bitmap iconCopy = new Bitmap(originIcon);
            for (int x = 0; x < iconCopy.Width; x++)
            {
                for (int y = 0; y < iconCopy.Height; y++)
                {
                    Color color = originIcon.GetPixel(x, y);
                    if (color.A != 0)
                    {
                        if (!enabled)
                        {
                            Color flyBlue = Color.FromArgb(192, 192, 192);
                            // Multiply with flyBlue
                            int red = color.R * flyBlue.R / 255;
                            int green = color.G * flyBlue.G / 255;
                            int blue = color.B * flyBlue.B / 255;
                            iconCopy.SetPixel(x, y, Color.FromArgb(color.A, red, green, blue));
                        }
                        else if (global)
                        {
                            Color flyBlue = Color.FromArgb(25, 125, 191);
                            // Multiply with flyBlue
                            int red   = color.R * flyBlue.R / 255;
                            int green = color.G * flyBlue.G / 255; 
                            int blue  = color.B * flyBlue.B / 255;
                            iconCopy.SetPixel(x, y, Color.FromArgb(color.A, red, green, blue));
                        }
                    }
                    else
                    {
                        iconCopy.SetPixel(x, y, Color.FromArgb(color.A, color.R, color.G, color.B));
                    }
                }
            }
            return iconCopy;
        }

        private Bitmap AddBitmapOverlay(Bitmap original, params Bitmap[] overlays)
        {
            Bitmap bitmap = new Bitmap(original.Width, original.Height, PixelFormat.Format64bppArgb);
            Graphics canvas = Graphics.FromImage(bitmap);
            canvas.DrawImage(original, new Point(0, 0));
            foreach (Bitmap overlay in overlays)
            {
                canvas.DrawImage(new Bitmap(overlay, original.Size), new Point(0, 0));
            }
            canvas.Save();
            return bitmap;
        }

        #endregion

        #region MenuItems and MenuGroups

        private MenuItem CreateMenuItem(string text, EventHandler click)
        {
            return new MenuItem(I18N.GetString(text), click);
        }

        private MenuItem CreateMenuGroup(string text, MenuItem[] items)
        {
            return new MenuItem(I18N.GetString(text), items);
        }

        private void LoadMenu()
        {
            this.contextMenu1 = new ContextMenu(new MenuItem[] {
                this.ServersItem = CreateMenuGroup("Servers", new MenuItem[] {
                    this.SeperatorItem = new MenuItem("-"),
                    this.ConfigItem = CreateMenuItem("Edit Servers...", new EventHandler(this.Config_Click)),
                    new MenuItem("-"),
                }),
                this.AutoStartupItem = CreateMenuItem("Start on Boot", new EventHandler(this.AutoStartupItem_Click)),
                CreateMenuGroup("Help", new MenuItem[] {
                    CreateMenuItem("About...", new EventHandler(this.AboutItem_Click)),
                }),
                new MenuItem("-"),
                CreateMenuItem("Quit", new EventHandler(this.Quit_Click))
            });
        }

        #endregion

        private void controller_ConfigChanged(object sender, EventArgs e)
        {
            LoadCurrentConfiguration();
            UpdateTrayIcon();
        }
        void controller_FileReadyToOpen(object sender, ShadowsocksController.PathEventArgs e)
        {
            string argument = @"/select, " + e.Path;

            System.Diagnostics.Process.Start("explorer.exe", argument);
        }

        void ShowBalloonTip(string title, string content, ToolTipIcon icon, int timeout)
        {
            _notifyIcon.BalloonTipTitle = title;
            _notifyIcon.BalloonTipText = content;
            _notifyIcon.BalloonTipIcon = icon;
            _notifyIcon.ShowBalloonTip(timeout);
        }
        void notifyIcon1_BalloonTipClicked(object sender, EventArgs e)
        {
        }

        private void _notifyIcon_BalloonTipClosed(object sender, EventArgs e)
        {
        }

        private void LoadCurrentConfiguration()
        {
            Configuration config = controller.GetConfigurationCopy();
            UpdateServersMenu();
            AutoStartupItem.Checked = AutoStartup.Check();
            //UpdateUpdateMenu();
        }

        private void UpdateServersMenu()
        {
            var items = ServersItem.MenuItems;
            while (items[0] != SeperatorItem)
            {
                items.RemoveAt(0);
            }
            int i = 0;
            //foreach (var strategy in controller.GetStrategies())
            //{
            //    MenuItem item = new MenuItem(strategy.Name);
            //    item.Tag = strategy.ID;
            //    item.Click += AStrategyItem_Click;
            //    items.Add(i, item);
            //    i++;
            //}

            // user wants a seperator item between strategy and servers menugroup
            items.Add( i++, new MenuItem("-") );

            int strategyCount = i;
            Configuration configuration = controller.GetConfigurationCopy();
            foreach (var server in configuration.configs)
            {
                MenuItem item = new MenuItem(server.server);
                item.Tag = i - strategyCount;
                item.Click += AServerItem_Click;
                items.Add(i, item);
                i++;
            }

            foreach (MenuItem item in items)
            {
                if (item.Tag != null && (item.Tag.ToString() == configuration.index.ToString()))
                {
                    item.Checked = true;
                }
            }
        }

        private void ShowConfigForm()
        {
            if (configForm != null)
            {
                configForm.Activate();
            }
            else
            {
                configForm = new ConfigForm(controller);
                configForm.Show();
                configForm.Activate();
                configForm.FormClosed += configForm_FormClosed;
            }
        }
        void configForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            configForm.Dispose();
            configForm = null;
            Utils.ReleaseMemory(true);
            if (_isFirstRun)
            {
                ShowFirstTimeBalloon();
                _isFirstRun = false;
            }
        }
        private void Config_Click(object sender, EventArgs e)
        {
            ShowConfigForm();
        }

        private void Quit_Click(object sender, EventArgs e)
        {
            controller.Stop();
            _notifyIcon.Visible = false;
            Application.Exit();
        }
        private void ShowFirstTimeBalloon()
        {
            _notifyIcon.BalloonTipTitle = I18N.GetString("Shadowsocks is here");
            _notifyIcon.BalloonTipText = I18N.GetString("You can turn on/off Shadowsocks in the context menu");
            _notifyIcon.BalloonTipIcon = ToolTipIcon.Info;
            _notifyIcon.ShowBalloonTip(0);
        }

        private void AboutItem_Click(object sender, EventArgs e)
        {
            Process.Start("https://github.com/shadowsocks/shadowsocks-windows");
        }

        private void notifyIcon1_Click(object sender, MouseEventArgs e)
        {
            if ( e.Button == MouseButtons.Middle )
            {
            }
        }

        private void notifyIcon1_DoubleClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ShowConfigForm();
            }
        }

        private void EnableItem_Click(object sender, EventArgs e)
        {
           // controller.ToggleEnable(!enableItem.Checked);
        }
        private void AServerItem_Click(object sender, EventArgs e)
        {
            MenuItem item = (MenuItem)sender;
            controller.SelectServerIndex((int)item.Tag);
        }

        void splash_FormClosed(object sender, FormClosedEventArgs e)
        {
            ShowConfigForm();
        }
        private void AutoStartupItem_Click(object sender, EventArgs e)
        {
            AutoStartupItem.Checked = !AutoStartupItem.Checked;
            if (!AutoStartup.Set(AutoStartupItem.Checked))
            {
                MessageBox.Show(I18N.GetString("Failed to update registry"));
            }
        }
    }
}
