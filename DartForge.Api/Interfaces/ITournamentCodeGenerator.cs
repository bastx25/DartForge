namespace DartForge.Api.Interfaces
{
    public interface ITournamentCodeGenerator
    {
        Task<string> GenerateUniqueTournamentCodeAsync();
    }
}
