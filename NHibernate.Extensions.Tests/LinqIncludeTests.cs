using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using NHibernate.Extensions.Linq;
using NHibernate.Extensions.Tests.Entities;
using NHibernate.Linq;
using NHibernate.Stat;
using NUnit.Framework;

namespace NHibernate.Extensions.Tests
{
    [TestFixture]
    public partial class LinqIncludeTests : BaseIncludeTest
    {
        [Test]
        public void TestImmutable()
        {
            using (var session = NHConfig.OpenSession())
            {
                var query = session.Query<EQBPerson>().Include(o => o.BestFriend);

                Assert.AreEqual(typeof(IncludeQueryProvider), query.Provider.GetType());
                Assert.AreNotEqual(query.Provider, query.Include(o => o.DrivingLicence).Provider);
                Assert.AreNotEqual(query.Provider, query.Include(o => o.CurrentOwnedVehicles).Provider);
                Assert.AreNotEqual(query.Provider, query.Include("DrivingLicence").Provider);
                Assert.AreNotEqual(query.Provider, ((IQueryable)query).Include("DrivingLicence").Provider);

                var includeQuery = query.Include(o => o.CurrentOwnedVehicles);
                Assert.AreNotEqual(includeQuery.Provider, includeQuery.ThenInclude(o => o.CurrentOwner).Provider);
                Assert.AreNotEqual(includeQuery.Provider, includeQuery.ThenInclude(o => o.PreviousUsers).Provider);
            }
        }

        [Test]
        public void TestInheritance()
        {
            using (var session = NHConfig.OpenSession())
            {
                var list = session.Query<Animal>().Include(o => o.Type).ToList();
            }
        }

        [Test]
        public void TestLinqToObject()
        {
            var person = new List<EQBPerson>()
                .AsQueryable()
                .Include(o => o.BestFriend)
                .Include(o => o.CurrentOwnedVehicles).ThenInclude(o => o.CurrentOwner)
                .FirstOrDefault();

            Assert.IsNull(person);
        }

        [Test]
        public void TestLinqToObjectWithIncludeOptions()
        {
            var person = new List<EQBPerson>()
                .AsQueryable()
                .Include(o => o.BestFriend)
                .WithIncludeOptions(options => options.SetMaximumColumnsPerQuery(10))
                .FirstOrDefault();

            Assert.IsNull(person);
        }

        [Test]
        public void TestLinqToObjectWithIncludeOptionsNonGeneric()
        {
            IQueryable query = new List<EQBPerson>().AsQueryable();
            var persons = query
                .Include("BestFriend")
                .WithIncludeOptions(options => options.SetMaximumColumnsPerQuery(10))
                .ToList<EQBPerson>();

            Assert.AreEqual(0, persons.Count);
        }

        [Test]
        public void TestSessionWithOptions()
        {
            using (var session = NHConfig.OpenSession())
            {
                var person = session.Query<EQBPerson>()
                    .Include(o => o.BestFriend)
                    .WithOptions(o => o.SetReadOnly(true))
                    .First(o => o.Name == "Petra");

                Assert.IsTrue(session.IsReadOnly(person));
                Assert.IsTrue(session.IsReadOnly(person.BestFriend));
            }
        }

        [Test]
        public void TestQueryableCollection()
        {
            List<EQBVehicle> vehicles;
            using (var session = NHConfig.OpenSession())
            {
                var person = session.Query<EQBPerson>()
                    .First(o => o.Name == "Petra");

                vehicles = person.CurrentOwnedVehicles.AsQueryable()
                    .Include(o => o.Wheels)
                    .ToList();
            }

            Assert.AreEqual(1, vehicles.Count);
            Assert.IsTrue(NHibernateUtil.IsInitialized(vehicles.First().Wheels));
            Assert.IsTrue(NHibernateUtil.IsInitialized(vehicles.First().Wheels.First()));
        }

        [Test]
        public void TestFutureLongCount()
        {
            using (var session = NHConfig.OpenSession())
            {
                var query = session.Query<EQBPerson>()
                    .Include(o => o.BestFriend)
                    .Include(o => o.CurrentOwnedVehicles)
                    .Where(o => o.Name == "Petra");

                var futureCount = query.ToFutureValue(o => o.LongCount());
                var items = query.ToList();

                Assert.AreEqual(1, futureCount.Value);
                Assert.AreEqual(1, items.Count);
                Assert.IsTrue(NHibernateUtil.IsInitialized(items[0].BestFriend));
                Assert.IsTrue(NHibernateUtil.IsInitialized(items[0].CurrentOwnedVehicles));
            }
        }

        [Test]
        public async Task TestFutureLongCountAsync()
        {
            using (var session = NHConfig.OpenSession())
            {
                var query = session.Query<EQBPerson>()
                    .Include(o => o.BestFriend)
                    .Include(o => o.CurrentOwnedVehicles)
                    .Where(o => o.Name == "Petra");

                var futureCount = query.ToFutureValue(o => o.LongCount());
                var items = await query.ToListAsync();

                Assert.AreEqual(1, await futureCount.GetValueAsync());
                Assert.AreEqual(1, items.Count);
                Assert.IsTrue(NHibernateUtil.IsInitialized(items[0].BestFriend));
                Assert.IsTrue(NHibernateUtil.IsInitialized(items[0].CurrentOwnedVehicles));
            }
        }

