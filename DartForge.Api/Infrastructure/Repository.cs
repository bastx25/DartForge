using DartForge.EF.DAL;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;

namespace DartForge.Api.Infrastructure
{
    public static class RepositoryActivitySource
    {
        public const string Name = "SSAB.TCM.Api";

        public static readonly ActivitySource Instance = new(Name);
    }

    internal sealed class Repository<T> : IRepository<T> where T : class, new()
    {
        private sealed record KeyPropertyMetadata(string Name, Type ClrType, MethodInfo EfPropertyMethod);

        private static readonly ConcurrentDictionary<(string SortBy, bool Descending), Func<IQueryable<T>, IQueryable<T>>> OrderCache = new();
        private static KeyPropertyMetadata[]? _cachedKeyMetadata;

        private readonly IDbContextFactory<DFDbContext>? _contextFactory;
        private readonly DFDbContext? _sharedContext;

        public Repository(IDbContextFactory<DFDbContext> contextFactory)
        {
            _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
        }

        internal Repository(DFDbContext sharedContext)
        {
            _sharedContext = sharedContext ?? throw new ArgumentNullException(nameof(sharedContext));
        }

        private bool IsStandalone => _sharedContext is null;

        /// <inheritdoc/>
        public Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(entity);

            return TraceAsync(nameof(AddAsync), null, async _ =>
            {
                await using var lease = await AcquireContextAsync(cancellationToken);
                var dbSet = lease.Context.Set<T>();

                // Attach statt Add: Bereits existierende Navigation Properties behalten den Status Unchanged,
                // während die neue Haupt-Entität (auch bei bereits gesetztem Primary Key) auf Added gesetzt wird.
                var entry = dbSet.Attach(entity);
                if (entry.State == EntityState.Unchanged)
                {
                    entry.State = EntityState.Added;
                }

                if (lease.OwnsContext)
                {
                    await lease.Context.SaveChangesAsync(cancellationToken);
                }

                return entity;
            });
        }

        /// <inheritdoc/>
        public Task<bool> UpdateAsync(
            Action<T> applyChanges,
            CancellationToken cancellationToken,
            params object[] keys)
        {
            ArgumentNullException.ThrowIfNull(applyChanges);

            return TraceAsync(nameof(UpdateAsync), keys, async activity =>
            {
                await using var lease = await AcquireContextAsync(cancellationToken);
                var dbSet = lease.Context.Set<T>();

                // FindAsync lädt die Entität getrackt in einem einzigen Roundtrip.
                // Der EF Core Change Tracker erkennt dadurch exakt, welche Properties geändert wurden.
                var entity = await dbSet.FindAsync(keys, cancellationToken);
                if (entity is null)
                {
                    activity?.SetTag("db.found", false);
                    return false;
                }

                activity?.SetTag("db.found", true);
                applyChanges(entity);

                if (lease.OwnsContext)
                {
                    await lease.Context.SaveChangesAsync(cancellationToken);
                }

                return true;
            });
        }

        /// <inheritdoc/>
        public Task<bool> UpdateAsync(T entity, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(entity);

            return TraceAsync(nameof(UpdateAsync), null, async _ =>
            {
                await using var lease = await AcquireContextAsync(cancellationToken);
                var entry = lease.Context.Entry(entity);

                if (entry.State == EntityState.Detached)
                {
                    lease.Context.Set<T>().Update(entity);
                }

                if (lease.OwnsContext)
                {
                    await lease.Context.SaveChangesAsync(cancellationToken);
                }

                return true;
            });
        }

        /// <inheritdoc/>
        public Task<bool> DeleteAsync(params object[] keys) =>
            DeleteAsync(CancellationToken.None, keys);

        /// <inheritdoc/>
        public Task<bool> DeleteAsync(CancellationToken cancellationToken, params object[] keys) =>
            TraceAsync(nameof(DeleteAsync), keys, async activity =>
            {
                await using var lease = await AcquireContextAsync(cancellationToken);
                var dbSet = lease.Context.Set<T>();

                var entity = await dbSet.FindAsync(keys, cancellationToken);
                if (entity is null)
                {
                    activity?.SetTag("db.found", false);
                    return false;
                }

                activity?.SetTag("db.found", true);
                dbSet.Remove(entity);

                if (lease.OwnsContext)
                {
                    await lease.Context.SaveChangesAsync(cancellationToken);
                }

                return true;
            });

        /// <inheritdoc/>
        public Task<IReadOnlyList<T>> GetAllAsync(
            Func<IQueryable<T>, IQueryable<T>>? include = null,
            Expression<Func<T, bool>>? predicate = null,
            string? sortBy = null,
            bool descending = false,
            int? take = null,
            bool splitQuery = false,
            CancellationToken cancellationToken = default) =>
            TraceAsync<IReadOnlyList<T>>(nameof(GetAllAsync), null, async activity =>
            {
                await using var lease = await AcquireContextAsync(cancellationToken);
                IQueryable<T> query = lease.Context.Set<T>().AsNoTracking();

                if (include is not null)
                {
                    query = include(query);
                    if (splitQuery)
                    {
                        query = query.AsSplitQuery();
                    }
                }

                if (predicate is not null)
                {
                    query = query.Where(predicate);
                }

                query = ApplyOrder(query, sortBy, descending);

                if (take.HasValue)
                {
                    query = query.Take(take.Value);
                }

                var result = await query.ToListAsync(cancellationToken);
                activity?.SetTag("db.row_count", result.Count);
                return result;
            });

        /// <inheritdoc/>
        public Task<T?> GetByKeyAsync(params object[] keys) =>
            GetByKeyAsync(include: null, asNoTracking: true, splitQuery: false, CancellationToken.None, keys);

