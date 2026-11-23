using System.ComponentModel.DataAnnotations.Schema;

namespace DTC.Api.Models
{
    public partial class Group
    {
        [NotMapped]
        public int QualifiersCount { get => TournamentPlayers.Where(x => x.IsQualifiedForKnockout).Count(); }
    }
}