        [Test]
        public void TestSkipAndTake()
        {
            /*NHibernate way*/
            using (var session = NHConfig.OpenSession())
            {
                var subQuery = session.Query<EQBPerson>()
                    .Skip(0)
                    .Take(10);
                var query = session.Query<EQBPerson>()
                    .Fetch(o => o.BestFriend)
                        .ThenFetch(o => o.IdentityCard)
                    .Fetch(o => o.BestFriend)
                        .ThenFetch(o => o.BestFriend)
                        .ThenFetch(o => o.BestFriend)
                        .ThenFetch(o => o.BestFriend)
                    .FetchMany(o => o.CurrentOwnedVehicles)
                        .ThenFetchMany(o => o.Wheels)
                    .FetchMany(o => o.CurrentOwnedVehicles)
                        .ThenFetchMany(o => o.RoadworthyTests)
                    .FetchMany(o => o.CurrentOwnedVehicles)
                        .ThenFetchMany(o => o.MileageHistory)
                    .Fetch(o => o.DrivingLicence)
                    .Fetch(o => o.IdentityCard)
                    .Fetch(o => o.MarriedWith)
                    .Where(o => subQuery.Contains(o))
                    .ToFuture();
                session.Query<EQBPerson>()
                    .FetchMany(o => o.OwnedHouses)
                    .Where(o => subQuery.Contains(o))
                    .ToFuture();
                session.Query<EQBPerson>()
                    .FetchMany(o => o.PreviouslyOwnedVehicles)
                    .Where(o => subQuery.Contains(o))
                    .ToFuture();
                Assert.AreEqual(4, query.ToList().Count);
            }


            using (var session = NHConfig.OpenSession())
            {
                var test = session.Query<EQBPerson>()
                    .Skip(0)
                    .Take(10)
                    .Include(o => o.BestFriend.IdentityCard)
                    .Include(o => o.BestFriend.BestFriend.BestFriend.BestFriend)
                    .Include(o => o.CurrentOwnedVehicles)
                    .Include(o => o.CurrentOwnedVehicles).ThenInclude(o => o.Wheels)
                    .Include(o => o.CurrentOwnedVehicles).ThenInclude(o => o.RoadworthyTests)
                    .Include(o => o.CurrentOwnedVehicles).ThenInclude(o => o.MileageHistory)
                    .Include(o => o.DrivingLicence)
                    .Include(o => o.IdentityCard)
                    .Include(o => o.MarriedWith)
                    .Include(o => o.OwnedHouses)
                    .Include(o => o.PreviouslyOwnedVehicles)
                    .ToList();
                Assert.AreEqual(4, test.Count);
            }
        }

        [Test]
        public async Task TestSkipAndTakeAsync()
        {
            using (var session = NHConfig.OpenSession())
            {
                var test = await session.Query<EQBPerson>()
                    .Skip(0)
                    .Take(10)
                    .Include(o => o.BestFriend.IdentityCard)
                    .Include(o => o.BestFriend.BestFriend.BestFriend.BestFriend)
                    .Include(o => o.CurrentOwnedVehicles.First().Wheels)
                    .Include(o => o.CurrentOwnedVehicles.First().RoadworthyTests)
                    .Include(o => o.DrivingLicence)
                    .Include(o => o.IdentityCard)
                    .Include(o => o.MarriedWith)
                    .Include(o => o.OwnedHouses)
                    .Include(o => o.PreviouslyOwnedVehicles)
                    .ToListAsync();
                Assert.AreEqual(4, test.Count);
            }
        }

        [Test]
        public void TestCollectionsWithSamePrefix()
        {
            EQBPerson person;
            using (var session = NHConfig.OpenSession())
            {
                var stats = session.SessionFactory.Statistics;
                stats.Clear();

                person = session.Query<EQBPerson>()
                    .Include(o => o.CurrentOwnedVehicles)
                    .Include(o => o.CurrentOwnedVehiclesOld)
                    .FirstOrDefault(o => o.Name == "Petra");

                CheckStatistics(stats, 2);
            }
            Assert.IsNotNull(person);
            Assert.AreEqual(1, person.CurrentOwnedVehicles.Count);
        }

        [Test]
        public void TestUsingAndCountMethod()
        {
            using (var session = NHConfig.OpenSession())
            {
                var query = session.Query<EQBPerson>()
                    .Include(o => o.BestFriend.IdentityCard)
                    .Include(o => o.BestFriend.BestFriend.BestFriend.BestFriend)
                    .Include(o => o.CurrentOwnedVehicles.First().Wheels)
                    .Include(o => o.CurrentOwnedVehicles.First().RoadworthyTests)
                    .Include(o => o.DrivingLicence)
                    .Include(o => o.IdentityCard)
                    .Include(o => o.MarriedWith)
                    .Include(o => o.OwnedHouses)
                    .Include(o => o.PreviouslyOwnedVehicles);

                var total = query.Count();
                Assert.AreEqual(4, total);

                var people = query.ToList();
                Assert.AreEqual(4, people.Count);
            }
        }

        [Test]
        public async Task TestUsingAndCountMethodAsync()
        {
            using (var session = NHConfig.OpenSession())
            {
                var query = session.Query<EQBPerson>()
                    .Include(o => o.BestFriend.IdentityCard)
                    .Include(o => o.BestFriend.BestFriend.BestFriend.BestFriend)
                    .Include(o => o.CurrentOwnedVehicles.First().Wheels)
                    .Include(o => o.CurrentOwnedVehicles.First().RoadworthyTests)
                    .Include(o => o.DrivingLicence)
                    .Include(o => o.IdentityCard)
                    .Include(o => o.MarriedWith)
                    .Include(o => o.OwnedHouses)
                    .Include(o => o.PreviouslyOwnedVehicles);

                var total = await query.CountAsync();
                Assert.AreEqual(4, total);

                var people = await query.ToListAsync();
                Assert.AreEqual(4, people.Count);
            }
        }

        [Test]
        public void TestUsingAndThenInclude()
        {
            EQBPerson petra;

            using (var session = NHConfig.OpenSession())
            {
                var stats = session.SessionFactory.Statistics;
                stats.Clear();
                var future = session.Query<EQBPerson>()
                    .Include(o => o.BestFriend.IdentityCard)
                    .Include(o => o.BestFriend.BestFriend.BestFriend.BestFriend)
                    .Include(o => o.CurrentOwnedVehicles).ThenInclude(o => o.Wheels)
                    .Include(o => o.CurrentOwnedVehicles).ThenInclude(o => o.RoadworthyTests)
                    .Include(o => o.CurrentOwnedVehicles).ThenInclude(o => o.MileageHistory)
                    .Include(o => o.DrivingLicence)
                    .Include(o => o.CreatedBy)
                    .Include(o => o.IdentityCard)
                    .Include(o => o.MarriedWith)
                    .Include(o => o.OwnedHouses)
                    .Include(o => o.PreviouslyOwnedVehicles)
                    .Where(o => o.Name == "Petra")
                    .ToFutureValue();
                petra = future.Value;
                CheckStatistics(stats, 5);
            }
            ValidateGetEntityResult(petra);
        }

