using System.ComponentModel.DataAnnotations.Schema;

namespace DTC.Api.Models
{
    [Table("Players")]
    public class Player
    {
        public int Id { get; set; }
        public string OwnerId { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string? LastName { get; set; }
        public string? Nickname { get; set; }

        //Navigation
        public ApplicationUser Owner { get; set; } = null!;
        public ICollection<TournamentPlayer> TournamentPlayers { get; set; } = new List<TournamentPlayer>();
    }
}
