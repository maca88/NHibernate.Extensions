using System;
using System.Threading.Tasks;

namespace NHibernate.Extensions
{
    public interface ITransactionSubscription
    {
        ITransactionSubscription AfterCommit(Action<ISession, bool> action);

        ITransactionSubscription AfterCommit(Func<ISession, bool, Task> action);

        ITransactionSubscription AfterCommit(Action<bool> action);

        ITransactionSubscription AfterCommit(Func<bool, Task> action);

        ITransactionSubscription BeforeCommit(Action<ISession> action);

        ITransactionSubscription BeforeCommit(Func<ISession, Task> action);

        ITransactionSubscription BeforeCommit(System.Action action);

        ITransactionSubscription BeforeCommit(Func<Task> action);

    }
}