        [Test]
        public void TestSetMaximumColumnsPerQuery()
        {
            EQBPerson petra;

            using (var session = NHConfig.OpenSession())
            {
                var stats = session.SessionFactory.Statistics;
                stats.Clear();
                petra = session.Query<EQBPerson>()
                    .Include(o => o.BestFriend.IdentityCard)
                    .WithIncludeOptions(o => o.SetMaximumColumnsPerQuery(50))
                    .Include(o => o.BestFriend.BestFriend.BestFriend.BestFriend)
                    .Include(o => o.CurrentOwnedVehicles).ThenInclude(o => o.Wheels)
                    .Include(o => o.CurrentOwnedVehicles).ThenInclude(o => o.RoadworthyTests)
                    .Include(o => o.CurrentOwnedVehicles).ThenInclude(o => o.MileageHistory)
                    .Include(o => o.DrivingLicence)
                    .Include(o => o.CreatedBy)
                    .Include(o => o.IdentityCard)
                    .Include(o => o.MarriedWith)
                    .Include(o => o.OwnedHouses)
                    .Include(o => o.PreviouslyOwnedVehicles)
                    .First(o => o.Name == "Petra");
                CheckStatistics(stats, 6);
            }
            ValidateGetEntityResult(petra);
        }

        [Test]
        public void TestSetIgnoreIncludedRelationFunction()
        {
            EQBPerson petra;

            using (var session = NHConfig.OpenSession())
            {
                petra = session.Query<EQBPerson>()
                    .Include(o => o.BestFriend.IdentityCard)
                    .WithIncludeOptions(o => o.SetIgnoreIncludedRelationFunction(
                        (factory, type) => type.ReturnedClass == typeof(EQBIdentityCard)))
                    .First(o => o.Name == "Petra");
            }

            Assert.IsTrue(NHibernateUtil.IsInitialized(petra.BestFriend));
            Assert.IsFalse(NHibernateUtil.IsInitialized(petra.BestFriend.IdentityCard));
        }

        [Test]
        public void TestIgnoreIncludedRelationFunction()
        {
            IncludeOptions.Default.IgnoreIncludedRelationFunction = (factory, type) => type.ReturnedClass == typeof(EQBIdentityCard);
            try
            {
                EQBPerson petra;
                using (var session = NHConfig.OpenSession())
                {
                    petra = session.Query<EQBPerson>()
                        .Include(o => o.BestFriend.IdentityCard)
                        .First(o => o.Name == "Petra");
                }

                Assert.IsTrue(NHibernateUtil.IsInitialized(petra.BestFriend));
                Assert.IsFalse(NHibernateUtil.IsInitialized(petra.BestFriend.IdentityCard));
            }
            finally
            {
                IncludeOptions.Default.IgnoreIncludedRelationFunction = null;
            }
        }

        [Test]
        public void TestSingleForNotExistingPerson()
        {
            EQBPerson test;
            using (var session = NHConfig.OpenSession())
            {
                Assert.Throws<InvalidOperationException>(() =>
                {
                    test = session.Query<EQBPerson>()
                        .Include(o => o.BestFriend.IdentityCard)
                        .Include(o => o.BestFriend.BestFriend.BestFriend.BestFriend)
                        .Include(o => o.CurrentOwnedVehicles.First().Wheels)
                        .Include(o => o.CurrentOwnedVehicles.First().RoadworthyTests)
                        .Include(o => o.DrivingLicence)
                        .Include(o => o.IdentityCard)
                        .Include(o => o.MarriedWith)
                        .Include(o => o.OwnedHouses)
                        .Include(o => o.PreviouslyOwnedVehicles)
                        .Single(o => o.Name == "Test");
                });
            }
        }

        [Test]
        public void TestFirstForNotExistingPerson()
        {
            EQBPerson test;
            using (var session = NHConfig.OpenSession())
            {
                Assert.Throws<InvalidOperationException>(() =>
                {
                    test = session.Query<EQBPerson>()
                        .Include(o => o.BestFriend.IdentityCard)
                        .Include(o => o.BestFriend.BestFriend.BestFriend.BestFriend)
                        .Include(o => o.CurrentOwnedVehicles.First().Wheels)
                        .Include(o => o.CurrentOwnedVehicles.First().RoadworthyTests)
                        .Include(o => o.DrivingLicence)
                        .Include(o => o.IdentityCard)
                        .Include(o => o.MarriedWith)
                        .Include(o => o.OwnedHouses)
                        .Include(o => o.PreviouslyOwnedVehicles)
                        .First(o => o.Name == "Test");
                });
            }
        }

        [Test]
        public void TestToFutureValueWithNull()
        {
            EQBPerson test;
            using (var session = NHConfig.OpenSession())
            {
                var test2 = session.Query<EQBPerson>()
                    .Include(o => o.BestFriend.IdentityCard)
                    .Include(o => o.BestFriend.BestFriend.BestFriend.BestFriend)
                    .Include(o => o.CurrentOwnedVehicles.First().Wheels)
                    .Include(o => o.CurrentOwnedVehicles.First().RoadworthyTests)
                    .Include(o => o.DrivingLicence)
                    .Include(o => o.IdentityCard)
                    .Include(o => o.MarriedWith)
                    .Include(o => o.OwnedHouses)
                    .Include(o => o.PreviouslyOwnedVehicles)
                    .Where(o => o.Name == "Test")
                    .ToFutureValue();
                test = test2.Value;
            }
            Assert.IsNull(test);
        }

