using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OoLunar.Willow.Database;

namespace OoLunar.Willow
{
    public sealed class Program
    {
        private static void Main(string[] args)
        {
            IHost host = Host.CreateDefaultBuilder(args)
                .ConfigureServices(services => services
                    .AddDbContextFactory<DatabaseContext>()
                    .AddHostedService<ServerListener>()
                ).Build();

            host.Run();
        }
    }
}
