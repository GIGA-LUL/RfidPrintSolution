using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using RfidPrint.Common.Interfaces;
using RfidPrint.Database;
using RfidPrint.Database.Repositories;
using RfidPrint.Printing;
using RfidPrint.Rfid;
using RfidPrint.Rfid.Acr122u;
using RfidPrint.Service;

namespace RfidPrint.App
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var host = Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((context, config) =>
                {
                    config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                    if (context.HostingEnvironment.IsDevelopment())
                        config.AddJsonFile($"appsettings.Development.json", optional: true);
                })
                .UseSerilog((context, services, configuration) =>
                {
                    configuration.ReadFrom.Configuration(context.Configuration);
                    configuration.Enrich.FromLogContext();
                })
                .ConfigureServices((context, services) =>
                {
                    // Database
                    services.AddSingleton<DatabaseConnection>();
                    services.AddScoped<ICardMappingRepository, CardMappingRepository>();
                    services.AddScoped<IPrintLogRepository, PrintLogRepository>();

                    // Printing
                    services.AddSingleton<IPrintService, PrintService>();

                    // В файле Program.cs (примерно 45-50 строка)

                    // Было:
                    services.AddSingleton<IRfidReader, Acr122uReader>();

                    // Стало (для теста):
                    // services.AddSingleton<IRfidReader, FakeRfidReader>();

                    // Worker
                    services.AddHostedService<RfidPrintWorker>();
                })
                .Build();

            await host.RunAsync();
        }
    }
}