using DTC.Api.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace DartForge.EF.DAL
{
    internal class DFDbContext : DbContext
    {
        private static string _dbConnectionName = "ssab_pai";
        private static string _dbConnectionString = null;
        public static string DbConnectionName
        {
            get { return _dbConnectionName; }
            set { _dbConnectionName = value; _dbConnectionString = null; }
        }

        public DFDbContext()
        {
            InitializeConnString();
        }

        private static void InitializeConnString()
        {
            if (_dbConnectionString == null)
            {
                _dbConnectionString = System.Configuration.ConfigurationManager.ConnectionStrings[DbConnectionName].ConnectionString;
            }
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

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder
                    .UseSqlServer(_dbConnectionString);
            }
        }

    }
}
