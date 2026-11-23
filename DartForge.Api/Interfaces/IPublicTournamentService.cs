using DTC.Api.Dtos.Public;
using DTC.Api.Dtos.Tournament;

namespace DartForge.Api.Interfaces
{
    public interface IPublicTournamentService
    {
        Task<PublicTournamentDto?> GetTournamentByCodeAsync(string code);
        Task<PublicTournamentDto?> GetTournamentByIdAsync(int id);
        Task<IEnumerable<TournamentDto>> GetActivePublicTournamentsAsync();
    }
}
