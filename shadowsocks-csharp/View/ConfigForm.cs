using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using Microsoft.Win32;
using Shadowsocks.Controller;
using Shadowsocks.Model;
using Shadowsocks.Properties;

namespace Shadowsocks.View
{
    public partial class ConfigForm : Form
    {
        private ShadowsocksController controller;

        // this is a copy of configuration that we are working on
        private Configuration _modifiedConfiguration;
        private int _lastSelectedIndex = -1;

        public ConfigForm(ShadowsocksController controller)
        {
            this.Font = System.Drawing.SystemFonts.MessageBoxFont;
            InitializeComponent();

            // a dirty hack
            this.ServersListBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.PerformLayout();

            UpdateTexts();
            this.Icon = Icon.FromHandle(Resources.ssw128.GetHicon());

            this.controller = controller;
            //controller.ConfigChanged += controller_ConfigChanged;
            LoadCurrentConfiguration();
        }

        private void UpdateTexts()
        {
            AddButton.Text = I18N.GetString("&Add");
            DeleteButton.Text = I18N.GetString("&Delete");
            IPLabel.Text = I18N.GetString("Server Addr");
            ServerGroupBox.Text = I18N.GetString("Server");
            //OKButton.Text = I18N.GetString("OK");
            //MyCancelButton.Text = I18N.GetString("Cancel");
            this.Text = I18N.GetString("Edit Servers");
        }

        private void controller_ConfigChanged(object sender, EventArgs e)
        {
            LoadCurrentConfiguration();
        }

        private void ShowWindow()
        {
            this.Opacity = 1;
            this.Show();
            IPTextBox.Focus();
        }
        private void LoadSelectedServer()
        {
            if (ServersListBox.SelectedIndex >= 0 && ServersListBox.SelectedIndex < _modifiedConfiguration.configs.Count)
            {
                Server server = _modifiedConfiguration.configs[ServersListBox.SelectedIndex];

                IPTextBox.Text = server.server;
            }
        }

        private void LoadConfiguration(Configuration configuration)
        {
            ServersListBox.Items.Clear();
            foreach (Server server in _modifiedConfiguration.configs)
            {
                ServersListBox.Items.Add(server.server);
            }
            _lastSelectedIndex = 0;
            if (_modifiedConfiguration.configs.Count <= 0)
            {
                MessageBox.Show(I18N.GetString("Please add at least one server"));
            }
            else
            {
                IPTextBox.Text = _modifiedConfiguration.configs[_lastSelectedIndex].server;
            }
        }

        private void LoadCurrentConfiguration()
        {
            _modifiedConfiguration = controller.GetConfigurationCopy();
            LoadConfiguration(_modifiedConfiguration);
        }

        private void ConfigForm_Load(object sender, EventArgs e)
        {

        }

        private void ConfigForm_KeyDown(object sender, KeyEventArgs e)
        {
            // Sometimes the users may hit enter key by mistake, and the form will close without saving entries.

            if (e.KeyCode == Keys.Enter)
            {
                Server server = controller.GetCurrentServer();
                if (_modifiedConfiguration.configs.Count == 0)
                {
                    MessageBox.Show(I18N.GetString("Please add at least one server"));
                    return;
                }
                controller.SaveConfig(_modifiedConfiguration);
                //controller.SaveServers(_modifiedConfiguration.configs);
                //controller.SelectServerIndex(_modifiedConfiguration.configs.IndexOf(server));
            }

        }
        private void AddButton_Click(object sender, EventArgs e)
        {
            Server server = Configuration.GetDefaultServer();
            if (Uri.CheckHostName(server.server = IPTextBox.Text.Trim()) == UriHostNameType.Unknown)
            {
                MessageBox.Show(I18N.GetString("Invalid server address"));
                IPTextBox.Focus();
                return ;
            }
            _modifiedConfiguration.configs.Add(server);
            LoadConfiguration(_modifiedConfiguration);
            _lastSelectedIndex = ServersListBox.SelectedIndex;
        }

        private void DeleteButton_Click(object sender, EventArgs e)
        {
            if (_modifiedConfiguration.configs.Count == 1)
            {
                MessageBox.Show(I18N.GetString("Please add at least one server"));
                return;
            }
            _lastSelectedIndex = ServersListBox.SelectedIndex;
            if (_lastSelectedIndex >= 0 && _lastSelectedIndex < _modifiedConfiguration.configs.Count)
            {
                _modifiedConfiguration.configs.RemoveAt(_lastSelectedIndex);
            }
            ServersListBox.SelectedIndex = _lastSelectedIndex;
            LoadConfiguration(_modifiedConfiguration);
            ServersListBox.SelectedIndex = _lastSelectedIndex;
            LoadSelectedServer();
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void ConfigForm_Shown(object sender, EventArgs e)
        {
            IPTextBox.Focus();
        }

        private void ConfigForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            //controller.ConfigChanged -= controller_ConfigChanged;
            controller.SaveConfig(_modifiedConfiguration);
        }
        private void IPTextBox_TextChanged(object sender, EventArgs e)
        {

        }
    }
}
