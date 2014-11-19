using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FluentNHibernate.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NHibernate.Extensions.Tests.Entities;
using NHibernate.Linq;
using NHibernate.Util;
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
                       .Where(o => subQuery.Contains(o))
                       .ToFuture();
                session.Query<EQBPerson>()
                       .Fetch(o => o.BestFriend)
                       .ThenFetch(o => o.BestFriend)
                       .ThenFetch(o => o.BestFriend)
                       .ThenFetch(o => o.BestFriend)
                       .Where(o => subQuery.Contains(o))
                       .ToFuture();
                session.Query<EQBPerson>()
                       .FetchMany(o => o.CurrentOwnedVehicles)
                       .ThenFetchMany(o => o.Wheels)
                       .Where(o => subQuery.Contains(o))
                       .ToFuture();
                session.Query<EQBPerson>()
                       .Fetch(o => o.DrivingLicence)
                       .Where(o => subQuery.Contains(o))
                       .ToFuture();
                session.Query<EQBPerson>()
                       .Fetch(o => o.IdentityCard)
                       .Where(o => subQuery.Contains(o))
                       .ToFuture();
                session.Query<EQBPerson>()
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
                    .Include(o => o.CurrentOwnedVehicles.First().Wheels)
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
        [ExpectedException(typeof(InvalidOperationException))]
        public void using_single_method_for_retriving_a_person_that_dont_exists()
        {
            EQBPerson test;
            using (var session = NHConfig.OpenSession())
            {
                test = session.Query<EQBPerson>()
                    .Include(o => o.BestFriend.IdentityCard)
                    .Include(o => o.BestFriend.BestFriend.BestFriend.BestFriend)
                    .Include(o => o.CurrentOwnedVehicles.First().Wheels)
                    .Include(o => o.DrivingLicence)
                    .Include(o => o.IdentityCard)
                    .Include(o => o.MarriedWith)
                    .Include(o => o.OwnedHouses)
                    .Include(o => o.PreviouslyOwnedVehicles)
                    .Single(o => o.Name == "Test");
            }
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void using_first_method_for_retriving_a_person_that_dont_exists()
        {
            EQBPerson test;
            using (var session = NHConfig.OpenSession())
            {
                test = session.Query<EQBPerson>()
                    .Include(o => o.BestFriend.IdentityCard)
                    .Include(o => o.BestFriend.BestFriend.BestFriend.BestFriend)
                    .Include(o => o.CurrentOwnedVehicles.First().Wheels)
                    .Include(o => o.DrivingLicence)
                    .Include(o => o.IdentityCard)
                    .Include(o => o.MarriedWith)
                    .Include(o => o.OwnedHouses)
                    .Include(o => o.PreviouslyOwnedVehicles)
                    .First(o => o.Name == "Test");
            }
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
        public void test_include_with_interface()
        {
            EQBPerson petra;
            ClearStatistics();
            /*Without parameter*/
            using (var session = NHConfig.OpenSession())
            {
                petra = session.Query<EQBPerson>()
                    .Include(o => o.CreatedBy)
                    .Single(o => o.Name == "Petra");
                Assert.AreEqual(1, GetQueryCount(0));
            }
            Assert.AreEqual(petra.CreatedBy.UserName, "System");
        }

        [TestMethod]
        public void test_cast_to_base_type()
        {
            IPerson petra;
            ClearStatistics();
            /*Without parameter*/
            using (var session = NHConfig.OpenSession())
            {
                var query = session.Query<EQBPerson>() as IQueryable<IPerson>;
                petra = query
                    .Include("CreatedBy")
                    .Where(o => o.Name == "Petra")
                    .First();
                Assert.AreEqual(1, GetQueryCount(0));
            }
            Assert.AreEqual(petra.CreatedBy.UserName, "System");
        }

        [TestMethod]
        public void test_include_with_collection()
        {
            EQBPerson petra;
            ClearStatistics();
            /*Without parameter*/
            using (var session = NHConfig.OpenSession())
            {
                petra = session.Query<EQBPerson>()
                    .Include(o => o.PreviouslyOwnedVehicles)
                    .Single(o => o.Name == "Petra");
                Assert.AreEqual(1, GetQueryCount(0));
            }
        }

        [TestMethod]
        public void get_single_result_without_skip_or_take()
        {
            EQBPerson petra;

            /*NHibernate way*/
            using (var session = NHConfig.OpenSession())
            {
                session.Query<EQBPerson>()
                       .Fetch(o => o.BestFriend)
                       .ThenFetch(o => o.IdentityCard)
                       .Where(o => o.Name == "Petra")
                       .ToFuture();
                session.Query<EQBPerson>()
                       .Fetch(o => o.BestFriend)
                       .ThenFetch(o => o.BestFriend)
                       .ThenFetch(o => o.BestFriend)
                       .ThenFetch(o => o.BestFriend)
                       .Where(o => o.Name == "Petra")
                       .ToFuture();
                session.Query<EQBPerson>()
                       .FetchMany(o => o.CurrentOwnedVehicles)
                       .ThenFetchMany(o => o.Wheels)
                       .Where(o => o.Name == "Petra")
                       .ToFuture();
                session.Query<EQBPerson>()
                       .Fetch(o => o.DrivingLicence)
                       .Where(o => o.Name == "Petra")
                       .ToFuture();
                session.Query<EQBPerson>()
                       .Fetch(o => o.CreatedBy)
                       .Where(o => o.Name == "Petra")
                       .ToFuture();
                session.Query<EQBPerson>()
                       .Fetch(o => o.IdentityCard)
                       .Where(o => o.Name == "Petra")
                       .ToFuture();
                session.Query<EQBPerson>()
                       .Fetch(o => o.MarriedWith)
                       .Where(o => o.Name == "Petra")
                       .ToFuture();
                session.Query<EQBPerson>()
                       .FetchMany(o => o.OwnedHouses)
                       .Where(o => o.Name == "Petra")
                       .ToFuture();
                petra = session.Query<EQBPerson>()
                       .FetchMany(o => o.PreviouslyOwnedVehicles)
                       .Where(o => o.Name == "Petra")
                       .ToFutureValue().Value;

                Assert.AreEqual(9, GetQueryCount(0));
            }
            ValidateGetEntityResult(petra);

            #region SingleOrDefault

            /*Without parameter*/
            using (var session = NHConfig.OpenSession())
            {
                petra = session.Query<EQBPerson>()
                    .Include(o => o.BestFriend.IdentityCard)
                    .Include(o => o.BestFriend.BestFriend.BestFriend.BestFriend)
                    .Include(o => o.CurrentOwnedVehicles.First().Wheels)
                    .Include(o => o.DrivingLicence)
                    .Include(o => o.CreatedBy)
                    .Include(o => o.IdentityCard)
                    .Include(o => o.MarriedWith)
                    .Include(o => o.OwnedHouses)
                    .Include(o => o.PreviouslyOwnedVehicles)
                    .Where(o => o.Name == "Petra")
                    .SingleOrDefault();

                Assert.AreEqual(1, GetQueriesCount());
            }
            ValidateGetEntityResult(petra);

            /*With parameter*/
            using (var session = NHConfig.OpenSession())
            {
                petra = session.Query<EQBPerson>()
                    .Include(o => o.BestFriend.IdentityCard)
                    .Include(o => o.BestFriend.BestFriend.BestFriend.BestFriend)
                    .Include(o => o.CurrentOwnedVehicles.First().Wheels)
                    .Include(o => o.DrivingLicence)
                    .Include(o => o.CreatedBy)
                    .Include(o => o.IdentityCard)
                    .Include(o => o.MarriedWith)
                    .Include(o => o.OwnedHouses)
                    .Include(o => o.PreviouslyOwnedVehicles)
                    .SingleOrDefault(o => o.Name == "Petra");

                Assert.AreEqual(1, GetQueriesCount());
            }
            ValidateGetEntityResult(petra);

            #endregion

            #region Single

            /*Without parameter*/
            using (var session = NHConfig.OpenSession())
            {
                petra = session.Query<EQBPerson>()
                    .Include(o => o.BestFriend.IdentityCard)
                    .Include(o => o.BestFriend.BestFriend.BestFriend.BestFriend)
                    .Include(o => o.CurrentOwnedVehicles.First().Wheels)
                    .Include(o => o.DrivingLicence)
                    .Include(o => o.CreatedBy)
                    .Include(o => o.IdentityCard)
                    .Include(o => o.MarriedWith)
                    .Include(o => o.OwnedHouses)
                    .Include(o => o.PreviouslyOwnedVehicles)
                    .Where(o => o.Name == "Petra")
                    .Single();

                Assert.AreEqual(1, GetQueriesCount());
            }
            ValidateGetEntityResult(petra);

            /*With parameter*/
            using (var session = NHConfig.OpenSession())
            {
                petra = session.Query<EQBPerson>()
                    .Include(o => o.BestFriend.IdentityCard)
                    .Include(o => o.BestFriend.BestFriend.BestFriend.BestFriend)
                    .Include(o => o.CurrentOwnedVehicles.First().Wheels)
                    .Include(o => o.DrivingLicence)
                    .Include(o => o.CreatedBy)
                    .Include(o => o.IdentityCard)
                    .Include(o => o.MarriedWith)
                    .Include(o => o.OwnedHouses)
                    .Include(o => o.PreviouslyOwnedVehicles)
                    .Single(o => o.Name == "Petra");

                Assert.AreEqual(1, GetQueriesCount());
            }
            ValidateGetEntityResult(petra);

            #endregion

            #region FirstOrDefault

            /*Without parameter*/
            using (var session = NHConfig.OpenSession())
            {
                petra = session.Query<EQBPerson>()
                    .Include(o => o.BestFriend.IdentityCard)
                    .Include(o => o.BestFriend.BestFriend.BestFriend.BestFriend)
                    .Include(o => o.CurrentOwnedVehicles.First().Wheels)
                    .Include(o => o.DrivingLicence)
                    .Include(o => o.CreatedBy)
                    .Include(o => o.IdentityCard)
                    .Include(o => o.MarriedWith)
                    .Include(o => o.OwnedHouses)
                    .Include(o => o.PreviouslyOwnedVehicles)
                    .Where(o => o.Name == "Petra")
                    .FirstOrDefault();

                Assert.AreEqual(1, GetQueriesCount());
            }
            ValidateGetEntityResult(petra);

            /*With parameter*/
            using (var session = NHConfig.OpenSession())
            {
                petra = session.Query<EQBPerson>()
                    .Include(o => o.BestFriend.IdentityCard)
                    .Include(o => o.BestFriend.BestFriend.BestFriend.BestFriend)
                    .Include(o => o.CurrentOwnedVehicles.First().Wheels)
                    .Include(o => o.DrivingLicence)
                    .Include(o => o.CreatedBy)
                    .Include(o => o.IdentityCard)
                    .Include(o => o.MarriedWith)
                    .Include(o => o.OwnedHouses)
                    .Include(o => o.PreviouslyOwnedVehicles)
                    .FirstOrDefault(o => o.Name == "Petra");

                Assert.AreEqual(1, GetQueriesCount());
            }
            ValidateGetEntityResult(petra);

            #endregion

            #region First

            /*Without parameter*/
            using (var session = NHConfig.OpenSession())
            {
                petra = session.Query<EQBPerson>()
                    .Include(o => o.BestFriend.IdentityCard)
                    .Include(o => o.BestFriend.BestFriend.BestFriend.BestFriend)
                    .Include(o => o.CurrentOwnedVehicles.First().Wheels)
                    .Include(o => o.DrivingLicence)
                    .Include(o => o.CreatedBy)
                    .Include(o => o.IdentityCard)
                    .Include(o => o.MarriedWith)
                    .Include(o => o.OwnedHouses)
                    .Include(o => o.PreviouslyOwnedVehicles)
                    .Where(o => o.Name == "Petra")
                    .First();

                Assert.AreEqual(1, GetQueriesCount());
            }
            ValidateGetEntityResult(petra);

            /*With parameter*/
            using (var session = NHConfig.OpenSession())
            {
                petra = session.Query<EQBPerson>()
                    .Include(o => o.BestFriend.IdentityCard)
                    .Include(o => o.BestFriend.BestFriend.BestFriend.BestFriend)
                    .Include(o => o.CurrentOwnedVehicles.First().Wheels)
                    .Include(o => o.DrivingLicence)
                    .Include(o => o.CreatedBy)
                    .Include(o => o.IdentityCard)
                    .Include(o => o.MarriedWith)
                    .Include(o => o.OwnedHouses)
                    .Include(o => o.PreviouslyOwnedVehicles)
                    .First(o => o.Name == "Petra");

                Assert.AreEqual(1, GetQueriesCount());
            }
            ValidateGetEntityResult(petra);

            #endregion

            #region LastOrDefault

            /*Without parameter*/
            using (var session = NHConfig.OpenSession())
            {
                petra = session.Query<EQBPerson>()
                    .Include(o => o.BestFriend.IdentityCard)
                    .Include(o => o.BestFriend.BestFriend.BestFriend.BestFriend)
                    .Include(o => o.CurrentOwnedVehicles.First().Wheels)
                    .Include(o => o.DrivingLicence)
                    .Include(o => o.CreatedBy)
                    .Include(o => o.IdentityCard)
                    .Include(o => o.MarriedWith)
                    .Include(o => o.OwnedHouses)
                    .Include(o => o.PreviouslyOwnedVehicles)
                    .Where(o => o.Name == "Petra")
                    .LastOrDefault();

                Assert.AreEqual(1, GetQueriesCount());
            }
            ValidateGetEntityResult(petra);

            /*With parameter*/
            using (var session = NHConfig.OpenSession())
            {
                petra = session.Query<EQBPerson>()
                    .Include(o => o.BestFriend.IdentityCard)
                    .Include(o => o.BestFriend.BestFriend.BestFriend.BestFriend)
                    .Include(o => o.CurrentOwnedVehicles.First().Wheels)
                    .Include(o => o.DrivingLicence)
                    .Include(o => o.CreatedBy)
                    .Include(o => o.IdentityCard)
                    .Include(o => o.MarriedWith)
                    .Include(o => o.OwnedHouses)
                    .Include(o => o.PreviouslyOwnedVehicles)
                    .LastOrDefault(o => o.Name == "Petra");

                Assert.AreEqual(1, GetQueriesCount());
            }
            ValidateGetEntityResult(petra);

            #endregion

            #region ToFutureValue

            using (var session = NHConfig.OpenSession())
            {
                var future = session.Query<EQBPerson>()
                    .Include(o => o.BestFriend.IdentityCard)
                    .Include(o => o.BestFriend.BestFriend.BestFriend.BestFriend)
                    .Include(o => o.CurrentOwnedVehicles.First().Wheels)
                    .Include(o => o.DrivingLicence)
                    .Include(o => o.CreatedBy)
                    .Include(o => o.IdentityCard)
                    .Include(o => o.MarriedWith)
                    .Include(o => o.OwnedHouses)
                    .Include(o => o.PreviouslyOwnedVehicles)
                    .Where(o => o.Name == "Petra")
                    .ToFutureValue();
                petra = future.Value;
                Assert.AreEqual(1, GetQueriesCount());
            }
            ValidateGetEntityResult(petra);

            #endregion
        }
    }
}
