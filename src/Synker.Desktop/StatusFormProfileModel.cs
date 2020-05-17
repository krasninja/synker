using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Synker.Domain;

namespace Synker.Desktop
{
    /// <summary>
    /// Profile model with more information.
    /// </summary>
    [Serializable]
    internal class StatusFormProfileModel : INotifyPropertyChanged
    {
        public string Id => Profile.Id;

        public string Name => Profile.Name;

        /// <summary>
        /// Profile description.
        /// </summary>
        public string Description => Profile.Description;

        private DateTime? lastLocalUpdate;

        /// <summary>
        /// Settings update on local host.
        /// </summary>
        public DateTime? LastLocalUpdate
        {
            get => lastLocalUpdate;
            set
            {
                lastLocalUpdate = value;
                OnPropertyChanged(nameof(LastLocalUpdate));
            }
        }

        public DateTime? LastExport { get; private set; }

        public DateTime? LastImport { get; private set; }

        public Profile Profile { get;}

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="profile">Source profile.</param>
        /// <exception cref="ArgumentNullException">Profile is null.</exception>
        public StatusFormProfileModel(Profile profile)
        {
            this.Profile = profile ?? throw new ArgumentNullException(nameof(profile));
        }

        /// <summary>
        /// Sets last local update time.
        /// </summary>
        /// <returns>Task.</returns>
        public async Task SetLastUpdateAsync()
        {
            LastLocalUpdate = await this.Profile.GetLatestLocalUpdateDateTimeAsync();
        }

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
