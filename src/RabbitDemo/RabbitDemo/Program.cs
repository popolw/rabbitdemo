using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;
using System.Threading.Tasks;
using EasyNetQ.DI.Microsoft;
using Microsoft.Extensions.DependencyInjection;

namespace RabbitDemo
{
    class Program
    {
        async static Task Main(string[] args)
        {
            await CreateHost(args).RunAsync();
        }

        private static IHost CreateHost(string[] args) =>
             Host.CreateDefaultBuilder()
                .ConfigureHostConfiguration(builder => {
                    var dir = Directory.GetCurrentDirectory();
                    builder.AddJsonFile(Path.Combine(dir, "appsettings.json"), false);
                    builder.AddCommandLine(args);
                })
                .ConfigureServices((context, services) => {
                    services.AddLogging();
                    services.RegisterEasyNetQ(context.Configuration.GetConnectionString("Rabbit"));
                    var model= context.Configuration["m"];
                    if (model == "r")
                    {
                        services.AddHostedService<RabbitService>();
                    }
                    if (model == "p")
                    {
                        services.AddHostedService<PublishService>();
                    }
                })
                .UseConsoleLifetime()
                .Build();
    }
}
