using System.ComponentModel.DataAnnotations.Schema;

namespace DartForge.EF.Models
{
    [Table("Boards")]
    public class Board
    {
        public int Id { get; set; }
        public string OwnerId { get; set; } = string.Empty;
        public int LocationId { get; set; }
        public string? Label { get; set; }
        public bool IsActive { get; set; } = true;

        //Navigation
        public ICollection<Match> Matches { get; set; } = new List<Match>();
        public Location Location { get; set; } = null!;
        public ApplicationUser Owner { get; set; } = null!;
    }
}
