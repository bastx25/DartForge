using DartForge.EF.Models;
using DTC.Api.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace DartForge.EF.DAL
{
    public class DFDbContext : IdentityDbContext<ApplicationUser>
    {
        public DFDbContext(DbContextOptions<DFDbContext> dbContextOptions) : base(dbContextOptions)
        {
        }

        public virtual DbSet<ApplicationUser> ApplicationUsers { get; set; }
        public virtual DbSet<Availability> Availabilities { get; set; }
        public virtual DbSet<Board> Boards { get; set; }
        public virtual DbSet<Bracket> Brackets { get; set; }
        public virtual DbSet<Group> Groups { get; set; }
        public virtual DbSet<Location> Locations { get; set; }
        public virtual DbSet<Match> Matches { get; set; }
        public virtual DbSet<MatchParticipant> MatchParticipants { get; set; }
        public virtual DbSet<Player> Players { get; set; }
        public virtual DbSet<RefreshToken> RefreshTokens { get; set; }
        public virtual DbSet<Tournament> Tournaments { get; set; }
        public virtual DbSet<TournamentConfig> TournamentConfigs { get; set; }
        public virtual DbSet<TournamentPlayer> TournamentPlayers { get; set; }


    }
}
