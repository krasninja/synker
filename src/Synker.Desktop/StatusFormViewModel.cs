using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Eto.Forms;
using Synker.Desktop.Abstract;
using Synker.Domain;

namespace Synker.Desktop
{
    /// <summary>
    /// View model for <see cref="StatusForm" />.
    /// </summary>
    internal sealed class StatusFormViewModel : INotifyPropertyChanged, IViewModelWithAsyncLoading
    {
        private readonly IBundleFactory bundleFactory;
        private readonly IEnumerable<Profile> profiles;

        /// <summary>
        /// Profiles.
        /// </summary>
        public FilterCollection<StatusFormProfileModel> Profiles { get; }
            = new FilterCollection<StatusFormProfileModel>();

        /// <summary>
        /// Currently selected profile.
        /// </summary>
        public StatusFormProfileModel SelectedProfile { get; set; }

        /// <summary>
        /// Export selected profile command.
        /// </summary>
        public Command ExportCommand { get; }

        /// <summary>
        /// Import selected profile command.
        /// </summary>
        public Command ImportCommand { get; }

        /// <summary>
        /// Refresh profiles.
        /// </summary>
        public Command RefreshCommand { get; }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="profiles">Profiles to initialize.</param>
        /// <param name="bundleFactory">Bundle factory.</param>
        public StatusFormViewModel(IEnumerable<Profile> profiles, IBundleFactory bundleFactory)
        {
            this.profiles = profiles ?? throw new ArgumentNullException(nameof(profiles));
            this.bundleFactory = bundleFactory ?? throw new ArgumentNullException(nameof(bundleFactory));

            ExportCommand = new Command(ExportCommandHandler);
            ImportCommand = new Command(ImportCommandHandler);
            RefreshCommand = new Command(RefreshCommandHandler);
        }

        private async void ExportCommandHandler(object sender, EventArgs args)
        {
            if (SelectedProfile == null)
            {
                return;
            }
            var result =
                await new UseCases.Export.ExportCommand(SelectedProfile.Profile, bundleFactory).ExecuteAsync();
            MessageBox.Show("Export result: " + Saritasa.Tools.Common.Utils.EnumUtils.GetDescription(result));
        }

        private async void ImportCommandHandler(object sender, EventArgs args)
        {
            if (SelectedProfile == null)
            {
                return;
            }
            var result =
                await new UseCases.Import.ImportCommand(SelectedProfile.Profile, bundleFactory).ExecuteAsync();
            MessageBox.Show("Import result: " + Saritasa.Tools.Common.Utils.EnumUtils.GetDescription(result));
        }

        private async void RefreshCommandHandler(object sender, EventArgs args) => await LoadAsync();

        public async Task SetLastUpdateDateAsync(IEnumerable<StatusFormProfileModel> profileModels)
        {
            foreach (StatusFormProfileModel statusFormProfileModel in profileModels)
            {
                await statusFormProfileModel.SetLastUpdateAsync();
            }
        }

        #region INotifyPropertyChanged

        /// <inheritdoc />
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region IViewModelWithAsyncLoading

        /// <inheritdoc />
        public async Task LoadAsync(CancellationToken cancellationToken = default)
        {
            Profiles.Clear();
            var profileModels = profiles.Select(p => new StatusFormProfileModel(p))
                .ToArray();
            await SetLastUpdateDateAsync(profileModels);
            Profiles.AddRange(profileModels);
        }

        #endregion
    }
}
