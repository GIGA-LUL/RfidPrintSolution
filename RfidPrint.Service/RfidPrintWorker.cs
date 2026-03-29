using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RfidPrint.Common.Interfaces;
using RfidPrint.Common.Models;

namespace RfidPrint.Service
{
    public class RfidPrintWorker : BackgroundService
    {
        private readonly ILogger<RfidPrintWorker> _logger;
        private readonly IRfidReader _rfidReader;
        private readonly ICardMappingRepository _cardRepository;
        private readonly IPrintLogRepository _logRepository;
        private readonly IPrintService _printService;
        private readonly string _computerName;

        public RfidPrintWorker(
            ILogger<RfidPrintWorker> logger,
            IRfidReader rfidReader,
            ICardMappingRepository cardRepository,
            IPrintLogRepository logRepository,
            IPrintService printService)
        {
            _logger = logger;
            _rfidReader = rfidReader;
            _cardRepository = cardRepository;
            _logRepository = logRepository;
            _printService = printService;
            _computerName = Environment.MachineName;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("RfidPrintWorker started");

            _rfidReader.TagRead += OnTagRead;
            await _rfidReader.StartAsync(stoppingToken);

            try
            {
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                // Ожидаемо
            }

            await _rfidReader.StopAsync();
            _logger.LogInformation("RfidPrintWorker stopped");
        }

        private async void OnTagRead(object? sender, string uid)
        {
            try
            {
                _logger.LogInformation("Tag read: {Uid}", uid);
                var mapping = await _cardRepository.GetByUidAsync(uid);
                if (mapping == null)
                {
                    _logger.LogWarning("No mapping found for UID {Uid}", uid);
                    await LogPrintAttempt(uid, null, null, "error", "Card not found");
                    return;
                }

                if (!File.Exists(mapping.FilePath))
                {
                    _logger.LogError("File not found: {FilePath}", mapping.FilePath);
                    await LogPrintAttempt(uid, mapping.PrinterName, mapping.FilePath, "error", $"File not found: {mapping.FilePath}");
                    return;
                }

                var success = await _printService.PrintAsync(mapping.FilePath, mapping.PrinterName);
                var status = success ? "success" : "error";
                var errorMessage = success ? null : "Print service returned false";
                await LogPrintAttempt(uid, mapping.PrinterName, mapping.FilePath, status, errorMessage);
                if (success)
                    _logger.LogInformation("Print successful for UID {Uid}", uid);
                else
                    _logger.LogError("Print failed for UID {Uid}", uid);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing tag {Uid}", uid);
                await LogPrintAttempt(uid, null, null, "error", ex.Message);
            }
        }

        private async Task LogPrintAttempt(string uid, string? printerName, string? filePath, string status, string? errorMessage)
        {
            var entry = new PrintLogEntry
            {
                Uid = uid,
                ComputerName = _computerName,
                PrinterName = printerName,
                FilePath = filePath,
                Status = status,
                ErrorMessage = errorMessage,
                PrintedAt = DateTime.UtcNow
            };
            try
            {
                await _logRepository.AddAsync(entry);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write print log for UID {Uid}", uid);
            }
        }
    }
}