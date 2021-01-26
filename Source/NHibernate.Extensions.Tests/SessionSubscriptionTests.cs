﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NHibernate.Tool.hbm2ddl;
using NUnit.Framework;

namespace NHibernate.Extensions.Tests
{
    [TestFixture]
    public partial class SessionSubscriptionTests
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
