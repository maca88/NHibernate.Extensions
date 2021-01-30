using System;
using System.Threading;
using System.Threading.Tasks;
using NHibernate.Transaction;

namespace NHibernate.Extensions
{
    internal class TransactionListener : ITransactionCompletionSynchronization
    {
        private readonly ISession _session;
        private readonly TransactionSubscription _subscription;

        public TransactionListener(ISession session, TransactionSubscription subscription)
        {
            _session = session;
            _subscription = subscription;
        }

        public void ExecuteBeforeTransactionCompletion()
        {
            if (_subscription.BeforeCommitAsyncAction != null)
            {
                throw new NotSupportedException("An async before commit action cannot be executed when using ISession.Commit.");
            }

            _subscription.BeforeCommitAction?.Invoke(_session);
        }

        public async Task ExecuteBeforeTransactionCompletionAsync(CancellationToken cancellationToken)
        {
            var action = _subscription.BeforeCommitAsyncAction;
            if (action != null)
            {
                await action(_session).ConfigureAwait(false);
            }
            else
            {
                _subscription.BeforeCommitAction?.Invoke(_session);
            }
        }

        public void ExecuteAfterTransactionCompletion(bool success)
        {
            if (_subscription.AfterCommitAsyncAction != null)
            {
                throw new NotSupportedException("An async after commit action cannot be executed when using ISession.Commit.");
            }

            _subscription.AfterCommitAction?.Invoke(_session, success);
        }

        public async Task ExecuteAfterTransactionCompletionAsync(bool success, CancellationToken cancellationToken)
        {
            var action = _subscription.AfterCommitAsyncAction;
            if (action != null)
            {
                await action(_session, success).ConfigureAwait(false);
            }
            else
            {
                _subscription.AfterCommitAction?.Invoke(_session, success);
            }
        }
    }
}
