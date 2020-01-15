using System;
using System.Threading.Tasks;
using Synker.Domain;

namespace Synker.App
{
    /// <summary>
    /// Profile model with more information.
    /// </summary>
    [Serializable]
    public class StatusFormProfileModel
    {
        public string Id => Profile.Id;

        public string Name => Profile.Name;

        public string Description => Profile.Description;

        public DateTime? LastLocalUpdate { get; private set; }

        public Profile Profile { get;}

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="profile">Source profile.</param>
        /// <exception cref="ArgumentNullException">Profile is null.</exception>
        public StatusFormProfileModel(Profile profile)
        {
            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }
            this.Profile = profile;
        }

        /// <summary>
        /// Sets last local update time.
        /// </summary>
        /// <returns>Task.</returns>
        public async Task SetLastUpdateAsync()
        {
            LastLocalUpdate = await this.Profile.GetLatestLocalUpdateDateTimeAsync();
        }
    }
}
