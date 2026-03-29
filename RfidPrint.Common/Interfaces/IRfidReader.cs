using System;
using System.Threading;
using System.Threading.Tasks;

namespace RfidPrint.Common.Interfaces
{
    public interface IRfidReader : IDisposable
    {
        event EventHandler<string>? TagRead; // UID
        Task StartAsync(CancellationToken cancellationToken);
        Task StopAsync();
    }
}