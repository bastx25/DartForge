using DartForge.Shared.Enums;
using DTC.Api.Models;
using System.ComponentModel.DataAnnotations.Schema;

namespace DartForge.EF.Models
{
    [Table("Tournaments")]
    public class Tournament
    {
        public int Id { get; set; }
        public string OwnerId { get; set; } = string.Empty;
        public int Locationid { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string? Description { get; set; }
        public TournamentStatus Status { get; set; } = TournamentStatus.Draft;
        public DateTimeOffset StartDate { get; set; }

        //Navigation
        public ApplicationUser Owner { get; set; } = null!;
        public Location Location { get; set; } = null!;
        public ICollection<TournamentPlayer> TournamentPlayers { get; set; }
            = new List<TournamentPlayer>();
        public ICollection<Group> Groups { get; set; }
            = new List<Group>();
        public ICollection<Bracket> Brackets { get; set; }
            = new List<Bracket>();
        public ICollection<TournamentConfig> Configurations { get; set; } = new List<TournamentConfig>();
    }
}