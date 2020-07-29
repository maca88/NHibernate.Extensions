using System;
using System.Threading;
using System.Threading.Tasks;
using NHibernate.Transaction;

namespace NHibernate.Extensions
{
    internal class TransactionListener : ITransactionCompletionSynchronization
    {
        private readonly ISession _session;
        private readonly Action<ISession> _beforeCommit;
        private readonly Action<ISession, bool> _afterCommit;

        public TransactionListener(ISession session, Action<ISession> beforeCommit, Action<ISession, bool> afterCommit)
        {
            _session = session;
            _beforeCommit = beforeCommit;
            _afterCommit = afterCommit;
        }

        public void BeforeCompletion()
        {
            _beforeCommit?.Invoke(_session);
        }

        public void AfterCompletion(bool success)
        {
            _afterCommit?.Invoke(_session, success);
        }

        public void ExecuteBeforeTransactionCompletion()
        {
            _beforeCommit?.Invoke(_session);
        }

        public Task ExecuteBeforeTransactionCompletionAsync(CancellationToken cancellationToken)
        {
            _beforeCommit?.Invoke(_session);
            return Task.CompletedTask;
        }

        public void ExecuteAfterTransactionCompletion(bool success)
        {
            _afterCommit?.Invoke(_session, success);
        }

        public Task ExecuteAfterTransactionCompletionAsync(bool success, CancellationToken cancellationToken)
        {
            _afterCommit?.Invoke(_session, success);
            return Task.CompletedTask;
        }
    }
}
