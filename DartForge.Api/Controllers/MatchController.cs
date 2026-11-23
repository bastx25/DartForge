using DartForge.Api.Infrastructure;
using DartForge.Api.Interfaces;
using DartForge.Shared.Enums;
using DTC.Api.Dtos.Match;
using DTC.Api.Enums;
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
[Route("api/matches")]
[ApiController]
public class MatchController : ControllerBase
{
    private readonly IRepository<Match> _matchRepo;
    private readonly IMatchMakerService _matchMakerService;
    private readonly ITournamentAuthorizationService _authService;
    private readonly IMapper _mapper;

    public MatchController(
        IRepository<Match> matchRepo,
        IMatchMakerService matchMakerService,
        ITournamentAuthorizationService authService,
        IMapper mapper)
    {
        _matchRepo = matchRepo;
        _matchMakerService = matchMakerService;
        _authService = authService;
        _mapper = mapper;
    }

    private string GetCurrentUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("Benutzer ist nicht authentifiziert.");
    }

    [HttpGet("braket/{braketId:int}")]
    public async Task<ActionResult<IEnumerable<MatchDto>>> GetByBraketId(
        [FromRoute] int braketId,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        var hasAccess = await _authService.HasBraketAccessAsync(braketId, userId);
        if (!hasAccess)
        {
            return NotFound(new { message = "Runde nicht gefunden oder Zugriff verweigert." });
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

    [HttpGet("{id:int}")]
    public async Task<ActionResult<MatchDto>> GetById(
        [FromRoute] int id,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        var hasAccess = await _authService.HasMatchAccessAsync(id, userId);
        if (!hasAccess)
        {
            return NotFound(new { message = "Match nicht gefunden oder Zugriff verweigert." });
        }

        var match = await _matchRepo.GetByKeyAsync(
            include: q => q
                .Include(m => m.Board)
                .Include(m => m.Participants)
                    .ThenInclude(p => p.TournamentPlayer)
                        .ThenInclude(tp => tp.Player),
            asNoTracking: true,
            splitQuery: true,
            cancellationToken: cancellationToken,
            id);

        if (match is null)
        {
            return NotFound();
        }

        return Ok(match.AdaptTraced<MatchDto>(_mapper));
    }

    [HttpPost]
    public async Task<ActionResult<MatchDto>> Create(
        [FromBody] CreateMatchDto dto,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (dto.BoardId.HasValue)
        {
            var hasBoardAccess = await _authService.HasBoardAccessAsync(dto.BoardId.Value, userId);
            if (!hasBoardAccess)
            {
                return BadRequest(new { message = "Das ausgewählte Board existiert nicht oder gehört nicht zu deinem Benutzerkonto." });
            }
        }

        var entity = dto.AdaptTraced<Match>(_mapper);
        var createdMatch = await _matchRepo.AddAsync(entity, cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = createdMatch.Id },
            createdMatch.AdaptTraced<MatchDto>(_mapper));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        [FromRoute] int id,
        [FromBody] UpdateMatchDto dto,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        var hasAccess = await _authService.HasMatchAccessAsync(id, userId);
        if (!hasAccess)
        {
            return NotFound(new { message = "Match nicht gefunden oder Zugriff verweigert." });
        }

        if (dto.BoardId.HasValue)
        {
            var hasBoardAccess = await _authService.HasBoardAccessAsync(dto.BoardId.Value, userId);
            if (!hasBoardAccess)
            {
                return BadRequest(new { message = "Das ausgewählte Board existiert nicht oder gehört nicht zu deinem Benutzerkonto." });
            }
        }

        MatchStatus? updatedStatus = null;
        var updated = await _matchRepo.UpdateAsync(entity =>
        {
            dto.AdaptTraced(entity, _mapper);
            updatedStatus = entity.Status;
        }, cancellationToken, id);

        if (!updated)
        {
            return NotFound();
        }

        if (updatedStatus == MatchStatus.Completed)
        {
            await _matchMakerService.AdvanceKnockoutAsync(id);
        }

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(
        [FromRoute] int id,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        var hasAccess = await _authService.HasMatchAccessAsync(id, userId);
        if (!hasAccess)
        {
            return NotFound(new { message = "Match nicht gefunden oder Zugriff verweigert." });
        }

        var success = await _matchRepo.DeleteAsync(cancellationToken, id);
        if (!success)
        {
            return NotFound();
        }

        return NoContent();
    }
}