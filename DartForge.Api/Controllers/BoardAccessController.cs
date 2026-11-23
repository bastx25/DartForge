using DartForge.Api.Infrastructure;
using DartForge.Api.Interfaces;
using DartForge.EF.Models;
using DartForge.Shared.Enums;
using MapsterMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DartForge.Api.Controllers;

// Public endpoints used by the QR code on a dart board.
[AllowAnonymous]
[Route("api/board-access")]
[ApiController]
public class BoardAccessController : ControllerBase
{
    private readonly IRepository<Board> _boardRepo;
    private readonly IRepository<Match> _matchRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMatchMakerService _matchMakerService;
    private readonly IMapper _mapper;

    public BoardAccessController(
        IRepository<Board> boardRepo,
        IRepository<Match> matchRepo,
        IUnitOfWork unitOfWork,
        IMatchMakerService matchMakerService,
        IMapper mapper)
    {
        _boardRepo = boardRepo;
        _matchRepo = matchRepo;
        _unitOfWork = unitOfWork;
        _matchMakerService = matchMakerService;
        _mapper = mapper;
    }

    [HttpGet("boards")]
    [HttpGet("/api/public/boards")]
    public async Task<ActionResult<IEnumerable<PublicBoardDto>>> GetAllPublicBoards(
        CancellationToken cancellationToken = default)
    {
        var boards = await _boardRepo.GetAllAsync(
            include: q => q.Include(b => b.Location),
            sortBy: nameof(Board.Id),
            cancellationToken: cancellationToken);

        var result = boards.Select(b => new PublicBoardDto
        {
            Id = b.Id,
            Label = b.Label,
            LocationId = b.LocationId,
            LocationName = b.Location?.Name,
            IsActive = b.IsActive
        });

        return Ok(result);
    }

    [HttpGet("boards/{boardId:int}")]
    public async Task<ActionResult<BoardDto>> GetBoard(
        [FromRoute] int boardId,
        CancellationToken cancellationToken = default)
    {
        var board = await _boardRepo.GetByKeyAsync(cancellationToken, boardId);
        if (board is null || !board.IsActive)
        {
            return NotFound();
        }

        return Ok(board.AdaptTraced<BoardDto>(_mapper));
    }

    [HttpGet("boards/{boardId:int}/matches")]
    public async Task<ActionResult<IEnumerable<BoardMatchDto>>> GetBoardMatches(
        [FromRoute] int boardId,
        CancellationToken cancellationToken = default)
    {
        var board = await _boardRepo.GetByKeyAsync(cancellationToken, boardId);
        if (board is null || !board.IsActive)
        {
            return NotFound();
        }

        var matches = await _matchRepo.GetAllAsync(
            include: q => q
                .Include(m => m.Participants)
                    .ThenInclude(p => p.TournamentPlayer)
                        .ThenInclude(tp => tp.Player),
            predicate: m => m.BoardId == boardId,
            sortBy: nameof(Match.PlannedStart),
            splitQuery: true,
            cancellationToken: cancellationToken);

        var result = matches
            .OrderBy(m => m.PlannedStart)
            .ThenBy(m => m.Id)
            .Select(m => new BoardMatchDto
            {
                MatchId = m.Id,
                PlannedStart = m.PlannedStart,
                ActualStart = m.ActualStart,
                Status = m.Status,
                Participants = m.Participants
                    .OrderBy(p => p.Id)
                    .Select(p => p.AdaptTraced<MatchParticipantDto>(_mapper))
                    .ToList()
            });

        return Ok(result);
    }

