using System;

namespace RfidPrint.Common.Models
{
    public class CardMapping
    {
        public int Id { get; set; }
        public string Uid { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string? PrinterName { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}