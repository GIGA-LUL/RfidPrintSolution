using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RfidPrint.Common.Interfaces;

namespace RfidPrint.Rfid
{
    public abstract class RfidReaderBase : IRfidReader
    {
        protected readonly ILogger _logger;
        private CancellationTokenSource? _cts;

        public event EventHandler<string>? TagRead;

        protected RfidReaderBase(ILogger logger)
        {
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            await StartReadingAsync(_cts.Token);
        }

        protected abstract Task StartReadingAsync(CancellationToken cancellationToken);

        public virtual Task StopAsync()
        {
            _cts?.Cancel();
            return Task.CompletedTask;
        }

        protected virtual void OnTagRead(string uid)
        {
            TagRead?.Invoke(this, uid);
        }

        public abstract void Dispose();
    }
}