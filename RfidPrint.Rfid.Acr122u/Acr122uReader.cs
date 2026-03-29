using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using FrApp42.ACR122U;
using RfidPrint.Rfid;
using PCSC; // Необходим для прямого обращения к подсистеме смарт-карт

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
                    // 1. Проверяем службу и наличие ридеров. 
                    // Если что-то не так — метод выбросит исключение и остановит поток.
                    CheckConnectedReaders();

                    // 2. Инициализация монитора событий (только если проверка выше прошла успешно)
                    _reader = new Reader();
                    _reader.Connected += OnReaderConnected;
                    _reader.Disconnected += OnReaderDisconnected;
                    _reader.Inserted += OnCardInserted;
                    _reader.Removed += OnCardRemoved;

                    _logger.LogInformation("Мониторинг событий считывателя запущен, ожидаем метки...");

                    while (!cancellationToken.IsCancellationRequested)
                    {
                        Thread.Sleep(1000);
                    }
                }
                catch (Exception ex)
                {
                    // Логируем критическую ошибку. Воркер в Service перехватит её и остановится.
                    _logger.LogCritical(ex, "Критическая ошибка: инициализация считывателя невозможна.");
                    throw;
                }
                finally
                {
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

        private void CheckConnectedReaders()
        {
            try
            {
                var contextFactory = ContextFactory.Instance;
                using var context = contextFactory.Establish(SCardScope.System);
                var readerNames = context.GetReaders();

                if (readerNames == null || readerNames.Length == 0)
                {
                    // Считыватель не подключен. Бросаем исключение.
                    throw new InvalidOperationException("Аппаратный считыватель ACR122U физически не обнаружен в USB-порту.");
                }

                _logger.LogInformation("Обнаружены аппаратные считыватели: {Readers}", string.Join(", ", readerNames));
            }
            catch (PCSC.Exceptions.NoServiceException ex)
            {
                // Служба смарт-карт отключена. Бросаем понятное исключение.
                throw new InvalidOperationException("Системная служба 'Смарт-карта' (SCardSvr) отключена в ОС. Работа невозможна.", ex);
            }
            catch (Exception ex) when (ex is not InvalidOperationException)
            {
                // Любые другие непредвиденные ошибки PC/SC
                throw new InvalidOperationException("Не удалось опросить подсистему PC/SC.", ex);
            }
        }

        private void OnReaderConnected(string readerName)
        {
            _logger.LogInformation("Считыватель подключен: {ReaderName}", readerName);
        }

        private void OnReaderDisconnected(string readerName)
        {
            _logger.LogWarning("Считыватель отключен: {ReaderName}", readerName);
        }

        private void OnCardInserted(string uid)
        {
            _logger.LogInformation("Обнаружена метка. UID: {Uid}", uid);
            OnTagRead(uid);
        }

        private void OnCardRemoved()
        {
            _logger.LogDebug("Метка убрана со считывателя"); // Изменено на Debug, чтобы не засорять логи
        }

        public override void Dispose()
        {
            if (_disposed) return;
            lock (_lock)
            {
                if (_disposed) return;
                _reader = null;
                _disposed = true;
            }
            GC.SuppressFinalize(this);
        }
    }
}