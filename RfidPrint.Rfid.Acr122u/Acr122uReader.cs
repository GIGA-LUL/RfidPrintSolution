using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using FrApp42.ACR122U;
using RfidPrint.Rfid;

namespace RfidPrint.Rfid.Acr122u
{
    public class Acr122uReader : RfidReaderBase
    {
        private Reader? _reader;
        private readonly object _lock = new object();
        private bool _disposed;
        private CancellationToken _cancellationToken;

        public Acr122uReader(ILogger<Acr122uReader> logger) : base(logger)
        {
        }

        protected override async Task StartReadingAsync(CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;
            await Task.Run(() =>
            {
                try
                {
                    _reader = new Reader();

                    // Подписываемся на события
                    _reader.Connected += OnReaderConnected;
                    _reader.Disconnected += OnReaderDisconnected;
                    _reader.Inserted += OnCardInserted;
                    _reader.Removed += OnCardRemoved;

                    _logger.LogInformation("FrApp42.ACR122U reader initialized, waiting for cards...");

                    // Ожидаем отмены
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        Thread.Sleep(1000);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to initialize ACR122U reader");
                    throw;
                }
                finally
                {
                    // Отписываемся от событий
                    if (_reader != null)
                    {
                        _reader.Connected -= OnReaderConnected;
                        _reader.Disconnected -= OnReaderDisconnected;
                        _reader.Inserted -= OnCardInserted;
                        _reader.Removed -= OnCardRemoved;
                    }
                }
            }, cancellationToken);
        }

        private void OnReaderConnected(string readerName)
        {
            _logger.LogInformation("Reader connected: {ReaderName}", readerName);
        }

        private void OnReaderDisconnected(string readerName)
        {
            _logger.LogWarning("Reader disconnected: {ReaderName}", readerName);
        }

        private void OnCardInserted(string uid)
        {
            _logger.LogInformation("Card inserted: UID={Uid}", uid);
            OnTagRead(uid);
        }

        private void OnCardRemoved()
        {
            _logger.LogInformation("Card removed");
        }

        public override void Dispose()
        {
            if (_disposed) return;
            lock (_lock)
            {
                if (_disposed) return;
                _reader = null; // Библиотека не требует явного Dispose
                _disposed = true;
            }
            GC.SuppressFinalize(this);
        }
    }
}