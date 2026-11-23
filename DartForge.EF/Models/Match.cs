using DartForge.Shared.Enums;
using DTC.Api.Models;
using System.ComponentModel.DataAnnotations.Schema;

namespace DartForge.EF.Models
{
    [Table("Matches")]
    public class Match
    {
        public int Id { get; set; }
        public int BoardId { get; set; }
        public int? BracketId { get; set; }
        public int? GroupId { get; set; }
        public MatchStatus Status { get; set; } = MatchStatus.Scheduled;
        public DateTimeOffset? PlannedStart { get; set; }
        public DateTimeOffset? PlannedEnd { get; set; }

        public DateTimeOffset? ActualStart { get; set; }
        public DateTimeOffset? ActualEnd { get; set; }

        //Navigation
        public ICollection<MatchParticipant> Participants { get; set; } = new List<MatchParticipant>();               
        public Bracket? Bracket { get; set; }        
        public Group? Group { get; set; }         
        public Board Board { get; set; } = null!;
   
    }
}