        [Test]
        public async Task TestToFutureValueWithNullAsync()
        {
            EQBPerson test;
            using (var session = NHConfig.OpenSession())
            {
                var test2 = session.Query<EQBPerson>()
                    .Include(o => o.BestFriend.IdentityCard)
                    .Include(o => o.BestFriend.BestFriend.BestFriend.BestFriend)
                    .Include(o => o.CurrentOwnedVehicles.First().Wheels)
                    .Include(o => o.CurrentOwnedVehicles.First().RoadworthyTests)
                    .Include(o => o.DrivingLicence)
                    .Include(o => o.IdentityCard)
                    .Include(o => o.MarriedWith)
                    .Include(o => o.OwnedHouses)
                    .Include(o => o.PreviouslyOwnedVehicles)
                    .Where(o => o.Name == "Test")
                    .ToFutureValue();
                test = await test2.GetValueAsync();
            }
            Assert.IsNull(test);
        }

        [Test]
        public void TestToFutureValueForNotExistingPerson()
        {
            EQBPerson test;
            using (var session = NHConfig.OpenSession())
            {
                test = session.Query<EQBPerson>()
                    .Include(o => o.BestFriend.IdentityCard)
                    .Include(o => o.BestFriend.BestFriend.BestFriend.BestFriend)
                    .Include(o => o.CurrentOwnedVehicles.First().Wheels)
                    .Include(o => o.CurrentOwnedVehicles.First().RoadworthyTests)
                    .Include(o => o.DrivingLicence)
                    .Include(o => o.IdentityCard)
                    .Include(o => o.MarriedWith)
                    .Include(o => o.OwnedHouses)
                    .Include(o => o.PreviouslyOwnedVehicles)
                    .Where(o => o.Name == "Test")
                    .ToFutureValue().Value;
            }
            Assert.IsNull(test);
        }

        [Test]
        public async Task TestToFutureValueForNotExistingPersonAsync()
        {
            EQBPerson test;
            using (var session = NHConfig.OpenSession())
            {
                test = await session.Query<EQBPerson>()
                    .Include(o => o.BestFriend.IdentityCard)
                    .Include(o => o.BestFriend.BestFriend.BestFriend.BestFriend)
                    .Include(o => o.CurrentOwnedVehicles.First().Wheels)
                    .Include(o => o.CurrentOwnedVehicles.First().RoadworthyTests)
                    .Include(o => o.DrivingLicence)
                    .Include(o => o.IdentityCard)
                    .Include(o => o.MarriedWith)
                    .Include(o => o.OwnedHouses)
                    .Include(o => o.PreviouslyOwnedVehicles)
                    .Where(o => o.Name == "Test")
                    .ToFutureValue().GetValueAsync();
            }
            Assert.IsNull(test);
        }

        [Test]
        public void TestSelectMany()
        {
            /*Without parameter*/
            using (var session = NHConfig.OpenSession())
            {
                var stats = session.SessionFactory.Statistics;
                stats.Clear();
                var query = session.Query<EQBIdentityCard>()
                    .Where(o => o.Code == "SD")
                    .Fetch(o => o.Owner).ThenFetch(o => o.BestFriend).ThenFetch(o => o.MarriedWith)
                    //.Select(o => o.Owner)
                    .ToList();

                Assert.AreEqual(1, stats.PrepareStatementCount);
            }
            //Assert.AreEqual("System", petra.CreatedBy.UserName);
        }

        [Test]
        public void TestIncludeInterface()
        {
            EQBPerson petra;
            /*Without parameter*/
            using (var session = NHConfig.OpenSession())
            {
                var stats = session.SessionFactory.Statistics;
                stats.Clear();
                petra = session.Query<EQBPerson>()
                    .Include(o => o.CreatedBy)
                    .Single(o => o.Name == "Petra");
                CheckStatistics(stats, 1);
            }
            Assert.AreEqual("System", petra.CreatedBy.UserName);
        }

        [Test]
        public void TestCastToBaseType()
        {
            IPerson petra;
            /*Without parameter*/
            using (var session = NHConfig.OpenSession())
            {
                var stats = session.SessionFactory.Statistics;
                stats.Clear();
                var query = session.Query<EQBPerson>() as IQueryable<IPerson>;
                petra = query
                    .Include("CreatedBy")
                    .Where(o => o.Name == "Petra")
                    .First();
                CheckStatistics(stats, 1);
            }
            Assert.IsNotNull(petra);
            Assert.AreEqual("System", petra.CreatedBy.UserName);
        }

        [Test]
        public async Task TestCastToBaseTypeAsync()
        {
            IPerson petra;
            /*Without parameter*/
            using (var session = NHConfig.OpenSession())
            {
                var stats = session.SessionFactory.Statistics;
                stats.Clear();
                var query = session.Query<EQBPerson>() as IQueryable<IPerson>;
                petra = await query
                    .Include("CreatedBy")
                    .Where(o => o.Name == "Petra")
                    .FirstAsync();
                CheckStatistics(stats, 1);
            }
            Assert.IsNotNull(petra);
            Assert.AreEqual("System", petra.CreatedBy.UserName);
        }

        [Test]
        public void TestCastToBaseTypeRelation()
        {
            IPerson petra;
            /*Without parameter*/
            using (var session = NHConfig.OpenSession())
            {
                var stats = session.SessionFactory.Statistics;
                stats.Clear();
                var query = session.Query<EQBPerson>()
                    .Where(o => o.Name == "Petra") as IQueryable;
                petra = query
                    .Include("CurrentOwnedVehicles")
                    .ToList<IPerson>().First();
                CheckStatistics(stats, 1);
            }
            Assert.AreEqual(petra.CurrentOwnedVehicles.Any(), true);
        }

        [Test]
        public void TestIncludeCollection()
        {
            EQBPerson petra;
            /*Without parameter*/
            using (var session = NHConfig.OpenSession())
            {
                var stats = session.SessionFactory.Statistics;
                stats.Clear();
                petra = session.Query<EQBPerson>()
                    .Include(o => o.PreviouslyOwnedVehicles)
                    .Single(o => o.Name == "Petra");
                CheckStatistics(stats, 1);
            }
        }

#region FutureValue

