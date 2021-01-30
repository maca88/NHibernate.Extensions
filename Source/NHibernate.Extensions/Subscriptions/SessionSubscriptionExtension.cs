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

            var transaction = session.GetCurrentTransaction();
            if (transaction == null && config.Transaction.IsSet)
            {
                throw new InvalidOperationException("The session has not an active transaction to subscribe the before/after commit actions.");
            }

            transaction.RegisterSynchronization(new TransactionListener(session, config.Transaction));
        }
    }
}
