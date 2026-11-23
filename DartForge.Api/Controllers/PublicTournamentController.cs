using DartForge.Api.Infrastructure;
using DartForge.Api.Interfaces;
using DTC.Api.Dtos.Match;
using DTC.Api.Dtos.Public;
using DTC.Api.Dtos.Tournament;
using DTC.Api.Interfaces;
using DTC.Api.Models;
using DTC.Api.Services;
using MapsterMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DartForge.Api.Controllers;

[Route("api/public/tournaments")]
[ApiController]
[AllowAnonymous]
public class PublicTournamentController : ControllerBase
{
    private readonly IPublicTournamentService _publicTournamentService;
    private readonly IRepository<Match> _matchRepo;
    private readonly IMapper _mapper;

    public PublicTournamentController(
        IPublicTournamentService publicTournamentService,
        IRepository<Match> matchRepo,
        IMapper mapper)
    {
        _publicTournamentService = publicTournamentService;
        _matchRepo = matchRepo;
        _mapper = mapper;
    }

    [HttpGet("{code}")]
    public async Task<ActionResult<PublicTournamentDto>> GetByCode([FromRoute] string code)
    {
        var tournament = await _publicTournamentService.GetTournamentByCodeAsync(code);
        if (tournament is null)
        {
            return NotFound(new { message = $"Kein Turnier mit dem Code '{code}' gefunden." });
        }

        return Ok(tournament);
    }

    [HttpGet("by-id/{id:int}")]
    public async Task<ActionResult<PublicTournamentDto>> GetById([FromRoute] int id)
    {
        var tournament = await _publicTournamentService.GetTournamentByIdAsync(id);
        if (tournament is null)
        {
            return NotFound(new { message = $"Turnier mit ID {id} nicht gefunden." });
        }

        return Ok(tournament);
    }

    [HttpGet("active")]
    public async Task<ActionResult<IEnumerable<TournamentDto>>> GetActive()
    {
        var tournaments = await _publicTournamentService.GetActivePublicTournamentsAsync();
        return Ok(tournaments);
    }

    [HttpGet("{code}/groups/{groupId:int}/matches")]
    public async Task<ActionResult<IEnumerable<MatchDto>>> GetGroupMatches(
        [FromRoute] string code,
        [FromRoute] int groupId,
        CancellationToken cancellationToken = default)
    {
        var tournament = await _publicTournamentService.GetTournamentByCodeAsync(code);
        if (tournament is null || !tournament.Groups.Any(g => g.Id == groupId))
        {
            return NotFound(new { message = "Gruppe oder Turnier nicht gefunden." });
        }

        var matches = await _matchRepo.GetAllAsync(
            include: q => q
                .Include(m => m.Board)
                .Include(m => m.Participants)
                    .ThenInclude(p => p.TournamentPlayer)
                        .ThenInclude(tp => tp.Player),
            predicate: m => m.GroupId == groupId,
            splitQuery: true,
            cancellationToken: cancellationToken);

        return Ok(matches.AdaptTraced<List<MatchDto>>(_mapper));
    }

    [HttpGet("{code}/brakets/{braketId:int}/matches")]
    public async Task<ActionResult<IEnumerable<MatchDto>>> GetBraketMatches(
        [FromRoute] string code,
        [FromRoute] int braketId,
        CancellationToken cancellationToken = default)
    {
        var tournament = await _publicTournamentService.GetTournamentByCodeAsync(code);
        if (tournament is null || !tournament.Brakets.Any(b => b.Id == braketId))
        {
            return NotFound(new { message = "Bracket oder Turnier nicht gefunden." });
        }

        var matches = await _matchRepo.GetAllAsync(
            include: q => q
                .Include(m => m.Board)
                .Include(m => m.Participants)
                    .ThenInclude(p => p.TournamentPlayer)
                        .ThenInclude(tp => tp.Player),
            predicate: m => m.BraketId == braketId,
            splitQuery: true,
            cancellationToken: cancellationToken);

        return Ok(matches.AdaptTraced<List<MatchDto>>(_mapper));
    }
}