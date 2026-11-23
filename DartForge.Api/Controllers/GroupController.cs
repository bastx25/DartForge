using DartForge.Api.Infrastructure;
using DartForge.EF.Models;
using DTC.Api.Dtos.Match;
using DTC.Api.Models;
using DTC.Api.Services;
using MapsterMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DartForge.Api.Controllers;

[AllowAnonymous]
[Route("api/groups")]
[ApiController]
public class GroupController : ControllerBase
{
    private readonly IRepository<Match> _matchRepo;
    private readonly IMapper _mapper;

    public GroupController(IRepository<Match> matchRepo, IMapper mapper)
    {
        _matchRepo = matchRepo;
        _mapper = mapper;
    }

    [HttpGet("{id:int}/matches")]
    public async Task<IActionResult> GetMatchesByGroupId(
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
                predicate: m => m.GroupId == id,
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