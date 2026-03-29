using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Npgsql;
using RfidPrint.Common.Interfaces;
using RfidPrint.Common.Models;

namespace RfidPrint.Database.Repositories
{
    public class CardMappingRepository : ICardMappingRepository
    {
        private readonly DatabaseConnection _dbConnection;
        private readonly ILogger<CardMappingRepository> _logger;

        public CardMappingRepository(DatabaseConnection dbConnection, ILogger<CardMappingRepository> logger)
        {
            _dbConnection = dbConnection;
            _logger = logger;
        }

        public async Task<CardMapping?> GetByUidAsync(string uid)
        {
            try
            {
                await using var conn = _dbConnection.CreateConnection();
                await conn.OpenAsync();
                const string sql = @"
                    SELECT id, uid, file_path, printer_name, description, created_at, updated_at
                    FROM card_mappings
                    WHERE uid = @uid";
                await using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("uid", uid);

                await using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return new CardMapping
                    {
                        Id = reader.GetInt32(0),
                        Uid = reader.GetString(1),
                        FilePath = reader.GetString(2),
                        PrinterName = reader.IsDBNull(3) ? null : reader.GetString(3),
                        Description = reader.IsDBNull(4) ? null : reader.GetString(4),
                        CreatedAt = reader.GetDateTime(5),
                        UpdatedAt = reader.GetDateTime(6)
                    };
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting mapping for UID {Uid}", uid);
                throw;
            }
        }

        public async Task<IEnumerable<CardMapping>> GetAllAsync()
        {
            var list = new List<CardMapping>();
            try
            {
                await using var conn = _dbConnection.CreateConnection();
                await conn.OpenAsync();
                const string sql = "SELECT id, uid, file_path, printer_name, description, created_at, updated_at FROM card_mappings";
                await using var cmd = new NpgsqlCommand(sql, conn);
                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    list.Add(new CardMapping
                    {
                        Id = reader.GetInt32(0),
                        Uid = reader.GetString(1),
                        FilePath = reader.GetString(2),
                        PrinterName = reader.IsDBNull(3) ? null : reader.GetString(3),
                        Description = reader.IsDBNull(4) ? null : reader.GetString(4),
                        CreatedAt = reader.GetDateTime(5),
                        UpdatedAt = reader.GetDateTime(6)
                    });
                }
                return list;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all mappings");
                throw;
            }
        }

        public async Task AddAsync(CardMapping mapping)
        {
            try
            {
                await using var conn = _dbConnection.CreateConnection();
                await conn.OpenAsync();
                const string sql = @"
                    INSERT INTO card_mappings (uid, file_path, printer_name, description, created_at, updated_at)
                    VALUES (@uid, @filePath, @printerName, @description, @createdAt, @updatedAt)";
                await using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("uid", mapping.Uid);
                cmd.Parameters.AddWithValue("filePath", mapping.FilePath);
                cmd.Parameters.AddWithValue("printerName", mapping.PrinterName ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("description", mapping.Description ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("createdAt", DateTime.UtcNow);
                cmd.Parameters.AddWithValue("updatedAt", DateTime.UtcNow);
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding mapping for UID {Uid}", mapping.Uid);
                throw;
            }
        }

        public async Task UpdateAsync(CardMapping mapping)
        {
            try
            {
                await using var conn = _dbConnection.CreateConnection();
                await conn.OpenAsync();
                const string sql = @"
                    UPDATE card_mappings
                    SET file_path = @filePath,
                        printer_name = @printerName,
                        description = @description,
                        updated_at = @updatedAt
                    WHERE id = @id";
                await using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("id", mapping.Id);
                cmd.Parameters.AddWithValue("filePath", mapping.FilePath);
                cmd.Parameters.AddWithValue("printerName", mapping.PrinterName ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("description", mapping.Description ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("updatedAt", DateTime.UtcNow);
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating mapping for UID {Uid}", mapping.Uid);
                throw;
            }
        }

        public async Task DeleteAsync(int id)
        {
            try
            {
                await using var conn = _dbConnection.CreateConnection();
                await conn.OpenAsync();
                const string sql = "DELETE FROM card_mappings WHERE id = @id";
                await using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("id", id);
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting mapping with Id {Id}", id);
                throw;
            }
        }
    }
}