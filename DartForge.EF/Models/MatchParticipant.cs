using System.ComponentModel.DataAnnotations.Schema;

namespace DartForge.EF.Models
{
    [Table("MatchParticipants")]
    public class MatchParticipant
    {
        public int Id { get; set; }
        public int MatchId { get; set; }
        public int TournamentPlayerId { get; set; }      
        public int Score { get; set; }
        public bool IsWinner { get; set; }

        //Navigation
        public Match Match { get; set; } = null!;
        public TournamentPlayer TournamentPlayer { get; set; } = null!;
    }
}