using Mapster;
using MapsterMapper;
using System.Collections;
using System.Diagnostics;

namespace DartForge.Api.Infrastructure
{
    internal static class MapsterTracing
    {
        public static TDestination AdaptTraced<TDestination>(this object source, IMapper mapper)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(mapper);

            using var activity = Start(source.GetType(), typeof(TDestination));
            try
            {
                var result = mapper.Map<TDestination>(source);
                if (result is ICollection collection)
                {
                    activity?.SetTag("mapster.row_count", collection.Count);
                }

                return result;
            }
            catch (Exception ex)
            {
                Fail(activity, ex);
                throw;
            }
        }

        public static TDestination AdaptTraced<TSource, TDestination>(
            this TSource source,
            TDestination destination,
            IMapper mapper)
        {
            ArgumentNullException.ThrowIfNull(mapper);

            using var activity = Start(typeof(TSource), typeof(TDestination));
            try
            {
                return mapper.Map(source, destination);
            }
            catch (Exception ex)
            {
                Fail(activity, ex);
                throw;
            }
        }

        public static TDestination AdaptTraced<TSource, TDestination>(
            this TSource source,
            TDestination destination,
            TypeAdapterConfig config)
        {
            ArgumentNullException.ThrowIfNull(config);

            using var activity = Start(typeof(TSource), typeof(TDestination));
            try
            {
                return source.Adapt(destination, config);
            }
            catch (Exception ex)
            {
                Fail(activity, ex);
                throw;
            }
        }

        private static Activity? Start(Type source, Type destination)
        {
            var sourceName = TypeName(source);
            var destinationName = TypeName(destination);
            var activity = RepositoryActivitySource.Instance.StartActivity(
                $"Mapster {sourceName} to {destinationName}",
                ActivityKind.Internal);
            activity?.SetTag("mapster.source", sourceName);
            activity?.SetTag("mapster.destination", destinationName);
            return activity;
        }

        private static void Fail(Activity? activity, Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.AddException(ex);
        }

        private static string TypeName(Type type)
        {
            if (!type.IsGenericType)
            {
                return type.Name;
            }

            var name = type.Name;
            var tick = name.IndexOf('`');
            if (tick >= 0)
            {
                name = name[..tick];
            }

            return $"{name}<{string.Join(",", type.GetGenericArguments().Select(TypeName))}>";
        }
    }
}
