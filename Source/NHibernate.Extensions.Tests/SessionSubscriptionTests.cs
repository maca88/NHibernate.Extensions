using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NHibernate.Extensions.Tests.Entities;
using NHibernate.Tool.hbm2ddl;
using T4FluentNH.Tests;

namespace NHibernate.Extensions.Tests
{
    [TestClass]
    public class SessionSubscriptionTests
    {
        [TestMethod]
        public void test_transaction_subscription()
        {
            using (var session = NHConfig.OpenSession())
            using (var transaction = session.BeginTransaction())
            {
                var beforeCommitExecuted = false;
                var afterCommitExecuted = false;

                session.Subscribe(o => o.Transaction
                    .BeforeCommit(s =>
                    {
                        Assert.AreEqual(session, s);
                        Assert.IsTrue(s.Transaction.IsActive);
                        beforeCommitExecuted = true;
                    })
                    .AfterCommit((s, success) =>
                    {
                        Assert.IsTrue(success);
                        Assert.AreEqual(session, s);
                        Assert.IsFalse(s.Transaction.IsActive);
                        afterCommitExecuted = true;
                    }));

                Assert.IsFalse(beforeCommitExecuted);
                Assert.IsFalse(afterCommitExecuted);
                transaction.Commit();
                Assert.IsTrue(beforeCommitExecuted);
                Assert.IsTrue(afterCommitExecuted);
            }
        }

        [TestMethod]
        public void test_transaction_subscription_rollback()
        {
            using (var session = NHConfig.OpenSession())
            using (var transaction = session.BeginTransaction())
            {
                var beforeCommitExecuted = false;
                var afterCommitExecuted = false;

                // BeforeCommit wont be executed on rollback
                session.Subscribe(o => o.Transaction
                    .BeforeCommit(s =>
                    {
                        Assert.AreEqual(session, s);
                        Assert.IsTrue(s.Transaction.IsActive);
                        beforeCommitExecuted = true;
                    })
                    .AfterCommit((s, success) =>
                    {
                        Assert.IsFalse(success);
                        Assert.AreEqual(session, s);
                        Assert.IsFalse(s.Transaction.IsActive);
                        afterCommitExecuted = true;
                    }));

                Assert.IsFalse(beforeCommitExecuted);
                Assert.IsFalse(afterCommitExecuted);
                transaction.Rollback();
                Assert.IsFalse(beforeCommitExecuted);
                Assert.IsTrue(afterCommitExecuted);
            }
        }

        [TestInitialize]
        public void Initialize()
        {
            var schema = new SchemaExport(NHConfig.Configuration);
            schema.Drop(false, true);
            schema.Create(false, true);
        }
    }
}
