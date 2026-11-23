using DartForge.EF.Models;
using DTC.Api.Models;

namespace DartForge.Api.Interfaces
{
    public interface IBoardService
    {
        Task SetBoards(List<Group> groups, DateTimeOffset startTime, int matchDuration, int breakMinutes, string? userId = null);
        Task SetBoards(List<Braket> brakets, DateTimeOffset startTime, int matchDuration, int breakMinutes, string? userId = null);
        Task<List<Board>> GetActiveBoards(string? userId = null);
    }
}
