using DartForge.Api.Infrastructure;
using DartForge.Api.Interfaces;
using DartForge.EF.Models;
using DTC.Api.Dtos.Bracket;
using DTC.Api.Dtos.Match;
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
[Route("api/brackets")]
[ApiController]
public class BracketController : ControllerBase
{
    private readonly IRepository<Bracket> _braketRepo;
    private readonly IRepository<Match> _matchRepo;
    private readonly ITournamentAuthorizationService _authService;
    private readonly IMapper _mapper;

    public BracketController(
        IRepository<Bracket> braketRepo,
        IRepository<Match> matchRepo,
        ITournamentAuthorizationService authService,
        IMapper mapper)
    {
        _braketRepo = braketRepo;
        _matchRepo = matchRepo;
        _authService = authService;
        _mapper = mapper;
    }

    private string GetCurrentOwnerId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("Benutzer ist nicht authentifiziert.");
    }

    [HttpGet("tournament/{tournamentId:int}")]
    public async Task<ActionResult<IEnumerable<BracketDto>>> GetByTournament(
        [FromRoute] int tournamentId,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentOwnerId();
        var hasAccess = await _authService.HasTournamentAccessAsync(tournamentId, userId);
        if (!hasAccess)
        {
            return NotFound(new { message = "Turnier nicht gefunden oder Zugriff verweigert." });
        }

        var brakets = await _braketRepo.GetAllAsync(
            predicate: b => b.TournamentId == tournamentId,
            cancellationToken: cancellationToken);

        return Ok(brakets.AdaptTraced<List<BracketDto>>(_mapper));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<BracketDto>> GetById(
        [FromRoute] int id,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentOwnerId();
        var hasAccess = await _authService.HasBracketAccessAsync(id, userId);
        if (!hasAccess)
        {
            return NotFound(new { message = "Bracket nicht gefunden oder Zugriff verweigert." });
        }

        var braket = await _braketRepo.GetByKeyAsync(cancellationToken, id);
        if (braket is null)
        {
            return NotFound();
        }

        return Ok(braket.AdaptTraced<BracketDto>(_mapper));
    }

    [HttpPost]
    public async Task<ActionResult<BracketDto>> Create(
        [FromBody] CreateBracketDto dto,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentOwnerId();
        var hasAccess = await _authService.HasTournamentAccessAsync(dto.TournamentId, userId);
        if (!hasAccess)
        {
            return NotFound(new { message = "Turnier nicht gefunden oder Zugriff verweigert." });
        }

        var entity = dto.AdaptTraced<Bracket>(_mapper);
        var created = await _braketRepo.AddAsync(entity, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created.AdaptTraced<BracketDto>(_mapper));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        [FromRoute] int id,
        [FromBody] UpdateBracketDto dto,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentOwnerId();
        var hasAccess = await _authService.HasBracketAccessAsync(id, userId);
        if (!hasAccess)
        {
            return NotFound(new { message = "Bracket nicht gefunden oder Zugriff verweigert." });
        }

        var updated = await _braketRepo.UpdateAsync(
            entity => dto.AdaptTraced(entity, _mapper),
            cancellationToken,
            id);

        if (!updated)
        {
            return NotFound();
        }

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(
        [FromRoute] int id,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentOwnerId();
        var hasAccess = await _authService.HasBracketAccessAsync(id, userId);
        if (!hasAccess)
        {
            return NotFound(new { message = "Bracket nicht gefunden oder Zugriff verweigert." });
        }

        var success = await _braketRepo.DeleteAsync(cancellationToken, id);
        if (!success)
        {
            return NotFound();
        }

        return NoContent();
    }

    [AllowAnonymous]
    [HttpGet("{id:int}/matches")]
    public async Task<IActionResult> GetMatchesByBracketId(
        [FromRoute] int id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var matches = await _matchRepo.GetAllAsync(
                include: q => q
                    .Include(m => m.Board)
                    .Include(m => m.Participants)
                        .ThenInclude(p => p.TournamentPlayer)
                            .ThenInclude(tp => tp.Player),
                predicate: m => m.BracketId == id,
                splitQuery: true,
                cancellationToken: cancellationToken);

            return Ok(matches.AdaptTraced<List<MatchDto>>(_mapper));
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
}