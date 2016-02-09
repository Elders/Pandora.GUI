using System;
using System.Linq;
using System.Windows.Forms;
using Elders.Pandora;

namespace Pandora.GUI
{
    public partial class PandoraUI : Form
    {
        public PandoraUI()
        {
            InitializeComponent();

            LoadApplicationsControl();
        }

        private void LoadApplicationsControl()
        {
            ListBox appList = new ListBox();
            appList.SelectedIndexChanged += AppList_SelectedIndexChanged;
            appList.Name = "appList";
            appList.Dock = DockStyle.Fill;

            var apps = ApplicationConfiguration.GetAllOnMachine().Select(x => x.ApplicationName).Distinct().ToList();
            apps.Sort();
            appList.Items.AddRange(apps.ToArray());
            splitContainer1.Panel1.Controls.Add(appList);
        }

        private void AppList_SelectedIndexChanged(object sender, EventArgs e)
        {
            var selectedApp = (sender as ListBox).Text;
            if (String.IsNullOrEmpty(selectedApp))
                return;

            var pandoraListView = new PandoraListView(selectedApp);

            splitContainer1.Panel2.Controls.Clear();
            splitContainer1.Panel2.Controls.Add(pandoraListView);
        }
    }
}