        [Test]
        public void TestFutureValue()
        {
            EQBPerson petra;

            using (var session = NHConfig.OpenSession())
            {
                var stats = session.SessionFactory.Statistics;
                stats.Clear();
                var future = session.Query<EQBPerson>()
                    .Include(o => o.BestFriend.IdentityCard)
                    .Include(o => o.BestFriend.BestFriend.BestFriend.BestFriend)
                    .Include(o => o.CurrentOwnedVehicles.First().Wheels)
                    .Include(o => o.CurrentOwnedVehicles.First().RoadworthyTests)
                    .Include(o => o.CurrentOwnedVehicles.First().MileageHistory)
                    .Include(o => o.DrivingLicence)
                    .Include(o => o.CreatedBy)
                    .Include(o => o.IdentityCard)
                    .Include(o => o.MarriedWith)
                    .Include(o => o.OwnedHouses)
                    .Include(o => o.PreviouslyOwnedVehicles)
                    .Where(o => o.Name == "Petra")
                    .ToFutureValue();
                petra = future.Value;
                CheckStatistics(stats, 5);
            }
            ValidateGetEntityResult(petra);
        }

        [Test]
        public async Task TestFutureValueAsync()
        {
            EQBPerson petra;

            using (var session = NHConfig.OpenSession())
            {
                var stats = session.SessionFactory.Statistics;
                stats.Clear();
                var future = session.Query<EQBPerson>()
                    .Include(o => o.BestFriend.IdentityCard)
                    .Include(o => o.BestFriend.BestFriend.BestFriend.BestFriend)
                    .Include(o => o.CurrentOwnedVehicles.First().Wheels)
                    .Include(o => o.CurrentOwnedVehicles.First().RoadworthyTests)
                    .Include(o => o.CurrentOwnedVehicles.First().MileageHistory)
                    .Include(o => o.DrivingLicence)
                    .Include(o => o.CreatedBy)
                    .Include(o => o.IdentityCard)
                    .Include(o => o.MarriedWith)
                    .Include(o => o.OwnedHouses)
                    .Include(o => o.PreviouslyOwnedVehicles)
                    .Where(o => o.Name == "Petra")
                    .ToFutureValue();
                petra = await future.GetValueAsync();
                CheckStatistics(stats, 5);
            }
            ValidateGetEntityResult(petra);
        }

        [Test]
        public void TestFutureValueWithExpression()
        {
            EQBPerson petra;

            using (var session = NHConfig.OpenSession())
            {
                var stats = session.SessionFactory.Statistics;
                stats.Clear();
                var future = session.Query<EQBPerson>()
                    .Include(o => o.BestFriend.IdentityCard)
                    .Include(o => o.BestFriend.BestFriend.BestFriend.BestFriend)
                    .Include(o => o.CurrentOwnedVehicles.First().Wheels)
                    .Include(o => o.CurrentOwnedVehicles.First().RoadworthyTests)
                    .Include(o => o.CurrentOwnedVehicles.First().MileageHistory)
                    .Include(o => o.DrivingLicence)
                    .Include(o => o.CreatedBy)
                    .Include(o => o.IdentityCard)
                    .Include(o => o.MarriedWith)
                    .Include(o => o.OwnedHouses)
                    .Include(o => o.PreviouslyOwnedVehicles)
                    .Where(o => o.Name == "Petra")
                    .ToFutureValue(o => o.First());
                petra = future.Value;
                CheckStatistics(stats, 5);
            }
            ValidateGetEntityResult(petra);
        }

        [Test]
        public async Task TestFutureValueWithExpressionAsync()
        {
            EQBPerson petra;

            using (var session = NHConfig.OpenSession())
            {
                var stats = session.SessionFactory.Statistics;
                stats.Clear();
                var future = session.Query<EQBPerson>()
                    .Include(o => o.BestFriend.IdentityCard)
                    .Include(o => o.BestFriend.BestFriend.BestFriend.BestFriend)
                    .Include(o => o.CurrentOwnedVehicles.First().Wheels)
                    .Include(o => o.CurrentOwnedVehicles.First().RoadworthyTests)
                    .Include(o => o.CurrentOwnedVehicles.First().MileageHistory)
                    .Include(o => o.DrivingLicence)
                    .Include(o => o.CreatedBy)
                    .Include(o => o.IdentityCard)
                    .Include(o => o.MarriedWith)
                    .Include(o => o.OwnedHouses)
                    .Include(o => o.PreviouslyOwnedVehicles)
                    .Where(o => o.Name == "Petra")
                    .ToFutureValue(o => o.First());
                petra = await future.GetValueAsync();
                CheckStatistics(stats, 5);
            }
            ValidateGetEntityResult(petra);
        }

        #endregion

        #region SingleOrDefault

        [Test]
        public void TestSingleOrDefault()
        {
            EQBPerson petra;

            using (var session = NHConfig.OpenSession())
            {
                var stats = session.SessionFactory.Statistics;
                stats.Clear();
                petra = session.Query<EQBPerson>()
                    .Include(o => o.BestFriend.IdentityCard)
                    .Include(o => o.BestFriend.BestFriend.BestFriend.BestFriend)
                    .Include(o => o.CurrentOwnedVehicles.First().Wheels)
                    .Include(o => o.CurrentOwnedVehicles.First().RoadworthyTests)
                    .Include(o => o.CurrentOwnedVehicles.First().MileageHistory)
                    .Include(o => o.DrivingLicence)
                    .Include(o => o.CreatedBy)
                    .Include(o => o.IdentityCard)
                    .Include(o => o.MarriedWith)
                    .Include(o => o.OwnedHouses)
                    .Include(o => o.PreviouslyOwnedVehicles)
                    .Where(o => o.Name == "Petra")
                    .SingleOrDefault();
                CheckStatistics(stats, 5);
            }
            ValidateGetEntityResult(petra);
        }

