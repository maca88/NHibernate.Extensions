using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NHibernate.Extensions.Tests.Entities;
using NHibernate.Linq;
using T4FluentNH.Tests;

namespace NHibernate.Extensions.Tests
{
    [TestClass]
    public class LinqIncludeTests : BaseIncludeTest
    {
        [TestMethod]
        public void using_skip_and_take()
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

#if NH5
        [TestMethod]
        public async Task using_skip_and_take_async()
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
#endif

        [TestMethod]
        public void using_count_method()
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

        [TestMethod]
        public void using_then_include()
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
                Assert.AreEqual(1, stats.PrepareStatementCount);
                Assert.AreEqual("5 queries (MultiQuery)", stats.Queries[0]);
            }
            ValidateGetEntityResult(petra);
        }

#if NH5
        [TestMethod]
        public async Task using_async_count_method()
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
#endif

        [TestMethod]
#if NH5
        [ExpectedException(typeof (InvalidOperationException))]
#else
        [ExpectedException(typeof(TargetInvocationException))]
#endif
        public void using_single_method_for_retriving_a_person_that_dont_exists()
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
                    .Single(o => o.Name == "Test");
            }
        }

        [TestMethod]
#if NH5
        [ExpectedException(typeof (InvalidOperationException))]
#else
        [ExpectedException(typeof(TargetInvocationException))]
#endif
        public void using_first_method_for_retriving_a_person_that_dont_exists()
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
                    .First(o => o.Name == "Test");
            }
        }

#if NH5
        [TestMethod]
        [ExpectedException(typeof (InvalidOperationException))]
        public async Task using_first_async_method_for_retriving_a_person_that_dont_exists()
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
                    .FirstAsync(o => o.Name == "Test");
            }
        }
#endif

        [TestMethod]
        public void using_to_future_value_method_without_getting_value()
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

#if NH5
        [TestMethod]
        public async Task using_to_future_value_async_method_without_getting_value()
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
#endif

        [TestMethod]
        public void using_tofutorevalue_method_for_retriving_a_person_that_dont_exists()
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

        [TestMethod]
        public void test_selectmany()
        {
            IPerson petra;
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

#if NH5
        [TestMethod]
        public async Task using_tofutorevalue_async_method_for_retriving_a_person_that_dont_exists()
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
#endif

        [TestMethod]
        public void test_include_with_interface()
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
                Assert.AreEqual(1, stats.PrepareStatementCount);
                Assert.AreEqual("1 queries (MultiQuery)", stats.Queries[0]);
            }
            Assert.AreEqual("System", petra.CreatedBy.UserName);
        }

        [TestMethod]
        public void test_cast_to_base_type()
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
                Assert.AreEqual(1, stats.PrepareStatementCount);
                Assert.AreEqual("1 queries (MultiQuery)", stats.Queries[0]);
            }
            Assert.IsNotNull(petra);
            Assert.AreEqual("System", petra.CreatedBy.UserName);
        }

#if NH5
        [TestMethod]
        public async Task test_cast_to_base_type_async()
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
                Assert.AreEqual(1, stats.PrepareStatementCount);
                Assert.AreEqual("1 queries (MultiQuery)", stats.Queries[0]);
            }
            Assert.IsNotNull(petra);
            Assert.AreEqual("System", petra.CreatedBy.UserName);
        }
#endif

        [TestMethod]
        public void test_cast_to_base_type_relation()
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
                Assert.AreEqual(1, stats.PrepareStatementCount);
                Assert.AreEqual("1 queries (MultiQuery)", stats.Queries[0]);
            }
            Assert.AreEqual(petra.CurrentOwnedVehicles.Any(), true);
        }

        [TestMethod]
        public void test_include_with_collection()
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
                Assert.AreEqual(1, stats.PrepareStatementCount);
                Assert.AreEqual("1 queries (MultiQuery)", stats.Queries[0]);
            }
        }

#region FutureValue

        [TestMethod]
        public void get_single_result_without_skip_or_take_with_future_value()
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
                Assert.AreEqual(1, stats.PrepareStatementCount);
                Assert.AreEqual("5 queries (MultiQuery)", stats.Queries[0]);
            }
            ValidateGetEntityResult(petra);
        }

#if NH5
        [TestMethod]
        public async Task get_single_result_without_skip_or_take_with_future_value_async()
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
                Assert.AreEqual(1, stats.PrepareStatementCount);
                Assert.AreEqual("5 queries (MultiQuery)", stats.Queries[0]);
            }
            ValidateGetEntityResult(petra);
        }
#endif

#endregion

#region SingleOrDefault

        [TestMethod]
        public void get_single_result_without_skip_or_take_with_single_or_default()
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
                Assert.AreEqual(1, stats.PrepareStatementCount);
                Assert.AreEqual("5 queries (MultiQuery)", stats.Queries[0]);
            }
            ValidateGetEntityResult(petra);
        }

