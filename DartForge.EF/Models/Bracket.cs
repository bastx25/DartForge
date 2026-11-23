using DartForge.Shared.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace DartForge.EF.Models
{
    [Table("Brackets")]
    public class Bracket
    {
        public int Id { get; set; }
        public int TournamentId { get; set; }
        public int Sequence { get; set; }
        public string? Name { get; set; }
        public DateTimeOffset PlannedStart { get; set; }
        public DateTimeOffset? PlannedEnd { get; set; }
        public BracketStatus Status { get; set; } = BracketStatus.Scheduled;

        //Navigation
        public ICollection<Match> Matches { get; set; }
            = new List<Match>();
        public Tournament Tournament { get; set; } = null!;

    }
}