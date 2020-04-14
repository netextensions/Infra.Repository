using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace NetExtensions
{
    public class ContextHandler<TContext> where TContext : DbContext
    {
        protected readonly ILogger Logger;
        protected readonly DbContextOptions<TContext> Options;

        public ContextHandler(ILogger<ContextHandler<TContext>> logger, DbContextOptions<TContext> options)
        {
            Logger = logger;
            Options = options;
        }

        public void Execute(Action<TContext> action) => Execute(Options, action);
        protected static void Execute(DbContextOptions<TContext> options, Action<TContext> action) => Using(CreateContext(options), action);
        protected static void Using<T>(T disposable
            , Action<T> func) where T : IDisposable
        {
            using var d = disposable;
            func(d);
        }

        public T Execute<T>(Func<TContext, T> func) => Execute(Options, func);
        protected static TR Execute<TR>(DbContextOptions<TContext> options, Func<TContext, TR> func) => Using(CreateContext(options), func);
        protected static TR Using<T, TR>(T disposable
            , Func<T, TR> func) where T : IDisposable
        {
            using var d = disposable;
            return func(d);
        }

        public async Task ExecuteAsync(Func<TContext, Task> action) => await ExecuteAsync(Options, action);
        protected static async Task ExecuteAsync(DbContextOptions<TContext> options, Func<TContext, Task> func) => await UsingAsync(CreateContext(options), func);
        protected static async Task UsingAsync<T>(T disposable
            , Func<T, Task> func) where T : IDisposable
        {
            using var d = disposable;
            await func(d);
        }

        public async Task<T> ExecuteAsync<T>(Func<TContext, Task<T>> func) => await ExecuteAsync(Options, func);
        protected static async Task<TR> ExecuteAsync<TR>(DbContextOptions<TContext> options, Func<TContext, Task<TR>> func) => await UsingAsync(CreateContext(options), func);
        protected static async Task<TR> UsingAsync<T, TR>(T disposable, Func<T, Task<TR>> func) where T : IDisposable
        {
            using var d = disposable;
            return await func(d);
        }

        public async Task ExecuteSaveAsync(Func<TContext, Task> action) => await ExecuteSaveAsync(Options, action);
        protected static async Task ExecuteSaveAsync(DbContextOptions<TContext> options, Func<TContext, Task> func) => await UsingSaveAsync(CreateContext(options), func);
        protected static async Task UsingSaveAsync<T>(T disposable
            , Func<T, Task> func) where T : TContext, IDisposable
        {
            await using var d = disposable;
            await func(d);
            await d.SaveChangesAsync();
        }
        
        public async Task<T> ExecuteSaveAsync<T>(Func<TContext, Task<T>> func) => await ExecuteSaveAsync(Options, func);
        protected static async Task<TR> ExecuteSaveAsync<TR>(DbContextOptions<TContext> options, Func<TContext, Task<TR>> func) => await UsingSaveAsync(CreateContext(options), func);
        protected static async Task<TR> UsingSaveAsync<T, TR>(T disposable
            , Func<T, Task<TR>> func) where T : TContext, IDisposable
        {
            await using var d = disposable;
            var r = await func(d);
            await d.SaveChangesAsync();
            return r;
        }
        
        protected static TContext CreateContext(DbContextOptions<TContext> options) => (TContext)Activator.CreateInstance(typeof(TContext), options);
    }
}