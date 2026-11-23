using DartForge.Api.Infrastructure;
using DartForge.Api.Interfaces;
using DartForge.EF.Models;
using DTC.Api.Dtos.MatchParticipant;
using DTC.Api.Interfaces;
using DTC.Api.Models;
using DTC.Api.Services;
using MapsterMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DartForge.Api.Controllers;

[Authorize]
[Route("api/matchparticipants")]
[ApiController]
public class MatchParticipantController : ControllerBase
{
    private readonly IRepository<MatchParticipant> _participantRepo;
    private readonly IMatchMakerService _matchMakerService;
    private readonly ITournamentAuthorizationService _authService;
    private readonly IMapper _mapper;

    public MatchParticipantController(
        IRepository<MatchParticipant> participantRepo,
        IMatchMakerService matchMakerService,
        ITournamentAuthorizationService authService,
        IMapper mapper)
    {
        _participantRepo = participantRepo;
        _matchMakerService = matchMakerService;
        _authService = authService;
        _mapper = mapper;
    }

    private string GetCurrentUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("Benutzer ist nicht authentifiziert.");
    }

    [HttpGet("match/{matchId:int}")]
    public async Task<ActionResult<IEnumerable<MatchParticipantDto>>> GetByMatchId(
        [FromRoute] int matchId,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        var hasAccess = await _authService.HasMatchAccessAsync(matchId, userId);
        if (!hasAccess)
        {
            return NotFound(new { message = "Match nicht gefunden oder Zugriff verweigert." });
        }

        var participants = await _participantRepo.GetAllAsync(
            include: q => q
                .Include(p => p.TournamentPlayer)
                    .ThenInclude(tp => tp.Player),
            predicate: p => p.MatchId == matchId,
            cancellationToken: cancellationToken);

        return Ok(participants.AdaptTraced<List<MatchParticipantDto>>(_mapper));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<MatchParticipantDto>> GetById(
        [FromRoute] int id,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        var participant = await _participantRepo.GetByKeyAsync(
            include: q => q
                .Include(p => p.TournamentPlayer)
                    .ThenInclude(tp => tp.Player),
            asNoTracking: true,
            splitQuery: false,
            cancellationToken: cancellationToken,
            id);

        if (participant is null)
        {
            return NotFound();
        }

        var hasAccess = await _authService.HasMatchAccessAsync(participant.MatchId, userId);
        if (!hasAccess)
        {
            return NotFound(new { message = "Teilnehmer nicht gefunden oder Zugriff verweigert." });
        }

        return Ok(participant.AdaptTraced<MatchParticipantDto>(_mapper));
    }

    [HttpPost]
    public async Task<ActionResult<MatchParticipantDto>> Create(
        [FromBody] CreateMatchParticipantDto dto,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        var hasAccess = await _authService.HasMatchAccessAsync(dto.MatchId, userId);
        if (!hasAccess)
        {
            return NotFound(new { message = "Match nicht gefunden oder Zugriff verweigert." });
        }

        var entity = dto.AdaptTraced<MatchParticipant>(_mapper);
        var createdParticipant = await _participantRepo.AddAsync(entity, cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = createdParticipant.Id },
            createdParticipant.AdaptTraced<MatchParticipantDto>(_mapper));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        [FromRoute] int id,
        [FromBody] UpdateMatchParticipantDto dto,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        var existingParticipant = await _participantRepo.GetByKeyAsync(cancellationToken, id);
        if (existingParticipant is null)
        {
            return NotFound();
        }

        var hasAccess = await _authService.HasMatchAccessAsync(existingParticipant.MatchId, userId);
        if (!hasAccess)
        {
            return NotFound(new { message = "Match nicht gefunden oder Zugriff verweigert." });
        }

        await _participantRepo.UpdateAsync(
            entity => dto.AdaptTraced(entity, _mapper),
            cancellationToken,
            id);

        await _matchMakerService.AdvanceKnockoutAsync(existingParticipant.MatchId);

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(
        [FromRoute] int id,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        var existing = await _participantRepo.GetByKeyAsync(cancellationToken, id);
        if (existing is null)
        {
            return NotFound();
        }

        var hasAccess = await _authService.HasMatchAccessAsync(existing.MatchId, userId);
        if (!hasAccess)
        {
            return NotFound(new { message = "Match nicht gefunden oder Zugriff verweigert." });
        }

        var success = await _participantRepo.DeleteAsync(cancellationToken, id);
        if (!success)
        {
            return NotFound();
        }

        return NoContent();
    }
}