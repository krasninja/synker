using System.Collections.Generic;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Synker.Domain;

namespace Synker.Web
{
    /// <summary>
    /// Simple server implementation.
    /// </summary>
    public class Server
    {
        public static IWebHost Create(IBundleFactory bundleFactory, IList<Profile> profiles)
        {
            return new WebHostBuilder()
                .UseKestrel()
                .ConfigureServices(services =>
                {
                    services.AddSingleton(bundleFactory);
                    services.AddSingleton(profiles);
                })
                .UseStartup<Startup>()
                .Build();
        }
    }
}
