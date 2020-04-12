using System;
using Microsoft.EntityFrameworkCore;

namespace NetExtensions
{
    public static class ConnectionFactory
    {
        public static TR Using<T, TR>(T disposable
            , Func<T, TR> func) where T : IDisposable
        {
            using var d = disposable;
            return func(d);
        }

        public static void Using<T>(T disposable
            , Action<T> func) where T : IDisposable
        {
            using var d = disposable;
            func(d);
        }

        public static void Execute<TContext>
            (DbContextOptions<TContext> options, Action<TContext> action) where TContext : DbContext, new() =>
            Using(CreateContext(options), action);

        public static TR Execute<TContext, TR>(DbContextOptions<TContext> options, Func<TContext, TR> func) where TContext : DbContext, new() => Using(CreateContext(options), func);

        public static TContext CreateContext<TContext>(DbContextOptions<TContext> options) where TContext : DbContext, new() => (TContext) Activator.CreateInstance(typeof(TContext), options);
    }
}