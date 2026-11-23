using DartForge.Api.Interfaces;
using Microsoft.EntityFrameworkCore;
using DartForge.EF.Models;
using DartForge.EF.DAL;

namespace DartForge.Api.Services
{
    public class TournamentAuthorizationService : ITournamentAuthorizationService
    {
        private readonly DFDbContext _context;

        public TournamentAuthorizationService(DFDbContext context)
        {
            _context = context;
        }

        public async Task<Tournament?> GetAuthorizedTournamentAsync(int tournamentId, string ownerId)
        {
            if (string.IsNullOrWhiteSpace(ownerId)) return null;

            return await _context.Tournaments
                .Include(t => t.Configurations)
                .FirstOrDefaultAsync(t => t.Id == tournamentId && t.OwnerId == ownerId);
        }

        public async Task<bool> HasTournamentAccessAsync(int tournamentId, string ownerId)
        {
            if (string.IsNullOrWhiteSpace(ownerId)) return false;

            return await _context.Tournaments
                .AnyAsync(t => t.Id == tournamentId && t.OwnerId == ownerId);
        }

        public async Task<bool> HasGroupAccessAsync(int groupId, string ownerId)
        {
            if (string.IsNullOrWhiteSpace(ownerId)) return false;

            return await _context.Groups
                .AnyAsync(g => g.Id == groupId && g.Tournament.OwnerId == ownerId);
        }

        public async Task<bool> HasBraketAccessAsync(int braketId, string ownerId)
        {
            if (string.IsNullOrWhiteSpace(ownerId)) return false;

            return await _context.Brackets
                .AnyAsync(b => b.Id == braketId && b.Tournament.OwnerId == ownerId);
        }

        public async Task<bool> HasMatchAccessAsync(int matchId, string ownerId)
        {
            if (string.IsNullOrWhiteSpace(ownerId)) return false;

            return await _context.Matches
                .AnyAsync(m => m.Id == matchId && (
                    (m.Group != null && m.Group.Tournament.OwnerId == ownerId) ||
                    (m.Bracket != null && m.Bracket.Tournament.OwnerId == ownerId)));
        }

        public async Task<bool> HasTournamentPlayerAccessAsync(int tournamentPlayerId, string ownerId)
        {
            if (string.IsNullOrWhiteSpace(ownerId)) return false;

            return await _context.TournamentPlayers
                .AnyAsync(tp => tp.Id == tournamentPlayerId && tp.Tournament.OwnerId == ownerId);
        }

        public async Task<bool> HasPlayerAccessAsync(int playerId, string ownerId)
        {
            if (string.IsNullOrWhiteSpace(ownerId)) return false;

            return await _context.Players
                .AnyAsync(p => p.Id == playerId && p.OwnerId == ownerId);
        }

        public async Task<bool> HasAllPlayersAccessAsync(IEnumerable<int> playerIds, string ownerId)
        {
            if (string.IsNullOrWhiteSpace(ownerId)) return false;

            var idList = playerIds?.Distinct().ToList() ?? new List<int>();
            if (idList.Count == 0) return true;

            var matchingCount = await _context.Players
                .CountAsync(p => idList.Contains(p.Id) && p.OwnerId == ownerId);

            return matchingCount == idList.Count;
        }

        public async Task<bool> HasLocationAccessAsync(int locationId, string ownerId)
        {
            if (string.IsNullOrWhiteSpace(ownerId)) return false;

            return await _context.Locations
                .AnyAsync(l => l.Id == locationId && l.OwnerId == ownerId);
        }

        public async Task<bool> HasBoardAccessAsync(int boardId, string ownerId)
        {
            if (string.IsNullOrWhiteSpace(ownerId)) return false;

            return await _context.Boards
                .AnyAsync(b => b.Id == boardId && b.OwnerId == ownerId);
        }

        public async Task<int> GetActiveBoardsCountAsync(string ownerId)
        {
            if (string.IsNullOrWhiteSpace(ownerId)) return 0;

            return await _context.Boards
                .CountAsync(b => b.IsActive && b.OwnerId == ownerId);
        }

        public async Task<int> GetLocationsCountAsync(string ownerId)
        {
            if (string.IsNullOrWhiteSpace(ownerId)) return 0;

            return await _context.Locations
                .CountAsync(l => l.OwnerId == ownerId);
        }
    }
}
