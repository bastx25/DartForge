using System.ComponentModel.DataAnnotations.Schema;

namespace DartForge.EF.Models
{
    [Table("Locations")]
    public class Location
    {
        public int Id { get; set; }
        public string OwnerId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty; // z. B. "Sporthalle Freistadt"
        public string? Address { get; set; }

        //Navigation
        public ApplicationUser Owner { get; set; } = null!;
        public ICollection<Board> Boards { get; set; } = new List<Board>();
        public ICollection<Tournament> Tournaments { get; set; } = new List<Tournament>();
    }
}
