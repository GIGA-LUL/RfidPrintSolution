using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RfidPrint.Common.Interfaces;

namespace RfidPrint.Printing
{
    public class PrintService : IPrintService
    {
        private readonly ILogger<PrintService> _logger;

        public PrintService(ILogger<PrintService> logger)
        {
            _logger = logger;
        }

        public async Task<bool> PrintAsync(string filePath, string? printerName = null)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    _logger.LogError("Файл не найден: {FilePath}", filePath);
                    return false;
                }

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    return await PrintWindowsAsync(filePath, printerName);

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    return await PrintLinuxAsync(filePath, printerName);

                throw new PlatformNotSupportedException("Операционная система не поддерживается.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при попытке печати файла {FilePath}", filePath);
                return false;
            }
        }

        private async Task<bool> PrintWindowsAsync(string filePath, string? printerName)
        {
            // Windows использует команду printto для конкретного принтера или print для дефолтного
            var psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardError = true
            };

            if (string.IsNullOrEmpty(printerName))
            {
                psi.ArgumentList.Add("/c");
                psi.ArgumentList.Add("print");
                psi.ArgumentList.Add(filePath);
            }
            else
            {
                // Для отправки на конкретный принтер часто используются сторонние утилиты 
                // или прямая запись в порт. Стандартный print /d:printer не всегда стабилен в .NET.
                // Оставляем вашу базовую логику, но упаковываем в безопасный ArgumentList.
                psi.ArgumentList.Add("/c");
                psi.ArgumentList.Add("print");
                psi.ArgumentList.Add($"/d:{printerName}");
                psi.ArgumentList.Add(filePath);
            }

            return await RunPrintProcessAsync(psi, "Windows (cmd)");
        }

        private async Task<bool> PrintLinuxAsync(string filePath, string? printerName)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "lp",
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };

            // Безопасное добавление аргументов для предотвращения Command Injection
            if (!string.IsNullOrEmpty(printerName))
            {
                psi.ArgumentList.Add("-d");
                psi.ArgumentList.Add(printerName);
            }
            psi.ArgumentList.Add(filePath);

            return await RunPrintProcessAsync(psi, "Linux (lp)");
        }

        private async Task<bool> RunPrintProcessAsync(ProcessStartInfo psi, string platformName)
        {
            using var process = new Process { StartInfo = psi };

            try
            {
                if (!process.Start())
                {
                    _logger.LogError("Не удалось запустить процесс печати для {Platform}", platformName);
                    return false;
                }

                // Создаем таймаут на 15 секунд для операции печати
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));

                try
                {
                    // Асинхронное ожидание не блокирует поток пула
                    await process.WaitForExitAsync(cts.Token);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogError("Процесс печати {Platform} превысил таймаут и будет принудительно завершен.", platformName);

                    if (!process.HasExited)
                    {
                        process.Kill(entireProcessTree: true); // Убиваем зависший процесс
                    }
                    return false;
                }

                if (process.ExitCode != 0)
                {
                    string error = await process.StandardError.ReadToEndAsync();
                    _logger.LogError("Команда печати {Platform} завершилась с ошибкой {ExitCode}. Ошибка: {Error}",
                        platformName, process.ExitCode, error);
                    return false;
                }

                _logger.LogInformation("Задание успешно отправлено на печать через {Platform}", platformName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Критический сбой при выполнении процесса печати {Platform}", platformName);
                return false;
            }
        }
    }
}