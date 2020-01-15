using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Synker.Domain;
using Synker.UseCases.Export;
using Synker.UseCases.Import;

namespace Synker.Web
{
    /// <summary>
    /// Profiles.
    /// </summary>
    public class ProfilesController : ControllerBase
    {
        private readonly IList<Profile> profiles;
        private readonly IBundleFactory bundleFactory;

        public ProfilesController(IList<Profile> profiles, IBundleFactory bundleFactory)
        {
            this.profiles = profiles;
            this.bundleFactory = bundleFactory;
        }

        [HttpGet]
        public IList<Profile> Index() => profiles;

        [HttpPost]
        public async Task<IActionResult> Export(string id, CancellationToken cancellationToken)
        {
            var profile = profiles.FirstOrDefault(p => p.Id == id);
            if (profile == null)
            {
                return NotFound();
            }
            await new ExportCommand(profile, bundleFactory).ExecuteAsync(cancellationToken);
            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> Import(string id)
        {
            var profile = profiles.FirstOrDefault(p => p.Id == id);
            if (profile == null)
            {
                return NotFound();
            }
            await new ImportCommand(profile, bundleFactory).ExecuteAsync();
            return Ok();
        }
    }
}
