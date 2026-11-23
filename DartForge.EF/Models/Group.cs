using DartForge.EF.Models;
using System.ComponentModel.DataAnnotations.Schema;

namespace DTC.Api.Models
{
    [Table("Groups")]
    public partial class Group
    {
        public int Id { get; set; }
        public int TournamentId { get; set; }
        public int Sequence { get; set; }
        public string Name { get; set; } = string.Empty;

        //Navígation
        public Tournament Tournament { get; set; } = null!;
        public ICollection<TournamentPlayer> TournamentPlayers { get; set; } = new List<TournamentPlayer>();
        public ICollection<Match> Matches { get; set; } = new List<Match>();
    }
}
