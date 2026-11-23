using Mapster;
using MapsterMapper;
using System.Linq.Expressions;

namespace DartForge.Api.Infrastructure
{
    internal sealed class CrudService<TDto, TEntity> : ICrudService<TDto, TEntity>
    where TDto : class
    where TEntity : class, new()
    {
        public CrudService(IRepository<TEntity> repository, IMapper mapper)
        {
            Repository = repository ?? throw new ArgumentNullException(nameof(repository));
            Mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        public IRepository<TEntity> Repository { get; }

        public IMapper Mapper { get; }

        /// <inheritdoc/>
        public async Task<TDto> AddAsync(TDto dto, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(dto);

            var entity = new TEntity();
            dto.AdaptTraced(entity, Mapper);

            var createdEntity = await Repository.AddAsync(entity, cancellationToken);
            return createdEntity.AdaptTraced<TDto>(Mapper);
        }

        /// <inheritdoc/>
        public Task<bool> DeleteAsync(params object[] keys) =>
            Repository.DeleteAsync(CancellationToken.None, keys);

        /// <inheritdoc/>
        public Task<bool> DeleteAsync(CancellationToken cancellationToken, params object[] keys) =>
            Repository.DeleteAsync(cancellationToken, keys);

        /// <inheritdoc/>
        public async Task<IReadOnlyList<TDto>> GetAllAsync(
            Func<IQueryable<TEntity>, IQueryable<TEntity>>? include = null,
            Expression<Func<TEntity, bool>>? predicate = null,
            string? sortBy = null,
            bool descending = false,
            int? take = null,
            bool splitQuery = false,
            CancellationToken cancellationToken = default)
        {
            var entities = await Repository.GetAllAsync(
                include,
                predicate,
                sortBy,
                descending,
                take,
                splitQuery,
                cancellationToken);

            return entities.AdaptTraced<List<TDto>>(Mapper);
        }

        /// <inheritdoc/>
        public Task<TDto?> GetByKeyAsync(params object[] keys) =>
            GetByKeyAsync(include: null, splitQuery: false, CancellationToken.None, keys);

        /// <inheritdoc/>
        public Task<TDto?> GetByKeyAsync(CancellationToken cancellationToken, params object[] keys) =>
            GetByKeyAsync(include: null, splitQuery: false, cancellationToken, keys);

        /// <inheritdoc/>
        public async Task<TDto?> GetByKeyAsync(
            Func<IQueryable<TEntity>, IQueryable<TEntity>>? include,
            bool splitQuery = false,
            CancellationToken cancellationToken = default,
            params object[] keys)
        {
            var entity = await Repository.GetByKeyAsync(
                include,
                asNoTracking: true,
                splitQuery,
                cancellationToken,
                keys);

            return entity is null ? null : entity.AdaptTraced<TDto>(Mapper);
        }

        /// <inheritdoc/>
        public Task<bool> UpdateAsync(TDto dto, params object[] keys) =>
            UpdateAsync(dto, CancellationToken.None, keys);

        /// <inheritdoc/>
        public Task<bool> UpdateAsync(TDto dto, CancellationToken cancellationToken, params object[] keys)
        {
            ArgumentNullException.ThrowIfNull(dto);
            return Repository.UpdateAsync(entity => dto.AdaptTraced(entity, Mapper), cancellationToken, keys);
        }

        /// <inheritdoc/>
        public Task<bool> UpdateWithConfigAsync(TDto dto, TypeAdapterConfig config, params object[] keys) =>
            UpdateWithConfigAsync(dto, config, CancellationToken.None, keys);

        /// <inheritdoc/>
        public Task<bool> UpdateWithConfigAsync(
            TDto dto,
            TypeAdapterConfig config,
            CancellationToken cancellationToken,
            params object[] keys)
        {
            ArgumentNullException.ThrowIfNull(dto);
            ArgumentNullException.ThrowIfNull(config);
            return Repository.UpdateAsync(entity => dto.AdaptTraced(entity, config), cancellationToken, keys);
        }
    }
}