    [HttpPost("boards/{boardId:int}/matches/{matchId:int}/start")]
    public async Task<ActionResult<BoardMatchDto>> StartMatch(
        [FromRoute] int boardId,
        [FromRoute] int matchId,
        CancellationToken cancellationToken = default)
    {
        await using var uow = _unitOfWork;

        // TODO: Set Braket on Running
        var match = await uow.Repository<Match>().GetByKeyAsync(
            include: q => q
                .Include(m => m.Braket)
                    .ThenInclude(r => r!.Tournament)
                .Include(m => m.Participants)
                    .ThenInclude(p => p.TournamentPlayer)
                        .ThenInclude(tp => tp.Player),
            asNoTracking: false,
            splitQuery: true,
            cancellationToken: cancellationToken,
            matchId);

        if (match is null || match.BoardId != boardId)
        {
            return NotFound();
        }

        if (match.Status == MatchStatus.Completed)
        {
            return Conflict("Das Spiel wurde bereits abgeschlossen.");
        }

        if (match.Status == MatchStatus.Cancelled)
        {
            return Conflict("Das Spiel wurde abgebrochen.");
        }

        if (match.Status == MatchStatus.InProgress)
        {
            return Ok(match.AdaptTraced<BoardMatchDto>(_mapper));
        }

        match.Status = MatchStatus.InProgress;
        match.ActualStart = DateTimeOffset.UtcNow;
        await uow.SaveChangesAsync(cancellationToken);

        return Ok(match.AdaptTraced<BoardMatchDto>(_mapper));
    }

    [HttpPost("boards/{boardId:int}/matches/{matchId:int}/finish")]
    public async Task<ActionResult<BoardMatchDto>> FinishMatch(
        [FromRoute] int boardId,
        [FromRoute] int matchId,
        [FromBody] FinishBoardMatchDto dto,
        CancellationToken cancellationToken = default)
    {
        await using var uow = _unitOfWork;

        var match = await uow.Repository<Match>().GetByKeyAsync(
            include: q => q
                .Include(m => m.Braket)
                    .ThenInclude(r => r!.Tournament)
                .Include(m => m.Participants)
                    .ThenInclude(p => p.TournamentPlayer)
                        .ThenInclude(tp => tp.Player),
            asNoTracking: false,
            splitQuery: true,
            cancellationToken: cancellationToken,
            matchId);

        if (match is null || match.BoardId != boardId)
        {
            return NotFound();
        }

        if (match.Status == MatchStatus.Completed)
        {
            return Conflict("Das Spiel wurde bereits abgeschlossen.");
        }

        if (match.Status == MatchStatus.Cancelled)
        {
            return Conflict("Das Spiel wurde abgebrochen.");
        }

        if (match.Participants.Count != 2)
        {
            return Conflict("Ein Spiel muss genau zwei Spieler haben.");
        }

        var submitted = dto.Participants
            .GroupBy(p => p.ParticipantId)
            .ToDictionary(g => g.Key, g => g.Last().Score);

        if (submitted.Count != match.Participants.Count ||
            match.Participants.Any(p => !submitted.ContainsKey(p.Id)))
        {
            return BadRequest("Für beide Spieler muss ein Score übermittelt werden.");
        }

        var first = match.Participants.ElementAt(0);
        var second = match.Participants.ElementAt(1);
        var firstScore = submitted[first.Id];
        var secondScore = submitted[second.Id];

        if (firstScore == secondScore)
        {
            return BadRequest("Ein Unentschieden ist hier nicht zulässig. Bitte einen eindeutigen Gewinner erfassen.");
        }

        first.Score = firstScore;
        second.Score = secondScore;
        first.IsWinner = firstScore > secondScore;
        second.IsWinner = secondScore > firstScore;

        match.Status = MatchStatus.Completed;
        match.ActualEnd = DateTimeOffset.UtcNow;
        match.ActualStart ??= match.ActualEnd;

        try
        {
            if (match.Braket?.Phase == BraketPhase.Knockout)
            {
                await _matchMakerService.AdvanceWinner(match);
            }
        }
        catch
        {
        }

        await uow.SaveChangesAsync(cancellationToken);

        return Ok(match.AdaptTraced<BoardMatchDto>(_mapper));
    }
}