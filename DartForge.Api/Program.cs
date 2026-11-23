using DartForge.Api.Infrastructure;
using DartForge.Api.Interfaces;
using DartForge.Api.Services;
using DartForge.EF.DAL;
using DartForge.EF.Models;
using Mapster;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers().AddNewtonsoftJson(options =>
    options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore
);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<DFDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
);

// ASP.NET Core Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<DFDbContext>()
.AddDefaultTokenProviders();

// JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"] ?? "DartTournamentCreatorSuperSecretKeyForJwtAuthentication2026!";
var issuer = builder.Configuration["Jwt:Issuer"] ?? "DartTournamentCreator";
var audience = builder.Configuration["Jwt:Audience"] ?? "DartTournamentCreatorClient";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = issuer,
        ValidAudience = audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
            "http://localhost:5173",
            "http://localhost:49622",
            "https://dartdashboard.landjugend-lasberg.at")
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials();
    });
});

// Services
builder.Services.AddScoped<IMatchMakerService, MatchMakerService>();
builder.Services.AddScoped<IBoardService, BoardService>();
builder.Services.AddScoped<ITournamentCodeGenerator, TournamentCodeGenerator>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ITournamentAuthorizationService, TournamentAuthorizationService>();
builder.Services.AddScoped<IPublicTournamentService, PublicTournamentService>();
builder.Services.AddMapster(); // Registriert TypeAdapterConfig & IMapper
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped(typeof(ICrudService<,>), typeof(CrudService<,>));
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();


var app = builder.Build();

// Automatically apply pending database migrations and seed default user/codes if available
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<DFDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var codeGenerator = scope.ServiceProvider.GetRequiredService<ITournamentCodeGenerator>();

        if (db.Database.IsRelational())
        {
            db.Database.Migrate();
        }

        // Ensure initial user exists so legacy tournaments have an owner
        var defaultUser = userManager.FindByEmailAsync("admin@dartturnier.local").GetAwaiter().GetResult();
        if (defaultUser == null)
        {
            defaultUser = new ApplicationUser
            {
                UserName = "admin@dartturnier.local",
                Email = "admin@dartturnier.local",
                FirstName = "Admin",
                LastName = "System",
                EmailConfirmed = true,
                CreatedAt = DateTimeOffset.UtcNow
            };
            userManager.CreateAsync(defaultUser, "Admin123!").GetAwaiter().GetResult();
        }

        // Migrate any tournaments lacking OwnerId or Code
        var legacyTournaments = db.Tournaments
            .Where(t => string.IsNullOrEmpty(t.OwnerId) || string.IsNullOrEmpty(t.Code))
            .ToList();

        if (legacyTournaments.Any())
        {
            foreach (var t in legacyTournaments)
            {
                if (string.IsNullOrEmpty(t.OwnerId))
                {
                    t.OwnerId = defaultUser.Id;
                }
                if (string.IsNullOrEmpty(t.Code))
                {
                    t.Code = codeGenerator.GenerateUniqueTournamentCodeAsync().GetAwaiter().GetResult();
                }
            }
            db.SaveChanges();
        }

        // Migrate any legacy players, locations or boards lacking OwnerId
        var legacyPlayers = db.Players.Where(p => string.IsNullOrEmpty(p.OwnerId)).ToList();
        foreach (var p in legacyPlayers) p.OwnerId = defaultUser.Id;

        var legacyLocations = db.Locations.Where(l => string.IsNullOrEmpty(l.OwnerId)).ToList();
        foreach (var l in legacyLocations) l.OwnerId = defaultUser.Id;

        var legacyBoards = db.Boards.Where(b => string.IsNullOrEmpty(b.OwnerId)).ToList();
        foreach (var b in legacyBoards) b.OwnerId = defaultUser.Id;

        if (legacyPlayers.Any() || legacyLocations.Any() || legacyBoards.Any())
        {
            db.SaveChanges();
        }
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning(ex, "Initialisierung / Migration der Datenbank konnte beim Start nicht abgeschlossen werden.");
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
