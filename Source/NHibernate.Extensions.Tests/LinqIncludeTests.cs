using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NHibernate.Extensions.Linq;
using NHibernate.Extensions.Tests.Entities;
using NHibernate.Linq;
using NHibernate.Stat;

namespace NHibernate.Extensions.Tests
{
    [TestClass]
    public class LinqIncludeTests : BaseIncludeTest
    {
        [TestMethod]
        public void immutable()
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

        [TestMethod]
        public void inheritance()
        {
            using (var session = NHConfig.OpenSession())
            {
                var list = session.Query<Animal>().Include(o => o.Type).ToList();
            }
        }

        [TestMethod]
        public void linq_to_object()
        {
            var person = new List<EQBPerson>()
                .AsQueryable()
                .Include(o => o.BestFriend)
                .Include(o => o.CurrentOwnedVehicles).ThenInclude(o => o.CurrentOwner)
                .FirstOrDefault();

            Assert.IsNull(person);
        }

        [TestMethod]
        public void linq_to_object_with_include_options()
        {
            var person = new List<EQBPerson>()
                .AsQueryable()
                .Include(o => o.BestFriend)
                .WithIncludeOptions(options => options.SetMaximumColumnsPerQuery(10))
                .FirstOrDefault();

            Assert.IsNull(person);
        }

        [TestMethod]
        public void linq_to_object_with_include_options_non_generic()
        {
            IQueryable query = new List<EQBPerson>().AsQueryable();
            var persons = query
                .Include("BestFriend")
                .WithIncludeOptions(options => options.SetMaximumColumnsPerQuery(10))
                .ToList<EQBPerson>();

            Assert.AreEqual(0, persons.Count);
        }

        [TestMethod]
        public void session_with_options()
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

        [TestMethod]
        public void queryable_collection()
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

            Assert.AreEqual(4, vehicles.Count);
            Assert.IsTrue(NHibernateUtil.IsInitialized(vehicles.First().Wheels));
            Assert.IsTrue(NHibernateUtil.IsInitialized(vehicles.First().Wheels.First()));
        }

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

        [TestMethod]
        public void collections_with_same_prefix()
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
                CheckStatistics(stats, 5);
            }
            ValidateGetEntityResult(petra);
        }

        [TestMethod]
        public void set_maximum_columns_per_query()
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

        [TestMethod]
        public void set_ignore_included_relation_function()
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

        [TestMethod]
        public void set_ignore_included_relation_function_global()
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

        [TestMethod]
        [ExpectedException(typeof (InvalidOperationException))]
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
        [ExpectedException(typeof (InvalidOperationException))]
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
                CheckStatistics(stats, 1);
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
                CheckStatistics(stats, 1);
            }
            Assert.IsNotNull(petra);
            Assert.AreEqual("System", petra.CreatedBy.UserName);
        }

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
                CheckStatistics(stats, 1);
            }
            Assert.IsNotNull(petra);
            Assert.AreEqual("System", petra.CreatedBy.UserName);
        }

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
                CheckStatistics(stats, 1);
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
                CheckStatistics(stats, 1);
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
                CheckStatistics(stats, 5);
            }
            ValidateGetEntityResult(petra);
        }

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
                CheckStatistics(stats, 5);
            }
            ValidateGetEntityResult(petra);
        }

        [TestMethod]
        public void get_single_result_without_skip_or_take_with_future_value_expression()
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

        [TestMethod]
        public async Task get_single_result_without_skip_or_take_with_future_value_expression_async()
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
                CheckStatistics(stats, 5);
            }
            ValidateGetEntityResult(petra);
        }

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
                CheckStatistics(stats, 5);
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
                CheckStatistics(stats, 5);
            }
            ValidateGetEntityResult(petra);
        }

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
                CheckStatistics(stats, 5);
            }
            ValidateGetEntityResult(petra);
        }

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
                CheckStatistics(stats, 5);
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
                CheckStatistics(stats, 5);
            }
            ValidateGetEntityResult(petra);
        }

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
                CheckStatistics(stats, 5);
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
                CheckStatistics(stats, 5);
            }
            ValidateGetEntityResult(petra);
        }

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
                CheckStatistics(stats, 5);
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
                CheckStatistics(stats, 5);
            }
            ValidateGetEntityResult(petra);
        }

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
                CheckStatistics(stats, 5);
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
                CheckStatistics(stats, 5);
            }
            ValidateGetEntityResult(petra);
        }

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
                CheckStatistics(stats, 5);
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
                CheckStatistics(stats, 5);
            }
            ValidateGetEntityResult(petra);
        }

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
                CheckStatistics(stats, 5);
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
                CheckStatistics(stats, 5);
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
