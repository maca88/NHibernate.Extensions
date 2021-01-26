﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NHibernate.Extensions.Tests.Entities;
using NHibernate.Tool.hbm2ddl;
using NUnit.Framework;

namespace NHibernate.Extensions.Tests
{
    [TestFixture]
    public partial class BatchFetchTests
    {
        [Test]
        public void TestStringProperty()
        {
            var keys = new HashSet<string>();
            var r = new Random();
            for (var i = 0; i < 600; i++)
            {
                keys.Add($"Batch{r.Next(0, 1300)}");
            }

            using (var session = NHConfig.OpenSession())
            {
                var stats = session.SessionFactory.Statistics;
                var queryCount = stats.PrepareStatementCount;
                var models = session.BatchFetch<BatchModel, string>(keys.ToList(), o => o.Name, 50);

                var expectedQueryCount = (int) Math.Ceiling(keys.Count/50m);
                Assert.AreEqual(keys.Count, models.Count);
                Assert.AreEqual(expectedQueryCount, stats.PrepareStatementCount - queryCount);

                foreach (var model in models)
                {
                    Assert.IsTrue(keys.Contains(model.Name));
                }
            }
        }

        [Test]
        public void TestIntProperty()
        {
            var keys = new HashSet<int>();
            var r = new Random();
            for (var i = 0; i < 600; i++)
            {
                keys.Add(r.Next(1, 1200));
            }

            using (var session = NHConfig.OpenSession())
            {
                var stats = session.SessionFactory.Statistics;
                var queryCount = stats.PrepareStatementCount;
                var models = session.BatchFetch<BatchModel, int>(keys.ToList(), o => o.Id, 50);

                var expectedQueryCount = (int)Math.Ceiling(keys.Count / 50m);
                Assert.AreEqual(keys.Count, models.Count);
                Assert.AreEqual(expectedQueryCount, stats.PrepareStatementCount - queryCount);

                foreach (var model in models)
                {
                    Assert.IsTrue(keys.Contains(model.Id));
                }
            }
        }

        [Test]
        public void TestFilter()
        {
            var keys = Enumerable.Range(1, 600).ToList();

            using (var session = NHConfig.OpenSession())
            {
                var stats = session.SessionFactory.Statistics;
                var queryCount = stats.PrepareStatementCount;
                var models = session.BatchFetch<BatchModel, int>(keys, o => o.Id, 50, q => q.Where(o => o.Id > 400));

                var expectedQueryCount = (int)Math.Ceiling(keys.Count / 50m);
                Assert.AreEqual(200, models.Count);
                Assert.AreEqual(expectedQueryCount, stats.PrepareStatementCount - queryCount);

                foreach (var model in models)
                {
                    Assert.IsTrue(keys.Contains(model.Id));
                }
            }
        }

        [Test]
        public void TestSelectAnonymousType()
        {
            var keys = Enumerable.Range(1, 600).ToList();

            using (var session = NHConfig.OpenSession())
            {
                var stats = session.SessionFactory.Statistics;
                var queryCount = stats.PrepareStatementCount;
                var models = session.BatchFetch<BatchModel>(50)
                    .SetKeys(keys, o => o.Id)
                    .BeforeQueryExecution(q => q.Where(o => o.Id > 400))
                    .Select(o => new { o.Name })
                    .Execute();

                var expectedQueryCount = (int)Math.Ceiling(keys.Count / 50m);
                Assert.AreEqual(200, models.Count);
                Assert.AreEqual(expectedQueryCount, stats.PrepareStatementCount - queryCount);
            }
        }

        [Test]
        public void TestSelectString()
        {
            var keys = Enumerable.Range(1, 600).ToList();

            using (var session = NHConfig.OpenSession())
            {
                var stats = session.SessionFactory.Statistics;
                var queryCount = stats.PrepareStatementCount;
                var models = session.BatchFetch<BatchModel>(50)
                    .SetKeys(keys, o => o.Id)
                    .Select(o => o.Name)
                    .BeforeQueryExecution(q => q.Where(o => o.Id > 400))
                    .Execute();

                var expectedQueryCount = (int)Math.Ceiling(keys.Count / 50m);
                Assert.AreEqual(200, models.Count);
                Assert.AreEqual(expectedQueryCount, stats.PrepareStatementCount - queryCount);
            }
        }

        [Explicit]
        public void TestPerformance()
        {
            var keys = Enumerable.Range(1, 5000).ToList();
            var batchSizes = new [] { 250, 1, 10, 50, 100, 250, 500, 1000};
            var coldStart = true; // skip the first time as the time is always higher

            foreach (var batchSize in batchSizes)
            {
                using (var session = NHConfig.OpenSession())
                {
                    var stats = session.SessionFactory.Statistics;
                    var queryCount = stats.PrepareStatementCount;
                    var stopwatch = new Stopwatch();
                    stopwatch.Start();
                    var models = session.BatchFetch<BatchModel, int>(keys, o => o.Id, batchSize);
                    stopwatch.Stop();
                    if (coldStart)
                    {
                        coldStart = false;
                    }
                    else
                    {
                        Console.WriteLine($"Elapsed time for batch size {batchSize}: {stopwatch.ElapsedMilliseconds}ms");
                    }
                    var expectedQueryCount = (int)Math.Ceiling(keys.Count / (decimal)batchSize);
                    Assert.AreEqual(5000, models.Count);
                    Assert.AreEqual(expectedQueryCount, stats.PrepareStatementCount - queryCount);

                    foreach (var model in models)
                    {
                        Assert.IsTrue(keys.Contains(model.Id));
                    }
                }
            }
        }

        [OneTimeSetUp]
        public void Initialize()
        {
            var schema = new SchemaExport(NHConfig.Configuration);
            schema.Drop(false, true);
            schema.Create(false, true);
            FillData();
        }

        [OneTimeTearDown]
        public void Cleanup()
        {
            var schema = new SchemaExport(NHConfig.Configuration);
            schema.Drop(false, true);
        }

        protected void FillData()
        {
            //Saving entities
            using (var session = NHConfig.SessionFactory.OpenStatelessSession())
            using (var transaction = session.BeginTransaction())
            {
                for (var i = 0; i < 5000; i++)
                {
                    session.Insert(new BatchModel
                    {
                        Name = $"Batch{i}"
                    });
                }
                transaction.Commit();
            }
        }
    }
}
