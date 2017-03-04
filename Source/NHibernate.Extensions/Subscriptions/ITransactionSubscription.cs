using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NHibernate.Extensions
{
    public interface ITransactionSubscription
    {
        ITransactionSubscription AfterCommit(Action<ISession, bool> action);

        ITransactionSubscription AfterCommit(Action<bool> action);

        ITransactionSubscription BeforeCommit(Action<ISession> action);

        ITransactionSubscription BeforeCommit(System.Action action);
    }
}
