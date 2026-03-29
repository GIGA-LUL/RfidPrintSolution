using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Npgsql;
using RfidPrint.Common.Interfaces;
using RfidPrint.Common.Models;

namespace RfidPrint.Database.Repositories
{
    public class PrintLogRepository : IPrintLogRepository
    {
        private readonly DatabaseConnection _dbConnection;
        private readonly ILogger<PrintLogRepository> _logger;

        public PrintLogRepository(DatabaseConnection dbConnection, ILogger<PrintLogRepository> logger)
        {
            _dbConnection = dbConnection;
            _logger = logger;
        }

        public async Task AddAsync(PrintLogEntry entry)
        {
            try
            {
                await using var conn = _dbConnection.CreateConnection();
                await conn.OpenAsync();
                const string sql = @"
                    INSERT INTO print_log (uid, computer_name, printer_name, file_path, status, error_message, printed_at)
                    VALUES (@uid, @computerName, @printerName, @filePath, @status, @errorMessage, @printedAt)";
                await using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("uid", entry.Uid);
                cmd.Parameters.AddWithValue("computerName", entry.ComputerName);
                cmd.Parameters.AddWithValue("printerName", entry.PrinterName ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("filePath", entry.FilePath ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("status", entry.Status);
                cmd.Parameters.AddWithValue("errorMessage", entry.ErrorMessage ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("printedAt", entry.PrintedAt);
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding print log entry for UID {Uid}", entry.Uid);
                throw;
            }
        }

        public async Task<IEnumerable<PrintLogEntry>> GetByUidAsync(string uid, DateTime? from = null, DateTime? to = null)
        {
            var list = new List<PrintLogEntry>();
            try
            {
                await using var conn = _dbConnection.CreateConnection();
                await conn.OpenAsync();
                var sql = "SELECT id, uid, computer_name, printer_name, file_path, status, error_message, printed_at FROM print_log WHERE uid = @uid";
                if (from.HasValue) sql += " AND printed_at >= @from";
                if (to.HasValue) sql += " AND printed_at <= @to";
                sql += " ORDER BY printed_at DESC";

                await using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("uid", uid);
                if (from.HasValue) cmd.Parameters.AddWithValue("from", from.Value);
                if (to.HasValue) cmd.Parameters.AddWithValue("to", to.Value);

                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    list.Add(new PrintLogEntry
                    {
                        Id = reader.GetInt64(0),
                        Uid = reader.GetString(1),
                        ComputerName = reader.GetString(2),
                        PrinterName = reader.IsDBNull(3) ? null : reader.GetString(3),
                        FilePath = reader.IsDBNull(4) ? null : reader.GetString(4),
                        Status = reader.GetString(5),
                        ErrorMessage = reader.IsDBNull(6) ? null : reader.GetString(6),
                        PrintedAt = reader.GetDateTime(7)
                    });
                }
                return list;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving logs for UID {Uid}", uid);
                throw;
            }
        }
    }
}