        [Test]
        public async Task TestTestSingleOrDefaultAsync()
        {
            EQBPerson petra;

            using (var session = NHConfig.OpenSession())
            {
                var stats = session.SessionFactory.Statistics;
                stats.Clear();
                petra = await session.Query<EQBPerson>()
                    .Include(o => o.BestFriend.IdentityCard)
                    .Include(o => o.BestFriend.BestFriend.BestFriend.BestFriend)
                    .Include(o => o.CurrentOwnedVehicles.First().Wheels)
                    .Include(o => o.CurrentOwnedVehicles.First().RoadworthyTests)
                    .Include(o => o.CurrentOwnedVehicles.First().MileageHistory)
                    .Include(o => o.DrivingLicence)
                    .Include(o => o.CreatedBy)
                    .Include(o => o.IdentityCard)
                    .Include(o => o.MarriedWith)
                    .Include(o => o.OwnedHouses)
                    .Include(o => o.PreviouslyOwnedVehicles)
                    .Where(o => o.Name == "Petra")
                    .SingleOrDefaultAsync();
                CheckStatistics(stats, 5);
            }
            ValidateGetEntityResult(petra);
        }

        [Test]
        public async Task TestTestSingleOrDefaultWithCondition()
        {
            EQBPerson petra;

            using (var session = NHConfig.OpenSession())
            {
                var stats = session.SessionFactory.Statistics;
                stats.Clear();
                petra = await session.Query<EQBPerson>()
                    .Include(o => o.BestFriend.IdentityCard)
                    .Include(o => o.BestFriend.BestFriend.BestFriend.BestFriend)
                    .Include(o => o.CurrentOwnedVehicles.First().Wheels)
                    .Include(o => o.CurrentOwnedVehicles.First().RoadworthyTests)
                    .Include(o => o.CurrentOwnedVehicles.First().MileageHistory)
                    .Include(o => o.DrivingLicence)
                    .Include(o => o.CreatedBy)
                    .Include(o => o.IdentityCard)
                    .Include(o => o.MarriedWith)
                    .Include(o => o.OwnedHouses)
                    .Include(o => o.PreviouslyOwnedVehicles)
                    .SingleOrDefaultAsync(o => o.Name == "Petra");
                CheckStatistics(stats, 5);
            }
            ValidateGetEntityResult(petra);
        }

#endregion

#region Single

        [Test]
        public void TestSingle()
        {
            EQBPerson petra;

            using (var session = NHConfig.OpenSession())
            {
                var stats = session.SessionFactory.Statistics;
                stats.Clear();
                petra = session.Query<EQBPerson>()
                    .Include(o => o.BestFriend.IdentityCard)
                    .Include(o => o.BestFriend.BestFriend.BestFriend.BestFriend)
                    .Include(o => o.CurrentOwnedVehicles.First().Wheels)
                    .Include(o => o.CurrentOwnedVehicles.First().RoadworthyTests)
                    .Include(o => o.CurrentOwnedVehicles.First().MileageHistory)
                    .Include(o => o.DrivingLicence)
                    .Include(o => o.CreatedBy)
                    .Include(o => o.IdentityCard)
                    .Include(o => o.MarriedWith)
                    .Include(o => o.OwnedHouses)
                    .Include(o => o.PreviouslyOwnedVehicles)
                    .Where(o => o.Name == "Petra")
                    .Single();
                CheckStatistics(stats, 5);
            }
            ValidateGetEntityResult(petra);
        }

        [Test]
        public async Task TestSingleAsync()
        {
            EQBPerson petra;

            using (var session = NHConfig.OpenSession())
            {
                var stats = session.SessionFactory.Statistics;
                stats.Clear();
                petra = await session.Query<EQBPerson>()
                    .Include(o => o.BestFriend.IdentityCard)
                    .Include(o => o.BestFriend.BestFriend.BestFriend.BestFriend)
                    .Include(o => o.CurrentOwnedVehicles.First().Wheels)
                    .Include(o => o.CurrentOwnedVehicles.First().RoadworthyTests)
                    .Include(o => o.CurrentOwnedVehicles.First().MileageHistory)
                    .Include(o => o.DrivingLicence)
                    .Include(o => o.CreatedBy)
                    .Include(o => o.IdentityCard)
                    .Include(o => o.MarriedWith)
                    .Include(o => o.OwnedHouses)
                    .Include(o => o.PreviouslyOwnedVehicles)
                    .Where(o => o.Name == "Petra")
                    .SingleAsync();
                CheckStatistics(stats, 5);
            }
            ValidateGetEntityResult(petra);
        }

        [Test]
        public async Task TestSingleWithConditionAsync()
        {
            EQBPerson petra;

            using (var session = NHConfig.OpenSession())
            {
                var stats = session.SessionFactory.Statistics;
                stats.Clear();
                petra = await session.Query<EQBPerson>()
                    .Include(o => o.BestFriend.IdentityCard)
                    .Include(o => o.BestFriend.BestFriend.BestFriend.BestFriend)
                    .Include(o => o.CurrentOwnedVehicles.First().Wheels)
                    .Include(o => o.CurrentOwnedVehicles.First().RoadworthyTests)
                    .Include(o => o.CurrentOwnedVehicles.First().MileageHistory)
                    .Include(o => o.DrivingLicence)
                    .Include(o => o.CreatedBy)
                    .Include(o => o.IdentityCard)
                    .Include(o => o.MarriedWith)
                    .Include(o => o.OwnedHouses)
                    .Include(o => o.PreviouslyOwnedVehicles)
                    .SingleAsync(o => o.Name == "Petra");
                CheckStatistics(stats, 5);
            }
            ValidateGetEntityResult(petra);
        }

        [Test]
        public void TestSingleWithCondition()
        {
            EQBPerson petra;

            using (var session = NHConfig.OpenSession())
            {
                var stats = session.SessionFactory.Statistics;
                stats.Clear();
                petra = session.Query<EQBPerson>()
                    .Include(o => o.BestFriend.IdentityCard)
                    .Include(o => o.BestFriend.BestFriend.BestFriend.BestFriend)
                    .Include(o => o.CurrentOwnedVehicles.First().Wheels)
                    .Include(o => o.CurrentOwnedVehicles.First().RoadworthyTests)
                    .Include(o => o.CurrentOwnedVehicles.First().MileageHistory)
                    .Include(o => o.DrivingLicence)
                    .Include(o => o.CreatedBy)
                    .Include(o => o.IdentityCard)
                    .Include(o => o.MarriedWith)
                    .Include(o => o.OwnedHouses)
                    .Include(o => o.PreviouslyOwnedVehicles)
                    .Single(o => o.Name == "Petra");
                CheckStatistics(stats, 5);
            }
            ValidateGetEntityResult(petra);
        }

