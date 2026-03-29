using System;

namespace RfidPrint.Common.Models
{
    public class PrintLogEntry
    {
        public long Id { get; set; }
        public string Uid { get; set; } = string.Empty;
        public string ComputerName { get; set; } = string.Empty;
        public string? PrinterName { get; set; }
        public string? FilePath { get; set; }
        public string Status { get; set; } = string.Empty; // "success" или "error"
        public string? ErrorMessage { get; set; }
        public DateTime PrintedAt { get; set; }
    }
}