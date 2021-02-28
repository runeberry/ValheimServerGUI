using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ValheimServerGUI
{
    public partial class MainWindow : Form
    {
        public MainWindow()
        {
            InitializeComponent();
            AfterInitializeComponent();
        }

        private void AfterInitializeComponent()
        {
            // Add menu item handlers
            this.MenuItemFileDirectories.Click += new EventHandler(this.MenuItemFileDirectories_Clicked);
            this.MenuItemFileExit.Click += new EventHandler(this.MenuItemFileExit_Clicked);

            this.MenuItemHelpUpdates.Click += new EventHandler(this.MenuItemHelpUpdates_Clicked);
            this.MenuItemHelpAbout.Click += new EventHandler(this.MenuItemHelpAbout_Clicked);
        }

        private void MenuItemFileDirectories_Clicked(object sender, EventArgs e)
        {
            this.StatusStripLabel.Text = "Clicked Directories!";
        }

        private void MenuItemFileExit_Clicked(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void MenuItemHelpUpdates_Clicked(object sender, EventArgs e)
        {
            this.StatusStripLabel.Text = "Clicked Updates!";
        }

        private void MenuItemHelpAbout_Clicked(object sender, EventArgs e)
        {
            this.StatusStripLabel.Text = "Clicked About!";
        }
    }
}
