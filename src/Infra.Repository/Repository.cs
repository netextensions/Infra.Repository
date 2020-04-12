using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace NetExtensions
{
    public class Repository<TContext> where TContext : DbContext, new()
    {
        protected readonly ILogger Logger;
        protected readonly DbContextOptions<TContext> Options;
        protected readonly Dictionary<Guid, TContext> Transactions = new Dictionary<Guid, TContext>();

        public Repository(ILogger<Repository<TContext>> logger, DbContextOptions<TContext> options)
        {
            Logger = logger;
            Options = options;
        }

        public void Execute(Action<TContext> action) => Execute(Options, action);

        public T Execute<T>(Func<TContext, T> func) => Execute(Options, func);

        public static TR Using<T, TR>(T disposable
            , Func<T, TR> func) where T : IDisposable
        {
            using var d = disposable;
            return func(d);
        }

        protected static void Execute(DbContextOptions<TContext> options, Action<TContext> action) =>
            Using(CreateContext(options), action);

        protected static TR Execute<TR>(DbContextOptions<TContext> options, Func<TContext, TR> func) => Using(CreateContext(options), func);

        protected static TContext CreateContext(DbContextOptions<TContext> options) => (TContext) Activator.CreateInstance(typeof(TContext), options);

        protected static void Using<T>(T disposable
            , Action<T> func) where T : IDisposable
        {
            using var d = disposable;
            func(d);
        }
    }
}