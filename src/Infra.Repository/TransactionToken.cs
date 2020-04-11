using System;

namespace NetExtensions
{
    public class TransactionToken
    {
        public TransactionToken(Guid transactionId, Action rollBack, Action commit)
        {
            TransactionId = transactionId;
            RollBack = rollBack;
            Commit = commit;
        }

        public Guid TransactionId { get; }

        public Action RollBack { get; }

        public Action Commit { get; }

    }

    public class TransactionToken<T> : TransactionToken where T : class
    {
        public T Data { get; }

        public TransactionToken(Guid transactionId, Action rollBack, Action commit, T data) : base(transactionId, rollBack, commit)
        {
            Data = data;
        }
    }
}