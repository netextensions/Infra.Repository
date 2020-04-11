using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Transactions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace NetExtensions
{
    public class Repository<TContext> where TContext : DbContext, new()
    {
        private readonly ILogger _logger;
        private readonly DbContextOptions<TContext> _options;
        protected readonly Dictionary<Guid, TContext> Transactions = new Dictionary<Guid, TContext>();

        public Repository(ILogger<Repository<TContext>> logger, DbContextOptions<TContext> options)
        {
            _logger = logger;
            _options = options;
        }

        public void Insert<T>(T data) where T : class
        {
            AddEntry(data);
        }

        public void InsertList<T>(IEnumerable<T> data) where T : class
        {
            Execute(context =>
            {
                foreach (var item in data) context.Entry(item).State = EntityState.Added;
                context.SaveChanges();
            });
        }

        public void Update<T>(T data) where T : class
        {
            AddEntry(data, true);
        }

        public List<T> Get<T>(Expression<Func<T, bool>> filter = null,
            Func<IQueryable<T>, IOrderedQueryable<T>> orderBy = null, string includeProperties = null) where T : class
        {
            return Execute(context =>
            {
                var query = Prepare(filter, includeProperties, context);
                return orderBy != null ? orderBy(query).ToList() : query.ToList();
            });
        }

        private static IQueryable<T> Prepare<T>(Expression<Func<T, bool>> filter, string includeProperties, DbContext context) where T : class
        {
            return SetIncludeProperties(includeProperties, SetFilter(filter, context));
        }

        private static IQueryable<T> SetIncludeProperties<T>(string includeProperties, IQueryable<T> query) where T : class
        {
            return string.IsNullOrWhiteSpace(includeProperties)
                ? query
                : includeProperties.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries).Aggregate(query, (current, includeProperty) => current.Include(includeProperty));
        }

        private static IQueryable<T> SetFilter<T>(Expression<Func<T, bool>> filter, DbContext context) where T : class
        {
            return filter != null ? context.Set<T>().Where(filter) : context.Set<T>();
        }

        protected void AddEntry<T>(T data, bool update = false) where T : class
        {
            Execute(context =>
            {
                context.Entry(data).State = update ? EntityState.Modified : EntityState.Added;
                context.SaveChanges();
            });
        }

        public void Execute(Action<TContext> action) => ConnectionFactory.Create(_options, action);

        public T Execute<T>(Func<TContext, T> func) => ConnectionFactory.Create(_options, func);

        public TransactionToken<T> ExecuteOpenTransaction<T>(Func<TContext, T> func) where T : class
        {
            var context = ConnectionFactory.CreateContext(_options);
            var tran = context.Database.BeginTransaction();
            var transactionId = Guid.NewGuid();
            Transactions.Add(transactionId, context);
            var result = func(context);

            return new TransactionToken<T>(transactionId, () =>
            {
                tran.Rollback();
                context.Dispose();
            }, () =>
            {
                tran.Commit();
                context.Dispose();
            }, result);
        }

        public void ExecuteTransaction(Action<TContext> action)
        {
            Execute(context =>
            {
                using var tran = context.Database.BeginTransaction();
                try
                {
                    action(context);
                    context.SaveChanges();
                    tran.Commit();
                }
                catch (Exception e)
                {
                    _logger.LogError(e, action.Method.ToString());
                    tran.Rollback();
                    throw;
                }
            });
        }

        public void ExecuteInJoinedTransaction(Guid transactionId, Action<TContext> action)
        {
            var transaction = Transactions.FirstOrDefault(x => x.Key == transactionId);
            if (transaction.Equals(default(KeyValuePair<Guid, TContext>)))
            {
                var message = $"ExecuteInJoinedTransaction: transaction id not found: {transactionId}, Action: {action}";
                var transactionException = new TransactionException(message);
                _logger.LogError(transactionException, message);
                throw transactionException;
            }

            try
            {
                action(transaction.Value);
                transaction.Value.SaveChanges();
            }
            catch (Exception e)
            {
                _logger.LogError(e, action.Method.ToString());
                transaction.Value.Database.CurrentTransaction.Rollback();
                throw;
            }
        }

        public T ExecuteInJoinedTransaction<T>(Guid transactionId, Func<TContext, T> func)
        {
            var transaction = Transactions.FirstOrDefault(x => x.Key == transactionId);
            if (transaction.Equals(default(KeyValuePair<Guid, TContext>)))
            {
                var message = $"ExecuteInJoinedTransaction: transaction id not found: {transactionId}, Function: {func}";
                var transactionException = new TransactionException(message);
                _logger.LogError(transactionException, message);
                throw transactionException;
            }

            try
            {
                var result = func(transaction.Value);
                transaction.Value.SaveChanges();
                return result;
            }
            catch (Exception e)
            {
                _logger.LogError(e, func.Method.ToString());
                transaction.Value.Database.CurrentTransaction.Rollback();
                throw;
            }
        }

        public T ExecuteReadUncommitted<T>(Func<TContext, T> func)
        {
            using var tran = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions {IsolationLevel = IsolationLevel.ReadUncommitted});
            var ret = Execute(func);
            tran.Complete();
            return ret;
        }
    }
}