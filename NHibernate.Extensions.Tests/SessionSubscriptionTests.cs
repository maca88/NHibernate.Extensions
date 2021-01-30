using System;
using System.Threading.Tasks;
using NHibernate.Tool.hbm2ddl;
using NUnit.Framework;

namespace NHibernate.Extensions.Tests
{
    [TestFixture]
    public class SessionSubscriptionTests
    {
        [Test]
        public void TestTransactionSubscription()
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
                        Assert.IsTrue(transaction.IsActive);
                        beforeCommitExecuted = true;
                    })
                    .AfterCommit((s, success) =>
                    {
                        Assert.IsTrue(success);
                        Assert.AreEqual(session, s);
                        Assert.IsFalse(transaction.IsActive);
                        afterCommitExecuted = true;
                    }));

                Assert.IsFalse(beforeCommitExecuted);
                Assert.IsFalse(afterCommitExecuted);
                transaction.Commit();
                Assert.IsTrue(beforeCommitExecuted);
                Assert.IsTrue(afterCommitExecuted);
            }
        }

        [Test]
        public async Task TestTransactionSubscriptionAsync()
        {
            using (var session = NHConfig.OpenSession())
            using (var transaction = session.BeginTransaction())
            {
                var beforeCommitExecuted = false;
                var afterCommitExecuted = false;

                session.Subscribe(o => o.Transaction
                    .BeforeCommit(async s =>
                    {
                        await Task.Delay(0);
                        Assert.AreEqual(session, s);
                        Assert.IsTrue(transaction.IsActive);
                        beforeCommitExecuted = true;
                    })
                    .AfterCommit(async (s, success) =>
                    {
                        await Task.Delay(0);
                        Assert.IsTrue(success);
                        Assert.AreEqual(session, s);
                        Assert.IsFalse(transaction.IsActive);
                        afterCommitExecuted = true;
                    }));

                Assert.IsFalse(beforeCommitExecuted);
                Assert.IsFalse(afterCommitExecuted);
                await transaction.CommitAsync();
                Assert.IsTrue(beforeCommitExecuted);
                Assert.IsTrue(afterCommitExecuted);
            }
        }

        [Test]
        public void TestTransactionSubscriptionRollback()
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
                        Assert.IsTrue(transaction.IsActive);
                        beforeCommitExecuted = true;
                    })
                    .AfterCommit((s, success) =>
                    {
                        Assert.IsFalse(success);
                        Assert.AreEqual(session, s);
                        Assert.IsFalse(transaction.IsActive);
                        afterCommitExecuted = true;
                    }));

                Assert.IsFalse(beforeCommitExecuted);
                Assert.IsFalse(afterCommitExecuted);
                transaction.Rollback();
                Assert.IsFalse(beforeCommitExecuted);
                Assert.IsTrue(afterCommitExecuted);
            }
        }

        [Test]
        public async Task TestTransactionSubscriptionRollbackAsync()
        {
            using (var session = NHConfig.OpenSession())
            using (var transaction = session.BeginTransaction())
            {
                var beforeCommitExecuted = false;
                var afterCommitExecuted = false;

                // BeforeCommit wont be executed on rollback
                session.Subscribe(o => o.Transaction
                    .BeforeCommit(async s =>
                    {
                        await Task.Delay(0);
                        Assert.AreEqual(session, s);
                        Assert.IsTrue(transaction.IsActive);
                        beforeCommitExecuted = true;
                    })
                    .AfterCommit(async (s, success) =>
                    {
                        await Task.Delay(0);
                        Assert.IsFalse(success);
                        Assert.AreEqual(session, s);
                        Assert.IsFalse(transaction.IsActive);
                        afterCommitExecuted = true;
                    }));

                Assert.IsFalse(beforeCommitExecuted);
                Assert.IsFalse(afterCommitExecuted);
                await transaction.RollbackAsync();
                Assert.IsFalse(beforeCommitExecuted);
                Assert.IsTrue(afterCommitExecuted);
            }
        }

        [Test]
        public void TestTransactionSubscriptionWithoutTransaction()
        {
            using (var session = NHConfig.OpenSession())
            {
                Assert.Throws<InvalidOperationException>(() =>
                {
                    session.Subscribe(o => o.Transaction
                        .BeforeCommit(s => { })
                        .AfterCommit((s, success) => { }));
                });
            }
        }

        [Test]
        public void TestAsyncTransactionSubscriptionInSyncCommit()
        {
            using (var session = NHConfig.OpenSession())
            using (var transaction = session.BeginTransaction())
            {
                session.Subscribe(o => o.Transaction
                    .BeforeCommit(s => Task.CompletedTask)
                    .AfterCommit((s, success) => Task.CompletedTask));

                Assert.Throws<NotSupportedException>(() =>
                {
                    transaction.Commit();
                });
            }
        }

        [OneTimeSetUp]
        public void Initialize()
        {
            var schema = new SchemaExport(NHConfig.Configuration);
            schema.Drop(false, true);
            schema.Create(false, true);
        }

        [OneTimeTearDown]
        public void Cleanup()
        {
            var schema = new SchemaExport(NHConfig.Configuration);
            schema.Drop(false, true);
        }
    }
}
