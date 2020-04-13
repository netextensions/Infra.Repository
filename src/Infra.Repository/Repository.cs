using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace NetExtensions
{
    public class Repository<TContext> where TContext : DbContext, new()
    {
        protected readonly ILogger Logger;
        protected readonly DbContextOptions<TContext> Options;

        public Repository(ILogger<Repository<TContext>> logger, DbContextOptions<TContext> options)
        {
            Logger = logger;
            Options = options;
        }

        public void Execute(Action<TContext> action) => Execute(Options, action);

        public T Execute<T>(Func<TContext, T> func) => Execute(Options, func);

        public async Task ExecuteAsync(Func<TContext, Task> action) => await ExecuteAsync(Options, action);

        public async Task<T> ExecuteAsync<T>(Func<TContext, Task<T>> func) => await ExecuteAsync(Options, func);

        protected static TR Using<T, TR>(T disposable
            , Func<T, TR> func) where T : IDisposable
        {
            using var d = disposable;
            return func(d);
        }

        protected static void Execute(DbContextOptions<TContext> options, Action<TContext> action) =>
            Using(CreateContext(options), action);

        protected static TR Execute<TR>(DbContextOptions<TContext> options, Func<TContext, TR> func) => Using(CreateContext(options), func);

        protected static async Task ExecuteAsync(DbContextOptions<TContext> options, Func<TContext, Task> func) => await UsingAsync(CreateContext(options), func);

        protected static async Task<TR> ExecuteAsync<TR>(DbContextOptions<TContext> options, Func<TContext, Task<TR>> func) => await UsingAsync(CreateContext(options), func);

        protected static async Task UsingAsync<T>(T disposable
            , Func<T, Task> func) where T : IDisposable
        {
            using var d = disposable;
            await func(d);
        }

        protected static async Task<TR> UsingAsync<T, TR>(T disposable
            , Func<T, Task<TR>> func) where T : IDisposable
        {
            using var d = disposable;
            return await func(d);
        }

        protected static TContext CreateContext(DbContextOptions<TContext> options) => (TContext) Activator.CreateInstance(typeof(TContext), options);

        protected static void Using<T>(T disposable
            , Action<T> func) where T : IDisposable
        {
            using var d = disposable;
            func(d);
        }
    }
}