        /// <inheritdoc/>
        public Task<T?> GetByKeyAsync(CancellationToken cancellationToken, params object[] keys) =>
            GetByKeyAsync(include: null, asNoTracking: true, splitQuery: false, cancellationToken, keys);

        /// <inheritdoc/>
        public Task<T?> GetByKeyAsync(
            Func<IQueryable<T>, IQueryable<T>>? include,
            bool asNoTracking = true,
            bool splitQuery = false,
            CancellationToken cancellationToken = default,
            params object[] keys) =>
            TraceAsync(nameof(GetByKeyAsync), keys, async activity =>
            {
                await using var lease = await AcquireContextAsync(cancellationToken);
                IQueryable<T> query = lease.Context.Set<T>();

                if (asNoTracking)
                {
                    query = query.AsNoTracking();
                }

                if (include is not null)
                {
                    query = include(query);
                    if (splitQuery)
                    {
                        query = query.AsSplitQuery();
                    }
                }

                var result = await query.FirstOrDefaultAsync(
                    BuildKeyPredicate(lease.Context, keys),
                    cancellationToken);

                activity?.SetTag("db.found", result is not null);
                return result;
            });

        private async ValueTask<ContextLease> AcquireContextAsync(CancellationToken cancellationToken)
        {
            if (_sharedContext is not null)
            {
                return new ContextLease(_sharedContext, ownsContext: false);
            }

            var context = await _contextFactory!.CreateDbContextAsync(cancellationToken);
            return new ContextLease(context, ownsContext: true);
        }

        private static IQueryable<T> ApplyOrder(IQueryable<T> query, string? sortBy, bool descending)
        {
            if (string.IsNullOrWhiteSpace(sortBy))
            {
                return query;
            }

            var cacheKey = (sortBy.Trim().ToLowerInvariant(), descending);
            var orderDelegate = OrderCache.GetOrAdd(cacheKey, static key =>
            {
                var property = typeof(T).GetProperty(
                    key.SortBy,
                    BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

                if (property is null)
                {
                    return static q => q;
                }

                var parameter = Expression.Parameter(typeof(T), "e");
                var keySelector = Expression.Lambda(Expression.Property(parameter, property), parameter);
                var methodName = key.Descending ? nameof(Queryable.OrderByDescending) : nameof(Queryable.OrderBy);

                var orderMethod = typeof(Queryable)
                    .GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .Single(m => m.Name == methodName && m.GetParameters().Length == 2)
                    .MakeGenericMethod(typeof(T), property.PropertyType);

                return q =>
                {
                    var orderCall = Expression.Call(orderMethod, q.Expression, Expression.Quote(keySelector));
                    return q.Provider.CreateQuery<T>(orderCall);
                };
            });

            return orderDelegate(query);
        }

        private static Expression<Func<T, bool>> BuildKeyPredicate(DbContext context, object[] keys)
        {
            var keyMeta = GetKeyMetadata(context);
            if (keys.Length != keyMeta.Length)
            {
                throw new ArgumentException(
                    $"Expected {keyMeta.Length} primary key value(s) for {typeof(T).Name}, but received {keys.Length}.",
                    nameof(keys));
            }

            var param = Expression.Parameter(typeof(T), "e");
            Expression? body = null;

            for (int i = 0; i < keyMeta.Length; i++)
            {
                var meta = keyMeta[i];
                var prop = Expression.Call(meta.EfPropertyMethod, param, Expression.Constant(meta.Name));
                var eq = Expression.Equal(prop, Expression.Constant(keys[i], meta.ClrType));
                body = body is null ? eq : Expression.AndAlso(body, eq);
            }

            return Expression.Lambda<Func<T, bool>>(body!, param);
        }

        private static KeyPropertyMetadata[] GetKeyMetadata(DbContext context)
        {
            if (_cachedKeyMetadata is not null)
            {
                return _cachedKeyMetadata;
            }

            var keyProps = context.Model.FindEntityType(typeof(T))?.FindPrimaryKey()?.Properties
                ?? throw new InvalidOperationException($"No primary key defined for {typeof(T).Name}.");

            var efPropertyMethod = typeof(Microsoft.EntityFrameworkCore.EF)
                .GetMethod(nameof(Microsoft.EntityFrameworkCore.EF.Property), BindingFlags.Public | BindingFlags.Static)
                ?? throw new InvalidOperationException("EF.Property method could not be resolved.");

            _cachedKeyMetadata = keyProps
                .Select(p => new KeyPropertyMetadata(
                    p.Name,
                    p.ClrType,
                    efPropertyMethod.MakeGenericMethod(p.ClrType)))
                .ToArray();

            return _cachedKeyMetadata;
        }

        private async Task<TResult> TraceAsync<TResult>(
            string operation,
            object[]? keys,
            Func<Activity?, Task<TResult>> action)
        {
            using var activity = RepositoryActivitySource.Instance.StartActivity(
                $"{typeof(T).Name}.{operation}",
                ActivityKind.Internal);
            activity?.SetTag("db.entity", typeof(T).Name);
            activity?.SetTag("db.operation", operation);
            activity?.SetTag("db.uow", !IsStandalone);
            if (keys is { Length: > 0 })
            {
                activity?.SetTag("db.entity.key", string.Join(',', keys));
            }

            try
            {
                return await action(activity);
            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                activity?.AddException(ex);
                throw;
            }
        }

        private readonly struct ContextLease : IAsyncDisposable
        {
            public ContextLease(DFDbContext context, bool ownsContext)
            {
                Context = context;
                OwnsContext = ownsContext;
            }

            public DFDbContext Context { get; }

            public bool OwnsContext { get; }

            public ValueTask DisposeAsync() =>
                OwnsContext ? Context.DisposeAsync() : ValueTask.CompletedTask;
        }
    }
}
