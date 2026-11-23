using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection.Metadata.Ecma335;

namespace DTC.Api.Models
{
    [Table("TournamentPlayers")]
    public class TournamentPlayer
    {
        public int Id { get; set; }
        public int PlayerId { get; set; }
        public int TournamentId { get; set; }
        public int? GroupId { get; set; }
        public bool IsQualifiedForKnockout { get; set; }

        //Navigation
        public Player Player { get; set; } = null!;
        public Tournament Tournament { get; set; } = null!;
        public Group? Group { get; set; }
        public ICollection<Availability> Availabilities { get; set; } = new List<Availability>();
        public ICollection<MatchParticipant> MatchParticipants { get; set; }
            = new List<MatchParticipant>();
    }
}