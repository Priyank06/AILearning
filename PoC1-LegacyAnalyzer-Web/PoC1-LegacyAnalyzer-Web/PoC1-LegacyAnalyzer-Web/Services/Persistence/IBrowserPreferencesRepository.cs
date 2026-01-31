using PoC1_LegacyAnalyzer_Web.Models;

namespace PoC1_LegacyAnalyzer_Web.Services.Persistence
{
    /// <summary>
    /// Persists and retrieves user preferences in browser storage.
    /// </summary>
    public interface IBrowserPreferencesRepository
    {
        Task<UserPreferences> GetAsync(CancellationToken cancellationToken = default);
        Task SaveAsync(UserPreferences preferences, CancellationToken cancellationToken = default);
    }
}