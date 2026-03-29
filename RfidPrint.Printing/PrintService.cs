using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
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

        public Task<bool> PrintAsync(string filePath, string? printerName = null)
        {
            return Task.Run(() =>
            {
                try
                {
                    if (!File.Exists(filePath))
                    {
                        _logger.LogError("File not found: {FilePath}", filePath);
                        return false;
                    }

                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                        return PrintWindows(filePath, printerName);
                    else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                        return PrintLinux(filePath, printerName);
                    else
                        throw new PlatformNotSupportedException("Unsupported OS");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Print error for file {FilePath}", filePath);
                    return false;
                }
            });
        }

        private bool PrintWindows(string filePath, string? printerName)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = filePath,
                    UseShellExecute = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                if (string.IsNullOrEmpty(printerName))
                {
                    psi.Verb = "print";
                }
                else
                {
                    psi.Verb = "printto";
                    psi.Arguments = $"\"{printerName}\"";
                }

                using var process = Process.Start(psi);
                process?.WaitForExit(10000); // 10 sec timeout
                _logger.LogInformation("Print command sent for {FilePath} to printer {PrinterName}", filePath, printerName ?? "default");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Windows print failed");
                return false;
            }
        }

        private bool PrintLinux(string filePath, string? printerName)
        {
            try
            {
                string args = string.IsNullOrEmpty(printerName)
                    ? $"\"{filePath}\""
                    : $"-d {printerName} \"{filePath}\"";

                var psi = new ProcessStartInfo
                {
                    FileName = "lp",
                    Arguments = args,
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true
                };

                using var process = Process.Start(psi);
                if (process == null) throw new Exception("Could not start lp process");
                process.WaitForExit(10000);
                if (process.ExitCode != 0)
                {
                    var error = process.StandardError.ReadToEnd();
                    _logger.LogError("lp command failed with exit code {ExitCode}: {Error}", process.ExitCode, error);
                    return false;
                }
                _logger.LogInformation("Print command sent for {FilePath} to printer {PrinterName}", filePath, printerName ?? "default");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Linux print failed");
                return false;
            }
        }
    }
}