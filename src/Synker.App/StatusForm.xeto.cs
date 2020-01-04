using System;
using System.Linq;
using Eto.Forms;
using Eto.Serialization.Xaml;
using Synker.App.Abstract;

namespace Synker.App
{
    /// <summary>
    /// Form shows all profiles and its current status.
    /// </summary>
    public class StatusForm : Form
    {
        private readonly EventHandler<EventArgs> loadCompleteEventHandler;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="viewModel">View model.</param>
        internal StatusForm(object viewModel)
        {
            this.DataContext = viewModel;
            XamlReader.Load(this);
            InitializeProfilesGrid();

            loadCompleteEventHandler = async (sender, ev) =>
            {
                if (this.DataContext is IViewModelWithAsyncLoading vm)
                {
                    await vm.LoadAsync();
                }
            };
            this.LoadComplete += loadCompleteEventHandler;
        }

        /// <inheritdoc />
        protected override void OnUnLoad(EventArgs e)
        {
            this.LoadComplete -= loadCompleteEventHandler;
            base.OnUnLoad(e);
        }

        private void InitializeProfilesGrid()
        {
            var profilesGrid = this.FindChild<GridView>("Profiles");
            GridColumn GetColumn(string id) => profilesGrid.Columns.FirstOrDefault(c => c.ID == id);

            GetColumn("Id").DataCell = new TextBoxCell("Id");
            GetColumn("Name").DataCell = new TextBoxCell("Name");
            GetColumn("LastLocalUpdate").DataCell = new TextBoxCell("LastLocalUpdate");
        }
    }
}
