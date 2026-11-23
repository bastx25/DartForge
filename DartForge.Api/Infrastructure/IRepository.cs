using System.Linq.Expressions;

namespace DartForge.Api.Infrastructure
{
    internal interface IRepository<T> where T : class, new()
    {
        /// <summary>
        /// Gets all matching entities.
        /// </summary>
        Task<IReadOnlyList<T>> GetAllAsync(
            Func<IQueryable<T>, IQueryable<T>>? include = null,
            Expression<Func<T, bool>>? predicate = null,
            string? sortBy = null,
            bool descending = false,
            int? take = null,
            bool splitQuery = false,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets an entity by its primary key (or composite primary key).
        /// </summary>
        Task<T?> GetByKeyAsync(params object[] keys);

        /// <summary>
        /// Gets an entity by its primary key with cancellation support.
        /// </summary>
        Task<T?> GetByKeyAsync(CancellationToken cancellationToken, params object[] keys);

        /// <summary>
        /// Gets an entity by its primary key with optional eager-loaded navigation properties.
        /// </summary>
        Task<T?> GetByKeyAsync(
            Func<IQueryable<T>, IQueryable<T>>? include,
            bool asNoTracking = true,
            bool splitQuery = false,
            CancellationToken cancellationToken = default,
            params object[] keys);

        /// <summary>
        /// Adds a new entity.
        /// </summary>
        Task<T> AddAsync(T entity, CancellationToken cancellationToken = default);

        /// <summary>
        /// Loads the tracked entity by key, applies <paramref name="applyChanges"/>, and saves only modified columns.
        /// </summary>
        Task<bool> UpdateAsync(
            Action<T> applyChanges,
            CancellationToken cancellationToken,
            params object[] keys);

        /// <summary>
        /// Marks a detached entity as modified or saves changes if already tracked.
        /// </summary>
        Task<bool> UpdateAsync(T entity, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes an entity by its primary key (or composite primary key).
        /// </summary>
        Task<bool> DeleteAsync(params object[] keys);

        /// <summary>
        /// Deletes an entity by its primary key with cancellation support.
        /// </summary>
        Task<bool> DeleteAsync(CancellationToken cancellationToken, params object[] keys);
    }
}
