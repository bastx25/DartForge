using DartForge.Api.Infrastructure;
using DartForge.EF.Models;
using DTC.Api.Dtos.Player;
using DTC.Api.Models;
using DTC.Api.Services;
using MapsterMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DartForge.Api.Controllers;

[Authorize]
[Route("api/players")]
[ApiController]
public class PlayerController : ControllerBase
{
    private readonly IRepository<Player> _playerRepo;
    private readonly IMapper _mapper;

    public PlayerController(IRepository<Player> playerRepo, IMapper mapper)
    {
        _playerRepo = playerRepo;
        _mapper = mapper;
    }

    private string GetCurrentUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("Benutzer ist nicht authentifiziert.");
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PlayerDto>>> GetAllPlayers(
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        var players = await _playerRepo.GetAllAsync(
            predicate: p => p.UserId == userId,
            cancellationToken: cancellationToken);

        return Ok(players.AdaptTraced<List<PlayerDto>>(_mapper));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PlayerDto>> GetPlayerById(
        [FromRoute] int id,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        var player = await _playerRepo.GetByKeyAsync(cancellationToken, id);
        if (player is null || player.UserId != userId)
        {
            return NotFound(new { message = "Spieler nicht gefunden oder Zugriff verweigert." });
        }

        return Ok(player.AdaptTraced<PlayerDto>(_mapper));
    }

    [HttpPost]
    public async Task<ActionResult<PlayerDto>> CreatePlayer(
        [FromBody] CreatePlayerDto playerDto,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        var playerEntity = playerDto.AdaptTraced<Player>(_mapper);
        playerEntity.UserId = userId;

        var createdPlayer = await _playerRepo.AddAsync(playerEntity, cancellationToken);
        return Ok(createdPlayer.AdaptTraced<PlayerDto>(_mapper));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdatePlayer(
        [FromRoute] int id,
        [FromBody] UpdatePlayerDto playerDto,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        var existingPlayer = await _playerRepo.GetByKeyAsync(cancellationToken, id);
        if (existingPlayer is null || existingPlayer.UserId != userId)
        {
            return NotFound(new { message = "Spieler nicht gefunden oder Zugriff verweigert." });
        }

        Player? updatedPlayer = null;
        await _playerRepo.UpdateAsync(entity =>
        {
            playerDto.AdaptTraced(entity, _mapper);
            updatedPlayer = entity;
        }, cancellationToken, id);

        return Ok(updatedPlayer!.AdaptTraced<PlayerDto>(_mapper));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeletePlayer(
        [FromRoute] int id,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        var existingPlayer = await _playerRepo.GetByKeyAsync(cancellationToken, id);
        if (existingPlayer is null || existingPlayer.UserId != userId)
        {
            return BadRequest("Spieler konnte nicht gelöscht werden! Ist der Spieler noch in einem Turnier?");
        }

        try
        {
            var success = await _playerRepo.DeleteAsync(cancellationToken, id);
            if (!success)
            {
                return BadRequest("Spieler konnte nicht gelöscht werden! Ist der Spieler noch in einem Turnier?");
            }
        }
        catch (DbUpdateException)
        {
            return BadRequest("Spieler konnte nicht gelöscht werden! Ist der Spieler noch in einem Turnier?");
        }

        return NoContent();
    }
}