#if NH5
        [TestMethod]
        public async Task get_single_result_without_skip_or_take_with_single_or_default_async()
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
                Assert.AreEqual(1, stats.PrepareStatementCount);
                Assert.AreEqual("5 queries (MultiQuery)", stats.Queries[0]);
            }
            ValidateGetEntityResult(petra);
        }

        [TestMethod]
        public async Task get_single_result_without_skip_or_take_with_single_or_default_async_with_parameter()
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
                Assert.AreEqual(1, stats.PrepareStatementCount);
                Assert.AreEqual("5 queries (MultiQuery)", stats.Queries[0]);
            }
            ValidateGetEntityResult(petra);
        }
#endif
#endregion

#region Single

        [TestMethod]
        public void get_single_result_without_skip_or_take_with_single()
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
                Assert.AreEqual(1, stats.PrepareStatementCount);
                Assert.AreEqual("5 queries (MultiQuery)", stats.Queries[0]);
            }
            ValidateGetEntityResult(petra);
        }

#if NH5
        [TestMethod]
        public async Task get_single_result_without_skip_or_take_with_single_async()
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
                Assert.AreEqual(1, stats.PrepareStatementCount);
                Assert.AreEqual("5 queries (MultiQuery)", stats.Queries[0]);
            }
            ValidateGetEntityResult(petra);
        }

        [TestMethod]
        public async Task get_single_result_without_skip_or_take_with_single_async_with_parameter()
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
                Assert.AreEqual(1, stats.PrepareStatementCount);
                Assert.AreEqual("5 queries (MultiQuery)", stats.Queries[0]);
            }
            ValidateGetEntityResult(petra);
        }
#endif

        [TestMethod]
        public void get_single_result_without_skip_or_take_with_single_with_parameter()
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
                Assert.AreEqual(1, stats.PrepareStatementCount);
                Assert.AreEqual("5 queries (MultiQuery)", stats.Queries[0]);
            }
            ValidateGetEntityResult(petra);
        }

#endregion

#region FirstOrDefault

        [TestMethod]
        public void get_single_result_without_skip_or_take_with_first_or_default()
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
                Assert.AreEqual(1, stats.PrepareStatementCount);
                Assert.AreEqual("5 queries (MultiQuery)", stats.Queries[0]);
            }
            ValidateGetEntityResult(petra);
        }

#if NH5
        [TestMethod]
        public async Task get_single_result_without_skip_or_take_with_first_or_default_async()
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
                Assert.AreEqual(1, stats.PrepareStatementCount);
                Assert.AreEqual("5 queries (MultiQuery)", stats.Queries[0]);
            }
            ValidateGetEntityResult(petra);
        }

        [TestMethod]
        public async Task get_single_result_without_skip_or_take_with_first_or_default_asnc_with_parameter()
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
                Assert.AreEqual(1, stats.PrepareStatementCount);
                Assert.AreEqual("5 queries (MultiQuery)", stats.Queries[0]);
            }
            ValidateGetEntityResult(petra);
        }
#endif

        [TestMethod]
        public void get_single_result_without_skip_or_take_with_first_or_default_with_parameter()
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
                Assert.AreEqual(1, stats.PrepareStatementCount);
                Assert.AreEqual("5 queries (MultiQuery)", stats.Queries[0]);
            }
            ValidateGetEntityResult(petra);
        }

#endregion

#region First

        [TestMethod]
        public void get_single_result_without_skip_or_take_with_first()
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
                Assert.AreEqual(1, stats.PrepareStatementCount);
                Assert.AreEqual("5 queries (MultiQuery)", stats.Queries[0]);
            }
            ValidateGetEntityResult(petra);
        }

#if NH5
        [TestMethod]
        public async Task get_single_result_without_skip_or_take_with_first_async()
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
                Assert.AreEqual(1, stats.PrepareStatementCount);
                Assert.AreEqual("5 queries (MultiQuery)", stats.Queries[0]);
            }
            ValidateGetEntityResult(petra);
        }

        [TestMethod]
        public async Task get_single_result_without_skip_or_take_with_first_async_with_parameter()
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
                Assert.AreEqual(1, stats.PrepareStatementCount);
                Assert.AreEqual("5 queries (MultiQuery)", stats.Queries[0]);
            }
            ValidateGetEntityResult(petra);
        }
#endif

        [TestMethod]
        public void get_single_result_without_skip_or_take_with_first_with_parameter()
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
                Assert.AreEqual(1, stats.PrepareStatementCount);
                Assert.AreEqual("5 queries (MultiQuery)", stats.Queries[0]);
            }
            ValidateGetEntityResult(petra);
        }

#endregion

#region LastOrDefault

        [TestMethod]
        public void get_single_result_without_skip_or_take_with_last_or_default()
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
                Assert.AreEqual(1, stats.PrepareStatementCount);
                Assert.AreEqual("5 queries (MultiQuery)", stats.Queries[0]);
            }
            ValidateGetEntityResult(petra);
        }

        [TestMethod]
        public void get_single_result_without_skip_or_take_with_last_or_default_with_parameter()
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
                Assert.AreEqual(1, stats.PrepareStatementCount);
                Assert.AreEqual("5 queries (MultiQuery)", stats.Queries[0]);
            }
            ValidateGetEntityResult(petra);
        }

#endregion

    }
}
