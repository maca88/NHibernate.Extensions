using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NHibernate.Extensions
{
    internal class SessionSubscription : ISessionSubscription
    {
        public SessionSubscription()
        {
            Transaction = new TransactionSubscription();
        }

        public ITransactionSubscription Transaction { get; }
    }
}
