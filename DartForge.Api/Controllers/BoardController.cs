using DartForge.Api.Infrastructure;
using DartForge.Api.Interfaces;
using DartForge.EF.Models;
using DTC.Api.Dtos.Board;
using DTC.Api.Interfaces;
using DTC.Api.Models;
using DTC.Api.Services;
using MapsterMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DartForge.Api.Controllers;

[Authorize]
[Route("api/boards")]
[ApiController]
public class BoardController : ControllerBase
{
    private readonly IRepository<Board> _boardRepo;
    private readonly ITournamentAuthorizationService _authService;
    private readonly IMapper _mapper;

    public BoardController(
        IRepository<Board> boardRepo,
        ITournamentAuthorizationService authService,
        IMapper mapper)
    {
        _boardRepo = boardRepo;
        _authService = authService;
        _mapper = mapper;
    }

    private string GetCurrentOwnerId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("Benutzer ist nicht authentifiziert.");
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<BoardDto>>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentOwnerId();
        var boards = await _boardRepo.GetAllAsync(
            predicate: b => b.OwnerId == userId,
            cancellationToken: cancellationToken);

        return Ok(boards.AdaptTraced<List<BoardDto>>(_mapper));
    }

    [HttpGet("location/{locationId:int}")]
    public async Task<ActionResult<IEnumerable<BoardDto>>> GetByLocation(
        [FromRoute] int locationId,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentOwnerId();
        var hasLocationAccess = await _authService.HasLocationAccessAsync(locationId, userId);
        if (!hasLocationAccess)
        {
            return NotFound(new { message = "Austragungsort nicht gefunden oder Zugriff verweigert." });
        }

        var boards = await _boardRepo.GetAllAsync(
            predicate: b => b.LocationId == locationId && b.OwnerId == userId,
            cancellationToken: cancellationToken);

        return Ok(boards.AdaptTraced<List<BoardDto>>(_mapper));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<BoardDto>> GetById(
        [FromRoute] int id,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentOwnerId();
        var board = await _boardRepo.GetByKeyAsync(cancellationToken, id);
        if (board is null || board.OwnerId != userId)
        {
            return NotFound(new { message = "Board nicht gefunden oder Zugriff verweigert." });
        }

        return Ok(board.AdaptTraced<BoardDto>(_mapper));
    }

    [HttpPost]
    public async Task<ActionResult<BoardDto>> Create(
        [FromBody] CreateBoardDto dto,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentOwnerId();

        var hasLocationAccess = await _authService.HasLocationAccessAsync(dto.LocationId, userId);
        if (!hasLocationAccess)
        {
            return BadRequest(new { message = "Der ausgewählte Austragungsort existiert nicht oder gehört nicht zu deinem Benutzerkonto." });
        }

        var entity = dto.AdaptTraced<Board>(_mapper);
        entity.OwnerId = userId;

        try
        {
            var created = await _boardRepo.AddAsync(entity, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created.AdaptTraced<BoardDto>(_mapper));
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        [FromRoute] int id,
        [FromBody] UpdateBoardDto dto,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentOwnerId();
        var existing = await _boardRepo.GetByKeyAsync(cancellationToken, id);
        if (existing is null || existing.OwnerId != userId)
        {
            return NotFound(new { message = "Board nicht gefunden oder Zugriff verweigert." });
        }

        var hasLocationAccess = await _authService.HasLocationAccessAsync(dto.LocationId, userId);
        if (!hasLocationAccess)
        {
            return BadRequest(new { message = "Der ausgewählte Austragungsort existiert nicht oder gehört nicht zu deinem Benutzerkonto." });
        }

        Board? updatedEntity = null;
        await _boardRepo.UpdateAsync(entity =>
        {
            dto.AdaptTraced(entity, _mapper);
            updatedEntity = entity;
        }, cancellationToken, id);

        return Ok(updatedEntity!.AdaptTraced<BoardDto>(_mapper));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(
        [FromRoute] int id,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentOwnerId();
        var existing = await _boardRepo.GetByKeyAsync(cancellationToken, id);
        if (existing is null || existing.OwnerId != userId)
        {
            return NotFound(new { message = "Board nicht gefunden oder Zugriff verweigert." });
        }

        await _boardRepo.DeleteAsync(cancellationToken, id);
        return NoContent();
    }
}