using System.Collections.Generic;
using System.Threading.Tasks;
using RfidPrint.Common.Models;

namespace RfidPrint.Common.Interfaces
{
    public interface ICardMappingRepository
    {
        Task<CardMapping?> GetByUidAsync(string uid);
        Task<IEnumerable<CardMapping>> GetAllAsync();
        Task AddAsync(CardMapping mapping);
        Task UpdateAsync(CardMapping mapping);
        Task DeleteAsync(int id);
    }
}