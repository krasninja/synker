using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Eto.Forms;
using Synker.App.Abstract;
using Synker.App.Annotations;
using Synker.Domain;

namespace Synker.App
{
    /// <summary>
    /// View model for <see cref="StatusForm" />.
    /// </summary>
    public sealed class StatusFormViewModel : INotifyPropertyChanged, IViewModelWithAsyncLoading
    {
        /// <summary>
        /// Profiles.
        /// </summary>
        public FilterCollection<StatusFormProfileModel> Profiles { get; }
            = new FilterCollection<StatusFormProfileModel>();

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="profiles">Profiles to initialize.</param>
        /// <exception cref="ArgumentNullException">Profiles argument is null.</exception>
        public StatusFormViewModel(IEnumerable<Profile> profiles)
        {
            if (profiles == null)
            {
                throw new ArgumentNullException(nameof(profiles));
            }
            Profiles.AddRange(profiles.Select(p => new StatusFormProfileModel(p)));
        }

        #region INotifyPropertyChanged

        /// <inheritdoc />
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region IViewModelWithAsyncLoading

        /// <inheritdoc />
        public async Task LoadAsync(CancellationToken cancellationToken = default)
        {
            foreach (StatusFormProfileModel statusFormProfileModel in Profiles)
            {
                await statusFormProfileModel.SetLastUpdateAsync();
            }
        }

        #endregion
    }
}
