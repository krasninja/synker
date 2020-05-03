using System;
using Eto.Drawing;
using Eto.Forms;
using Synker.Desktop.Abstract;

namespace Synker.Desktop
{
    /// <summary>
    /// Form shows all profiles and its current status.
    /// </summary>
    public class StatusForm : Form
    {
        private readonly EventHandler<EventArgs> loadCompleteEventHandler;
        private readonly GridView profilesGrid = new GridView
        {
            ID = "Profiles",
            AllowMultipleSelection = false,
            GridLines = GridLines.Both,
            AllowEmptySelection = true,
            Columns =
            {
                new GridColumn
                {
                    HeaderText = "Identifier",
                    Width = 170,
                    AutoSize = false,
                    DataCell = new TextBoxCell("Id")
                },
                new GridColumn
                {
                    ID = "Name",
                    HeaderText = "Name",
                    Width = 350,
                    AutoSize = false,
                    DataCell = new TextBoxCell("Name")
                },
                new GridColumn
                {
                    ID = "LastLocalUpdate",
                    HeaderText = "Last Local Update",
                    Width = 200,
                    AutoSize = false,
                    DataCell = new TextBoxCell("LastLocalUpdate")
                    {
                        Binding = Binding.Property<StatusFormProfileModel, string>(
                            p => p.LastLocalUpdate.HasValue ? p.LastLocalUpdate.Value.ToString("g") : String.Empty)
                    }
                }
            }
        };

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="viewModel">View model.</param>
        internal StatusForm(StatusFormViewModel viewModel)
        {
            ClientSize = new Size(800, 400);
            Title = "Status";
            Menu = new MenuBar
            {
                Items =
                {
                    new ButtonMenuItem
                    {
                        Text = "File",
                        Items =
                        {
                            new ButtonMenuItem
                            {
                                Command = viewModel.RefreshCommand,
                                Text = "Refresh",
                            }
                        }
                    }
                }
            };
            Content = new TableLayout
            {
                Spacing = new Size(5, 5),
                Padding = new Padding(10, 10, 10, 10),

                Rows =
                {
                    new TableRow(
                        new TableCell(profilesGrid)
                    )
                    {
                        ScaleHeight = true
                    }
                }
            };

            this.DataContext = viewModel;
            profilesGrid.DataStore = viewModel.Profiles;
            profilesGrid.SelectedItemBinding.Bind(
                viewModel,
                obj => obj.SelectedProfile);
            profilesGrid.ContextMenu = new ContextMenu
            {
                Items =
                {
                    new ButtonMenuItem
                    {
                        Command = viewModel.ExportCommand,
                        Text = "Export"
                    },
                    new ButtonMenuItem
                    {
                        Command = viewModel.ImportCommand,
                        Text = "Import"
                    },
                }
            };
            profilesGrid.MouseUp += (sender, e) =>
            {
                if (e.Buttons == MouseButtons.Alternate)
                {
                    profilesGrid.SelectedRow = profilesGrid.GetCellAt(e.Location).RowIndex;
                }
            };

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
    }
}
