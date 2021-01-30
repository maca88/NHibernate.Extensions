using System;
using System.Threading.Tasks;

namespace NHibernate.Extensions
{
    internal class TransactionSubscription : ITransactionSubscription
    {
        public Action<ISession> BeforeCommitAction { get; private set; }

        public Func<ISession, Task> BeforeCommitAsyncAction { get; private set; }

        public Action<ISession, bool> AfterCommitAction { get; private set; }

        public Func<ISession, bool, Task> AfterCommitAsyncAction { get; private set; }

        public ITransactionSubscription AfterCommit(Action<ISession, bool> action)
        {
            AfterCommitAction = action;
            return this;
        }

        public ITransactionSubscription AfterCommit(Func<ISession, bool, Task> action)
        {
            AfterCommitAsyncAction = action;
            return this;
        }

        public ITransactionSubscription AfterCommit(Action<bool> action)
        {
            AfterCommitAction = (session, success) => action(success);
            return this;
        }

        public ITransactionSubscription AfterCommit(Func<bool, Task> action)
        {
            AfterCommitAsyncAction = (session, success) => action(success);
            return this;
        }

        public ITransactionSubscription BeforeCommit(Action<ISession> action)
        {
            BeforeCommitAction = action;
            return this;
        }

        public ITransactionSubscription BeforeCommit(Func<ISession, Task> action)
        {
            BeforeCommitAsyncAction = action;
            return this;
        }

        public ITransactionSubscription BeforeCommit(System.Action action)
        {
            BeforeCommitAction = session => action();
            return this;
        }

        public ITransactionSubscription BeforeCommit(Func<Task> action)
        {
            BeforeCommitAsyncAction = session => action();
            return this;
        }

        public bool IsSet => BeforeCommitAction != null || BeforeCommitAsyncAction != null ||
                             AfterCommitAction != null || AfterCommitAsyncAction != null;
    }
}
