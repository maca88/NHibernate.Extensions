using System;

namespace NHibernate.Extensions
{
    public static class SessionSubscriptionExtension
    {
        /// <summary>
        /// Fluently subscribe to various NHiberante session events
        /// </summary>
        /// <param name="session">NHibernate session</param>
        /// <param name="configure">Action for configure the subscriptions</param>
        public static void Subscribe(this ISession session, Action<ISessionSubscription> configure)
        {
            var config = new SessionSubscription();
            configure(config);
            if (config.Transaction is TransactionSubscription transConfig && (transConfig.AfterCommitAction != null || transConfig.BeforeCommitAction != null))
            {
                session.GetCurrentTransaction().RegisterSynchronization(new TransactionListener(session,
                    transConfig.BeforeCommitAction, transConfig.AfterCommitAction));
            }
        }
    }
}
