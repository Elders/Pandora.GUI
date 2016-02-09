using System;
using System.Linq;
using System.Collections;
using System.Drawing;
using System.Windows.Forms;
using Elders.Pandora;

namespace Pandora.GUI
{
    public class PandoraListView : ListView
    {
        private string applicationName;
        private SettingListViewItem li;
        private int X = 0;
        private int Y = 0;
        private string subItemText;
        private int subItemSelected = 0;
        private TextBox editBox = new TextBox();
        private ListViewColumnSorter lvwColumnSorter;

        public PandoraListView(string applicationName)
        {
            this.applicationName = applicationName;
            InitializeComponent();
            LoadData(applicationName);
        }

        private void EditOver(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 13)
            {
                li.SubItems[subItemSelected].Text = editBox.Text;
                editBox.Hide();
            }

            if (e.KeyChar == 27)
                editBox.Hide();
        }

        private void FocusOver(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(editBox.Text) == false && li.SubItems[subItemSelected].Text != editBox.Text)
            {
                var confirmResult = MessageBox.Show(
                    "Are you sure to modify this item?",
                    "Confirm Modification!",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button2);

                if (confirmResult == DialogResult.Yes)
                {
                    li.SubItems[subItemSelected].Text = editBox.Text;
                    Environment.SetEnvironmentVariable(li.Raw, editBox.Text, EnvironmentVariableTarget.Machine);
                }
            }
            editBox.Hide();
        }

        public void OnDoubleClick(object sender, EventArgs e)
        {
            // Check the subitem clicked.
            int nStart = X;
            int startPosition = 0;
            int endPosition = 0;
            for (int i = 0; i < Columns.Count; i++)
            {
                var ind = Columns[i].DisplayIndex;
                endPosition += Columns[i].Width;
                if (i == Columns.Count - 1 && nStart > startPosition && nStart < endPosition)
                {
                    subItemSelected = i;
                    break;
                }
                startPosition = endPosition;
            }

            Console.WriteLine("SUB ITEM SELECTED = " + li.SubItems[subItemSelected].Text);
            subItemText = li.SubItems[subItemSelected].Text;

            string colName = Columns[subItemSelected].Text;
            editBox.Size = new Size(endPosition - startPosition, li.Bounds.Bottom - li.Bounds.Top);
            editBox.Location = new Point(startPosition, li.Bounds.Y);
            editBox.Show();
            editBox.Text = subItemText;
            editBox.SelectAll();
            editBox.Focus();
            editBox.TextAlign = Columns[subItemSelected].TextAlign;
        }

        public void OnMouseDown(object sender, MouseEventArgs e)
        {
            li = GetItemAt(e.X, e.Y) as SettingListViewItem;
            X = e.X;
            Y = e.Y;
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            //  Main
            this.Name = "pandoraSettingsView";
            this.View = View.Details;
            this.Dock = DockStyle.Fill;
            this.GridLines = true;
            this.FullRowSelect = true;
            this.AllowColumnReorder = true;

            this.KeyDown += OnKeyDown;

            //  Column headers
            this.ColumnClick += new ColumnClickEventHandler(OnColumnClick);
            var columnHeader1 = new ColumnHeader() { Text = "Cluster" };
            var columnHeader2 = new ColumnHeader() { Text = "Machine" };
            var columnHeader3 = new ColumnHeader() { Text = "Key", TextAlign = HorizontalAlignment.Right };
            var columnHeader4 = new ColumnHeader() { Text = "Value" };
            Columns.AddRange(new ColumnHeader[] { columnHeader1, columnHeader2, columnHeader3, columnHeader4 });
            this.HeaderStyle = ColumnHeaderStyle.Clickable;
            this.lvwColumnSorter = new ListViewColumnSorter();
            this.ListViewItemSorter = lvwColumnSorter;

            //  Editable items
            this.MouseDown += new MouseEventHandler(OnMouseDown);
            this.DoubleClick += new EventHandler(OnDoubleClick);
            editBox.Size = new Size(0, 0);
            editBox.Location = new Point(0, 0);
            Controls.AddRange(new Control[] { editBox });
            editBox.KeyPress += new KeyPressEventHandler(EditOver);
            editBox.LostFocus += new EventHandler(FocusOver);
            editBox.BackColor = Color.LightYellow;
            editBox.BorderStyle = BorderStyle.Fixed3D;

            editBox.Hide();
            editBox.Text = "";


            this.ResumeLayout(false);

        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                var confirmResult = MessageBox.Show(
                    "Are you sure to delete this item?",
                    "Confirm Delete!!",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button2);

                if (confirmResult == DialogResult.Yes)
                {
                    var pandoraListView = sender as PandoraListView;
                    foreach (SettingListViewItem selectedItem in pandoraListView.SelectedItems)
                    {
                        Environment.SetEnvironmentVariable(selectedItem.Raw, string.Empty, EnvironmentVariableTarget.Machine);
                        pandoraListView.Items.Remove(selectedItem);
                    }
                }
            }
        }

        private void LoadData(string applicationName)
        {
            //  Data
            this.Items.Clear();
            var settings = from s in ApplicationConfiguration.GetAllOnMachine()
                           where s.ApplicationName == applicationName
                           select new SettingListViewItem(s);
            this.Items.AddRange(settings.ToArray());
            this.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
        }

        private void OnColumnClick(object sender, ColumnClickEventArgs e)
        {
            if (e.Column == lvwColumnSorter.SortColumn)
            {
                // Reverse the current sort direction for this column.
                if (lvwColumnSorter.Order == SortOrder.Ascending)
                {
                    lvwColumnSorter.Order = SortOrder.Descending;
                }
                else
                {
                    lvwColumnSorter.Order = SortOrder.Ascending;
                }
            }
            else
            {
                // Set the column number that is to be sorted; default to ascending.
                lvwColumnSorter.SortColumn = e.Column;
                lvwColumnSorter.Order = SortOrder.Ascending;
            }
            this.Sort();
        }
        public class ListViewColumnSorter : IComparer
        {
            /// <summary>
            /// Specifies the column to be sorted
            /// </summary>
            private int ColumnToSort;
            /// <summary>
            /// Specifies the order in which to sort (i.e. 'Ascending').
            /// </summary>
            private SortOrder OrderOfSort;
            /// <summary>
            /// Case insensitive comparer object
            /// </summary>
            private CaseInsensitiveComparer ObjectCompare;

            /// <summary>
            /// Class constructor.  Initializes various elements
            /// </summary>
            public ListViewColumnSorter()
            {
                // Initialize the column to '0'
                ColumnToSort = 0;

                // Initialize the sort order to 'none'
                OrderOfSort = SortOrder.None;

                // Initialize the CaseInsensitiveComparer object
                ObjectCompare = new CaseInsensitiveComparer();
            }

            /// <summary>
            /// This method is inherited from the IComparer interface.  It compares the two objects passed using a case insensitive comparison.
            /// </summary>
            /// <param name="x">First object to be compared</param>
            /// <param name="y">Second object to be compared</param>
            /// <returns>The result of the comparison. "0" if equal, negative if 'x' is less than 'y' and positive if 'x' is greater than 'y'</returns>
            public int Compare(object x, object y)
            {
                int compareResult;
                ListViewItem listviewX, listviewY;

                // Cast the objects to be compared to ListViewItem objects
                listviewX = (ListViewItem)x;
                listviewY = (ListViewItem)y;

                // Compare the two items
                compareResult = ObjectCompare.Compare(listviewX.SubItems[ColumnToSort].Text, listviewY.SubItems[ColumnToSort].Text);

                // Calculate correct return value based on object comparison
                if (OrderOfSort == SortOrder.Ascending)
                {
                    // Ascending sort is selected, return normal result of compare operation
                    return compareResult;
                }
                else if (OrderOfSort == SortOrder.Descending)
                {
                    // Descending sort is selected, return negative result of compare operation
                    return (-compareResult);
                }
                else
                {
                    // Return '0' to indicate they are equal
                    return 0;
                }
            }

            /// <summary>
            /// Gets or sets the number of the column to which to apply the sorting operation (Defaults to '0').
            /// </summary>
            public int SortColumn
            {
                set { ColumnToSort = value; }
                get { return ColumnToSort; }
            }

            /// <summary>
            /// Gets or sets the order of sorting to apply (for example, 'Ascending' or 'Descending').
            /// </summary>
            public SortOrder Order
            {
                set { OrderOfSort = value; }
                get { return OrderOfSort; }
            }

        }
    }

    public class SettingListViewItem : ListViewItem
    {
        public SettingListViewItem(DeployedSetting setting)
        {
            Raw = setting.Raw;
            ApplicationName = setting.ApplicationName;
            Cluster = setting.Cluster;
            Machine = setting.Machine;
            Key = setting.Key;
            Value = setting.Value;

            this.Text = Cluster;
            this.SubItems.AddRange(new string[] { Machine, Key, Value });
        }

        public string Raw { get; private set; }
        public string ApplicationName { get; private set; }
        public string Cluster { get; private set; }
        public string Machine { get; private set; }
        public string Key { get; private set; }
        public string Value { get; private set; }
    }
}