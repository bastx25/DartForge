using DartForge.EF.DAL;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace DartForge.Api.Infrastructure
{
    internal sealed class UnitOfWork : IUnitOfWork
    {
        private readonly IDbContextFactory<DFDbContext> _contextFactory;
        private readonly Dictionary<Type, object> _repositories = new();
        private DFDbContext? _context;
        private bool _disposed;

        public UnitOfWork(IDbContextFactory<DFDbContext> contextFactory)
        {
            _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
        }

        /// <inheritdoc/>
        public IRepository<TEntity> Repository<TEntity>() where TEntity : class, new()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            var entityType = typeof(TEntity);
            if (_repositories.TryGetValue(entityType, out var existing))
            {
                return (IRepository<TEntity>)existing;
            }

            _context ??= _contextFactory.CreateDbContext();
            var repository = new Repository<TEntity>(_context);
            _repositories[entityType] = repository;
            return repository;
        }

        /// <inheritdoc/>
        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (_context is null)
            {
                return 0;
            }

            using var activity = RepositoryActivitySource.Instance.StartActivity(
                "UnitOfWork.SaveChangesAsync",
                ActivityKind.Internal);

            try
            {
                var affectedRows = await _context.SaveChangesAsync(cancellationToken);
                activity?.SetTag("db.affected_rows", affectedRows);
                return affectedRows;
            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                activity?.AddException(ex);
                throw;
            }
        }

        /// <inheritdoc/>
        public async ValueTask DisposeAsync()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _repositories.Clear();

            if (_context is not null)
            {
                await _context.DisposeAsync();
                _context = null;
            }
        }
    }
}
