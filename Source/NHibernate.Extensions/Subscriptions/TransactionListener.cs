using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NHibernate.Transaction;

namespace NHibernate.Extensions
{
    internal class TransactionListener : ISynchronization
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
    }
}