        #endregion

        #region FirstOrDefault

        [Test]
        public void TestFirstOrDefault()
        {
            EQBPerson petra;

            using (var session = NHConfig.OpenSession())
            {
                var stats = session.SessionFactory.Statistics;
                stats.Clear();
                petra = session.Query<EQBPerson>()
                    .Include(o => o.BestFriend.IdentityCard)
                    .Include(o => o.BestFriend.BestFriend.BestFriend.BestFriend)
                    .Include(o => o.CurrentOwnedVehicles.First().Wheels)
                    .Include(o => o.CurrentOwnedVehicles.First().RoadworthyTests)
                    .Include(o => o.CurrentOwnedVehicles.First().MileageHistory)
                    .Include(o => o.DrivingLicence)
                    .Include(o => o.CreatedBy)
                    .Include(o => o.IdentityCard)
                    .Include(o => o.MarriedWith)
                    .Include(o => o.OwnedHouses)
                    .Include(o => o.PreviouslyOwnedVehicles)
                    .Where(o => o.Name == "Petra")
                    .FirstOrDefault();
                CheckStatistics(stats, 5);
            }
            ValidateGetEntityResult(petra);
        }

        [Test]
        public async Task TestFirstOrDefaultAsync()
        {
            EQBPerson petra;

            using (var session = NHConfig.OpenSession())
            {
                var stats = session.SessionFactory.Statistics;
                stats.Clear();
                petra = await session.Query<EQBPerson>()
                    .Include(o => o.BestFriend.IdentityCard)
                    .Include(o => o.BestFriend.BestFriend.BestFriend.BestFriend)
                    .Include(o => o.CurrentOwnedVehicles.First().Wheels)
                    .Include(o => o.CurrentOwnedVehicles.First().RoadworthyTests)
                    .Include(o => o.CurrentOwnedVehicles.First().MileageHistory)
                    .Include(o => o.DrivingLicence)
                    .Include(o => o.CreatedBy)
                    .Include(o => o.IdentityCard)
                    .Include(o => o.MarriedWith)
                    .Include(o => o.OwnedHouses)
                    .Include(o => o.PreviouslyOwnedVehicles)
                    .Where(o => o.Name == "Petra")
                    .FirstOrDefaultAsync();
                CheckStatistics(stats, 5);
            }
            ValidateGetEntityResult(petra);
        }

        [Test]
        public async Task TestFirstOrDefaultWithConditionAsync()
        {
            EQBPerson petra;

            using (var session = NHConfig.OpenSession())
            {
                var stats = session.SessionFactory.Statistics;
                stats.Clear();
                petra = await session.Query<EQBPerson>()
                    .Include(o => o.BestFriend.IdentityCard)
                    .Include(o => o.BestFriend.BestFriend.BestFriend.BestFriend)
                    .Include(o => o.CurrentOwnedVehicles.First().Wheels)
                    .Include(o => o.CurrentOwnedVehicles.First().RoadworthyTests)
                    .Include(o => o.CurrentOwnedVehicles.First().MileageHistory)
                    .Include(o => o.DrivingLicence)
                    .Include(o => o.CreatedBy)
                    .Include(o => o.IdentityCard)
                    .Include(o => o.MarriedWith)
                    .Include(o => o.OwnedHouses)
                    .Include(o => o.PreviouslyOwnedVehicles)
                    .FirstOrDefaultAsync(o => o.Name == "Petra");
                CheckStatistics(stats, 5);
            }
            ValidateGetEntityResult(petra);
        }

        [Test]
        public void TestFirstOrDefaultWithCondition()
        {
            EQBPerson petra;

            using (var session = NHConfig.OpenSession())
            {
                var stats = session.SessionFactory.Statistics;
                stats.Clear();
                petra = session.Query<EQBPerson>()
                    .Include(o => o.BestFriend.IdentityCard)
                    .Include(o => o.BestFriend.BestFriend.BestFriend.BestFriend)
                    .Include(o => o.CurrentOwnedVehicles.First().Wheels)
                    .Include(o => o.CurrentOwnedVehicles.First().RoadworthyTests)
                    .Include(o => o.CurrentOwnedVehicles.First().MileageHistory)
                    .Include(o => o.DrivingLicence)
                    .Include(o => o.CreatedBy)
                    .Include(o => o.IdentityCard)
                    .Include(o => o.MarriedWith)
                    .Include(o => o.OwnedHouses)
                    .Include(o => o.PreviouslyOwnedVehicles)
                    .FirstOrDefault(o => o.Name == "Petra");
                CheckStatistics(stats, 5);
            }
            ValidateGetEntityResult(petra);
        }

#endregion

#region First

        [Test]
        public void TestFirst()
        {
            EQBPerson petra;

            using (var session = NHConfig.OpenSession())
            {
                var stats = session.SessionFactory.Statistics;
                stats.Clear();
                petra = session.Query<EQBPerson>()
                    .Include(o => o.BestFriend.IdentityCard)
                    .Include(o => o.BestFriend.BestFriend.BestFriend.BestFriend)
                    .Include(o => o.CurrentOwnedVehicles.First().Wheels)
                    .Include(o => o.CurrentOwnedVehicles.First().RoadworthyTests)
                    .Include(o => o.CurrentOwnedVehicles.First().MileageHistory)
                    .Include(o => o.DrivingLicence)
                    .Include(o => o.CreatedBy)
                    .Include(o => o.IdentityCard)
                    .Include(o => o.MarriedWith)
                    .Include(o => o.OwnedHouses)
                    .Include(o => o.PreviouslyOwnedVehicles)
                    .Where(o => o.Name == "Petra")
                    .First();
                CheckStatistics(stats, 5);
            }
            ValidateGetEntityResult(petra);
        }

