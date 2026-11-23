using System.ComponentModel.DataAnnotations.Schema;

namespace DTC.Api.Models
{
    [Table("Boards")]
    public class Board
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public int LocationId { get; set; }
        public string? Label { get; set; }
        public bool IsActive { get; set; } = true;

        //Navigation
        public ICollection<Match> Matches { get; set; } = new List<Match>();
        public Location Location { get; set; } = null!;
        public ApplicationUser User { get; set; } = null!;
    }
}
