using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NHibernate.Extensions
{
    internal class TransactionSubscription : ITransactionSubscription
    {
        public Action<ISession> BeforeCommitAction { get; private set; }

        public Action<ISession, bool> AfterCommitAction { get; private set; }

        public ITransactionSubscription AfterCommit(Action<ISession, bool> action)
        {
            AfterCommitAction = action;
            return this;
        }

        public ITransactionSubscription AfterCommit(Action<bool> action)
        {
            AfterCommitAction = (session, success) => action(success);
            return this;
        }

        public ITransactionSubscription BeforeCommit(Action<ISession> action)
        {
            BeforeCommitAction = action;
            return this;
        }

        public ITransactionSubscription BeforeCommit(System.Action action)
        {
            BeforeCommitAction = session => action();
            return this;
        }
    }
}