        [Test]
        public async Task TestFirstAsync()
        {
            EQBPerson petra;

            using (var session = NHConfig.OpenSession())
            {
                var stats = session.SessionFactory.Statistics;
                stats.Clear();
                petra = await session.Query<EQBPerson>()
                    .Include(o => o.BestFriend.IdentityCard)
                    .Include(o => o.BestFriend.BestFriend.BestFriend.BestFriend)
                    .Include(o => o.CurrentOwnedVehicles.First().Wheels)
                    .Include(o => o.CurrentOwnedVehicles.First().RoadworthyTests)
                    .Include(o => o.CurrentOwnedVehicles.First().MileageHistory)
                    .Include(o => o.DrivingLicence)
                    .Include(o => o.CreatedBy)
                    .Include(o => o.IdentityCard)
                    .Include(o => o.MarriedWith)
                    .Include(o => o.OwnedHouses)
                    .Include(o => o.PreviouslyOwnedVehicles)
                    .Where(o => o.Name == "Petra")
                    .FirstAsync();
                CheckStatistics(stats, 5);
            }
            ValidateGetEntityResult(petra);
        }

        [Test]
        public async Task TestFirstWithConditionAsync()
        {
            EQBPerson petra;

            using (var session = NHConfig.OpenSession())
            {
                var stats = session.SessionFactory.Statistics;
                stats.Clear();
                petra = await session.Query<EQBPerson>()
                    .Include(o => o.BestFriend.IdentityCard)
                    .Include(o => o.BestFriend.BestFriend.BestFriend.BestFriend)
                    .Include(o => o.CurrentOwnedVehicles.First().Wheels)
                    .Include(o => o.CurrentOwnedVehicles.First().RoadworthyTests)
                    .Include(o => o.CurrentOwnedVehicles.First().MileageHistory)
                    .Include(o => o.DrivingLicence)
                    .Include(o => o.CreatedBy)
                    .Include(o => o.IdentityCard)
                    .Include(o => o.MarriedWith)
                    .Include(o => o.OwnedHouses)
                    .Include(o => o.PreviouslyOwnedVehicles)
                    .FirstAsync(o => o.Name == "Petra");
                CheckStatistics(stats, 5);
            }
            ValidateGetEntityResult(petra);
        }

        [Test]
        public void TestFirstWithCondition()
        {
            EQBPerson petra;

            using (var session = NHConfig.OpenSession())
            {
                var stats = session.SessionFactory.Statistics;
                stats.Clear();
                petra = session.Query<EQBPerson>()
                    .Include(o => o.BestFriend.IdentityCard)
                    .Include(o => o.BestFriend.BestFriend.BestFriend.BestFriend)
                    .Include(o => o.CurrentOwnedVehicles.First().Wheels)
                    .Include(o => o.CurrentOwnedVehicles.First().RoadworthyTests)
                    .Include(o => o.CurrentOwnedVehicles.First().MileageHistory)
                    .Include(o => o.DrivingLicence)
                    .Include(o => o.CreatedBy)
                    .Include(o => o.IdentityCard)
                    .Include(o => o.MarriedWith)
                    .Include(o => o.OwnedHouses)
                    .Include(o => o.PreviouslyOwnedVehicles)
                    .First(o => o.Name == "Petra");
                CheckStatistics(stats, 5);
            }
            ValidateGetEntityResult(petra);
        }

#endregion

#region LastOrDefault

        [Test]
        public void TestLastOrDefault()
        {
            EQBPerson petra;

            using (var session = NHConfig.OpenSession())
            {
                var stats = session.SessionFactory.Statistics;
                stats.Clear();
                petra = session.Query<EQBPerson>()
                    .Include(o => o.BestFriend.IdentityCard)
                    .Include(o => o.BestFriend.BestFriend.BestFriend.BestFriend)
                    .Include(o => o.CurrentOwnedVehicles.First().Wheels)
                    .Include(o => o.CurrentOwnedVehicles.First().RoadworthyTests)
                    .Include(o => o.CurrentOwnedVehicles.First().MileageHistory)
                    .Include(o => o.DrivingLicence)
                    .Include(o => o.CreatedBy)
                    .Include(o => o.IdentityCard)
                    .Include(o => o.MarriedWith)
                    .Include(o => o.OwnedHouses)
                    .Include(o => o.PreviouslyOwnedVehicles)
                    .Where(o => o.Name == "Petra")
                    .LastOrDefault();
                CheckStatistics(stats, 5);
            }
            ValidateGetEntityResult(petra);
        }

        [Test]
        public void TestLastOrDefaultWithCondition()
        {
            EQBPerson petra;

            using (var session = NHConfig.OpenSession())
            {
                var stats = session.SessionFactory.Statistics;
                stats.Clear();
                petra = session.Query<EQBPerson>()
                    .Include(o => o.BestFriend.IdentityCard)
                    .Include(o => o.BestFriend.BestFriend.BestFriend.BestFriend)
                    .Include(o => o.CurrentOwnedVehicles.First().Wheels)
                    .Include(o => o.CurrentOwnedVehicles.First().RoadworthyTests)
                    .Include(o => o.CurrentOwnedVehicles.First().MileageHistory)
                    .Include(o => o.DrivingLicence)
                    .Include(o => o.CreatedBy)
                    .Include(o => o.IdentityCard)
                    .Include(o => o.MarriedWith)
                    .Include(o => o.OwnedHouses)
                    .Include(o => o.PreviouslyOwnedVehicles)
                    .LastOrDefault(o => o.Name == "Petra");
                CheckStatistics(stats, 5);
            }
            ValidateGetEntityResult(petra);
        }

#endregion


        private void CheckStatistics(IStatistics stats, int queryCount)
        {
            Assert.AreEqual(1, stats.PrepareStatementCount);
            Assert.AreEqual(1, stats.Queries.Length);

            if (stats.Queries[0].Contains("(MultiQuery)"))
            {
                // < NH 5.2
                Assert.AreEqual($"{queryCount} queries (MultiQuery)", stats.Queries[0]);
            }
            else
            {
                // >= NH 5.2
                Assert.AreEqual(queryCount, stats.Queries[0].Trim().Trim(';').Split(';').Length);
            }
        }
    }
}
