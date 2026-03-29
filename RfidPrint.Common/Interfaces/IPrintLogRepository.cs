using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RfidPrint.Common.Models;

namespace RfidPrint.Common.Interfaces
{
    public interface IPrintLogRepository
    {
        Task AddAsync(PrintLogEntry entry);
        Task<IEnumerable<PrintLogEntry>> GetByUidAsync(string uid, DateTime? from = null, DateTime? to = null);
    }
}