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
            _logger.LogInformation("Служба RfidPrintWorker запущена на машине: {ComputerName}", _computerName);

            // Подписываемся на событие считывания метки
            _rfidReader.TagRead += async (s, uid) => await ProcessTagAsync(uid);

            try
            {
                // Запуск считывателя (внутри ACR122U теперь есть жесткая проверка службы)
                await _rfidReader.StartAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Не удалось запустить RFID-модуль. Проверьте службу смарт-карт и подключение устройства.");
                // Позволяем исключению всплыть, чтобы Host остановился согласно настройкам
                throw;
            }

            // Держим воркер активным, пока не придет сигнал остановки
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }

        private async Task ProcessTagAsync(string uid)
        {
            _logger.LogInformation("=== Обработка новой метки: {Uid} ===", uid);

            string? targetFile = null;
            string? printerName = null;

            try
            {
                // 1. Ищем маппинг в БД
                var mapping = await _cardRepository.GetByUidAsync(uid);
                if (mapping == null)
                {
                    _logger.LogWarning("Для карты {Uid} не найдено правил в базе данных.", uid);
                    await LogPrintAttempt(uid, null, null, "error", "Карта не зарегистрирована");
                    return;
                }

                targetFile = mapping.FilePath;
                printerName = mapping.PrinterName;

                // 2. Проверяем физическое наличие файла
                if (!File.Exists(targetFile))
                {
                    _logger.LogError("Файл для печати не найден по пути: {Path}", targetFile);
                    await LogPrintAttempt(uid, printerName, targetFile, "error", "Файл отсутствует на диске");
                    return;
                }

                // 3. Отправляем на печать
                _logger.LogInformation("Отправка файла {Path} на принтер {Printer}", targetFile, printerName ?? "По умолчанию");
                bool success = await _printService.PrintAsync(targetFile, printerName);

                // 4. Логируем результат
                if (success)
                {
                    _logger.LogInformation("Задание на печать успешно создано для {Uid}", uid);
                    await LogPrintAttempt(uid, printerName, targetFile, "success", null);
                }
                else
                {
                    _logger.LogError("Сервис печати вернул ошибку для {Uid}", uid);
                    await LogPrintAttempt(uid, printerName, targetFile, "error", "Ошибка сервиса печати (проверьте очередь печати ОС)");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Критический сбой при обработке события карты {Uid}", uid);
                await LogPrintAttempt(uid, printerName, targetFile, "error", $"Исключение: {ex.Message}");
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
                // Используем репозиторий с исправленной типизацией Npgsql
                await _logRepository.AddAsync(entry);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Не удалось записать результат в базу данных для {Uid}. Проверьте соединение с PostgreSQL.", uid);
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Остановка службы RfidPrintWorker...");
            await _rfidReader.StopAsync();
            await base.StopAsync(cancellationToken);
        }
    }
}