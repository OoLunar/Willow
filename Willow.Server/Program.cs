using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace OoLunar.Willow.Server
{
    public sealed class Program
    {
        public static void Main(string[] args)
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
