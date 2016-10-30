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
    public class BatchFetchTests
    {
        [TestMethod]
        public void batch_test_with_string_property()
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

        [TestMethod]
        public void batch_test_with_int_property()
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

        [TestMethod]
        public void batch_test_with_filter()
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

        [TestInitialize]
        public void Initialize()
        {
            var schema = new SchemaExport(NHConfig.Configuration);
            schema.Drop(false, true);
            schema.Create(false, true);
            FillData();
        }

        protected void FillData()
        {
            //Saving entities
            using (var session = NHConfig.OpenSession())
            {
                for (var i = 0; i < 1300; i++)
                {
                    session.Save(new BatchModel
                    {
                        Name = $"Batch{i}"
                    });
                }
                session.Flush();
            }
        }
    }
}
