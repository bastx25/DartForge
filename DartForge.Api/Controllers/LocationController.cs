using DartForge.Api.Infrastructure;
using DTC.Api.Dtos.Location;
using DTC.Api.Models;
using DTC.Api.Services;
using MapsterMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DartForge.Api.Controllers;

[Authorize]
[Route("api/locations")]
[ApiController]
public class LocationController : ControllerBase
{
    private readonly IRepository<Location> _locationRepo;
    private readonly IMapper _mapper;

    public LocationController(IRepository<Location> locationRepo, IMapper mapper)
    {
        _locationRepo = locationRepo;
        _mapper = mapper;
    }

    private string GetCurrentOwnerId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("Benutzer ist nicht authentifiziert.");
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<LocationDto>>> GetAll(
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentOwnerId();
        var locations = await _locationRepo.GetAllAsync(
            predicate: l => l.OwnerId == userId,
            cancellationToken: cancellationToken);

        return Ok(locations.AdaptTraced<List<LocationDto>>(_mapper));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<LocationDto>> GetById(
        [FromRoute] int id,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentOwnerId();
        var location = await _locationRepo.GetByKeyAsync(cancellationToken, id);
        if (location is null || location.OwnerId != userId)
        {
            return NotFound(new { message = "Austragungsort nicht gefunden oder Zugriff verweigert." });
        }

        return Ok(location.AdaptTraced<LocationDto>(_mapper));
    }

    [HttpPost]
    public async Task<ActionResult<LocationDto>> Create(
        [FromBody] CreateLocationDto dto,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentOwnerId();
        var entity = dto.AdaptTraced<Location>(_mapper);
        entity.OwnerId = userId;

        var created = await _locationRepo.AddAsync(entity, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created.AdaptTraced<LocationDto>(_mapper));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        [FromRoute] int id,
        [FromBody] UpdateLocationDto dto,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentOwnerId();
        var existing = await _locationRepo.GetByKeyAsync(cancellationToken, id);
        if (existing is null || existing.OwnerId != userId)
        {
            return NotFound(new { message = "Austragungsort nicht gefunden oder Zugriff verweigert." });
        }

        Location? updatedLocation = null;
        await _locationRepo.UpdateAsync(entity =>
        {
            dto.AdaptTraced(entity, _mapper);
            updatedLocation = entity;
        }, cancellationToken, id);

        return Ok(updatedLocation!.AdaptTraced<LocationDto>(_mapper));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(
        [FromRoute] int id,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentOwnerId();
        var existing = await _locationRepo.GetByKeyAsync(cancellationToken, id);
        if (existing is null || existing.OwnerId != userId)
        {
            return NotFound(new { message = "Austragungsort nicht gefunden oder Zugriff verweigert." });
        }

        await _locationRepo.DeleteAsync(cancellationToken, id);
        return NoContent();
    }
}