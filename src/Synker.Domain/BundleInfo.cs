using System;

namespace Synker.Domain
{
    /// <summary>
    /// Bundle information (file, database record, etc).
    /// </summary>
    public class BundleInfo
    {
        /// <summary>
        /// Identifier.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Bundle creation date time.
        /// </summary>
        public DateTime CreateAt { get; }

        /// <summary>
        /// Bundle size in bytes.
        /// </summary>
        public long SizeInBytes { get; }

        public BundleInfo(string id, DateTime createdAt, long sizeInBytes)
        {
            this.Id = id;
            this.CreateAt = createdAt;
            this.SizeInBytes = sizeInBytes;
        }

        /// <summary>
        /// Is bundle item outdated.
        /// </summary>
        /// <param name="now">Today date.</param>
        /// <param name="maxDays">Max days ags.</param>
        /// <returns><c>True</c> if outdated.</returns>
        public bool IsOutdated(DateTime now, int maxDays) => (now - CreateAt).TotalDays > maxDays;
    }
}
