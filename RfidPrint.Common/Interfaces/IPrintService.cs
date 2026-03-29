using System.Threading.Tasks;

namespace RfidPrint.Common.Interfaces
{
    public interface IPrintService
    {
        Task<bool> PrintAsync(string filePath, string? printerName = null);
    }
}