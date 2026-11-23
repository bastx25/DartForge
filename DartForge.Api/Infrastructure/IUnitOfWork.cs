namespace DartForge.Api.Infrastructure
{
    internal interface IUnitOfWork : IAsyncDisposable
    {
        /// <summary>
        /// Returns a repository bound to this unit of work's shared <see cref="SSAB.TCM.EF.DAL.DFDbContext"/>.
        /// </summary>
        IRepository<TEntity> Repository<TEntity>() where TEntity : class, new();

        /// <summary>
        /// Atomically persists all pending changes across all repositories in this unit of work.
        /// </summary>
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
