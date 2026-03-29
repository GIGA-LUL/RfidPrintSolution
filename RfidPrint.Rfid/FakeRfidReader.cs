using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RfidPrint.Common.Interfaces;

namespace RfidPrint.Rfid
{
    public class FakeRfidReader : IRfidReader
    {
        private readonly ILogger<FakeRfidReader> _logger;
        public event EventHandler<string>? TagRead;

        public FakeRfidReader(ILogger<FakeRfidReader> logger)
        {
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("=== ЗАПУЩЕН ЭМУЛЯТОР RFID ===");
            _logger.LogInformation("Вы можете имитировать прикладывание карты, введя UID в консоль и нажав Enter.");

            // Запускаем задачу чтения консоли в фоновом потоке
            _ = Task.Run(() =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var input = Console.ReadLine();
                    if (!string.IsNullOrWhiteSpace(input))
                    {
                        _logger.LogInformation("Эмулятор: Считан UID {Uid}", input);
                        TagRead?.Invoke(this, input.Trim());
                    }
                }
            }, cancellationToken);

            await Task.CompletedTask;
        }

        public Task StopAsync()
        {
            _logger.LogInformation("Эмулятор RFID остановлен.");
            return Task.CompletedTask;
        }

        public void Dispose() { }
    }
}