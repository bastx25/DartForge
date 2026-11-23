using Mapster;
using System.Linq.Expressions;

namespace DartForge.Api.Infrastructure
{
    public interface ICrudService<TDto, TEntity>
    where TDto : class
    where TEntity : class, new()
    {
        IRepository<TEntity> Repository { get; }

        Task<IReadOnlyList<TDto>> GetAllAsync(
            Func<IQueryable<TEntity>, IQueryable<TEntity>>? include = null,
            Expression<Func<TEntity, bool>>? predicate = null,
            string? sortBy = null,
            bool descending = false,
            int? take = null,
            bool splitQuery = false,
            CancellationToken cancellationToken = default);

        Task<TDto?> GetByKeyAsync(params object[] keys);

        Task<TDto?> GetByKeyAsync(CancellationToken cancellationToken, params object[] keys);

        Task<TDto?> GetByKeyAsync(
            Func<IQueryable<TEntity>, IQueryable<TEntity>>? include,
            bool splitQuery = false,
            CancellationToken cancellationToken = default,
            params object[] keys);

        Task<TDto> AddAsync(TDto dto, CancellationToken cancellationToken = default);

        Task<bool> UpdateAsync(TDto dto, params object[] keys);

        Task<bool> UpdateAsync(TDto dto, CancellationToken cancellationToken, params object[] keys);

        Task<bool> UpdateWithConfigAsync(TDto dto, TypeAdapterConfig config, params object[] keys);

        Task<bool> UpdateWithConfigAsync(
            TDto dto,
            TypeAdapterConfig config,
            CancellationToken cancellationToken,
            params object[] keys);

        Task<bool> DeleteAsync(params object[] keys);

        Task<bool> DeleteAsync(CancellationToken cancellationToken, params object[] keys);
    }
}
