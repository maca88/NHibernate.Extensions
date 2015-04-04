using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NHibernate.Engine;
using NHibernate.Extensions.Tests.Entities;
using NHibernate.Linq;
using T4FluentNH.Tests;

namespace NHibernate.Extensions.Tests
{

    [TestClass]
    public class EntityQueryBuilderTests : TestBase
    {

        #region GetEntity

        //[TestMethod]
        //public void GetEntityLongTestWithouSkipTake()
        //{
        //    FillData();
        //    EQBPerson petra;

        //    using (var session = NHConfig.OpenSession())
        //    {
        //        petra = session.QueryOver<EQBPerson>()
        //            .Where(o => o.Name == "Petra")
        //            .Lock().Upgrade
        //            .Include(o => o.BestFriend)
        //            .Include(o => o.BestFriend.IdentityCard)
        //            .Include(o => o.BestFriend.BestFriend)
        //            .Include(o => o.BestFriend.BestFriend.BestFriend)
        //            .Include(o => o.BestFriend.BestFriend.BestFriend.BestFriend)
        //            .Include(o => o.CurrentOwnedVehicles)
        //            .Include(o => o.CurrentOwnedVehicles.First().Wheels)
        //            .Include(o => o.DrivingLicence)
        //            .Include(o => o.IdentityCard)
        //            .Include(o => o.MarriedWith)
        //            .Include(o => o.OwnedHouses)
        //            .Include(o => o.PreviouslyOwnedVehicles)
        //            .SingleOrDefault();
        //    }
        //    ValidateGetEntityResult(petra);
        //}

        //[TestMethod]
        //public void GetEntityLongTestWithSkipTake()
        //{
        //    FillData();
        //    EQBPerson petra;
        //    using (var session = NHConfig.OpenSession())
        //    {
        //        petra = session.QueryOver<EQBPerson>()
        //            .Where(o => o.Name == "Petra")
        //            .Lock().Upgrade
        //            .Include(o => o.BestFriend)
        //            .Include(o => o.BestFriend.IdentityCard)
        //            .Include(o => o.BestFriend.BestFriend)
        //            .Include(o => o.BestFriend.BestFriend.BestFriend)
        //            .Include(o => o.BestFriend.BestFriend.BestFriend.BestFriend)
        //            .Include(o => o.CurrentOwnedVehicles)
        //            .Include(o => o.CurrentOwnedVehicles.First().Wheels)
        //            .Include(o => o.DrivingLicence)
        //            .Include(o => o.IdentityCard)
        //            .Include(o => o.MarriedWith)
        //            .Include(o => o.OwnedHouses)
        //            .Include(o => o.PreviouslyOwnedVehicles)
        //            .Skip(0).Take(1)
        //            .SingleOrDefault();
        //    }
        //    ValidateGetEntityResult(petra);
        //}

        //[TestMethod]
        //public void GetEntityShortTest()
        //{
        //    FillData();
        //    EQBPerson petra;
        //    using (var session = NHConfig.OpenSession())
        //    {
        //        petra = session.QueryOver<EQBPerson>()
        //            .Where(o => o.Name == "Petra")
        //            .Lock().Upgrade
        //            .Include(o => o.BestFriend.IdentityCard)
        //            .Include(o => o.BestFriend.BestFriend.BestFriend.BestFriend)
        //            .Include(o => o.CurrentOwnedVehicles.First().Wheels)
        //            .Include(o => o.DrivingLicence)
        //            .Include(o => o.IdentityCard)
        //            .Include(o => o.MarriedWith)
        //            .Include(o => o.OwnedHouses)
        //            .Include(o => o.PreviouslyOwnedVehicles)
        //            .SingleOrDefault();
        //    }
        //    ValidateGetEntityResult(petra);
        //}

        #endregion

        #region GetEntities

        [TestMethod]
        public void GetEntitiesLongTest()
        {
            FillData();
            EQBPerson petra;

            ///*NHibernate way*/
            //using (var session = NHConfig.OpenSession())
            //using (var transaction = session.BeginTransaction())
            //{
            //    session.QueryOver<EQBPerson>()
            //           .Fetch(o => o.BestFriend).Eager
            //           .Fetch(o => o.BestFriend.IdentityCard).Eager
            //           .Fetch(o => o.BestFriend.BestFriend).Eager
            //           .Fetch(o => o.BestFriend.BestFriend.BestFriend).Eager
            //           .Fetch(o => o.BestFriend.BestFriend.BestFriend.BestFriend).Eager
            //           .Fetch(o => o.CurrentOwnedVehicles).Eager
            //           .Fetch(o => o.CurrentOwnedVehicles.First().Wheels).Eager
            //           .Fetch(o => o.DrivingLicence).Eager
            //           .Fetch(o => o.IdentityCard).Eager
            //           .Fetch(o => o.MarriedWith).Eager
            //           .Where(o => o.Name == "Petra")
            //           .Future();
            //    session.QueryOver<EQBPerson>()
            //           .Fetch(o => o.OwnedHouses).Eager
            //           .Where(o => o.Name == "Petra")
            //           .Future();
            //    petra = session.QueryOver<EQBPerson>()
            //           .Fetch(o => o.PreviouslyOwnedVehicles).Eager
            //           .Where(o => o.Name == "Petra")
            //           .FutureValue().Value;
            //    //petra = session.QueryOver<EQBPerson>()
            //    //               .Where(o => o.Name == "Petra")
            //    //               .FutureValue().Value;
            //    transaction.Commit();

            //    //Assert.AreEqual(8, GetQueryCount(0));
            //    ClearStatistics();
            //}
            //ValidateGetEntityResult(petra);   

            /*Simplified way - QueryOver*/
            //using (var session = NHConfig.OpenSession())
            //{
            //    petra = session.QueryOver<EQBPerson>()
            //                      .Include(o => o.BestFriend.IdentityCard)
            //                      .Include(o => o.BestFriend.BestFriend.BestFriend.BestFriend)
            //                      .Include(o => o.CurrentOwnedVehicles.First().Wheels)
            //                      .Include(o => o.DrivingLicence)
            //                      .Include(o => o.IdentityCard)
            //                      .Include(o => o.MarriedWith)
            //                      .Include(o => o.OwnedHouses)
            //                      .Include(o => o.PreviouslyOwnedVehicles)
            //                      .Where(o => o.Name == "Petra")
            //                      .SingleOrDefault();

            //    //Assert.AreEqual(8, GetQueryCount(0));
            //    //ClearStatistics();
            //}
            //ValidateGetEntityResult(petra);

            /*Simplified way - Linq*/
            using (var session = NHConfig.OpenSession())
            {
                //session.Query<EQBPerson>()
                //   .Fetch(o => o.BestFriend)
                //    .ThenFetch(o => o.IdentityCard)
                //    .Fetch()


                petra = session.Query<EQBPerson>()
                                  .Include(o => o.BestFriend.IdentityCard)
                                  .Include(o => o.BestFriend.BestFriend.BestFriend.BestFriend)
                                  .Include(o => o.CurrentOwnedVehicles.First().Wheels)
                                  .Include(o => o.DrivingLicence)
                                  .Include(o => o.IdentityCard)
                                  .Include(o => o.MarriedWith)
                                  .Include(o => o.OwnedHouses)
                                  .Include(o => o.PreviouslyOwnedVehicles)
                                  .Where(o => o.Name == "Petra")
                                  .SingleOrDefault();

                //Assert.AreEqual(8, GetQueryCount(0));
                //ClearStatistics();
            }
            ValidateGetEntityResult(petra);
        }

        #endregion

        private int GetQueryCount(int index)
        {
            var factory = (ISessionFactoryImplementor)NHConfig.SessionFactory;
            var match = new Regex("([0-9]+).*").Match(factory.Statistics.Queries[index]);
            return int.Parse(match.Groups[1].Value);
        }

        private void ClearStatistics()
        {
            var factory = (ISessionFactoryImplementor)NHConfig.SessionFactory;
            factory.Statistics.Clear();
        }

        private int GetQueriesCount()
        {
            var factory = (ISessionFactoryImplementor)NHConfig.SessionFactory;
            return factory.Statistics.Queries.Length;
        }

        private void ValidateGetEntityResult(EQBPerson petra)
        {
            Assert.AreEqual(petra.BestFriend.Name, "Ana");
            Assert.AreEqual(petra.BestFriend.BestFriend.Name, "Simon");
            Assert.AreEqual(petra.BestFriend.IdentityCard.Code, "1");
            Assert.AreEqual(petra.BestFriend.BestFriend.BestFriend.Name, "Rok");
            Assert.AreEqual(petra.BestFriend.BestFriend.BestFriend.BestFriend.Name, "Petra");
            Assert.AreEqual(petra.BestFriend.BestFriend.BestFriend.BestFriend.BestFriend.Name, "Ana");
            Assert.AreEqual(petra.CurrentOwnedVehicles.Count, 1);
            Assert.AreEqual(petra.DrivingLicence.Code, "3");
            Assert.AreEqual(petra.IdentityCard.Code, "4");
            Assert.AreEqual(petra.MarriedWith, petra.BestFriend.BestFriend);
            Assert.AreEqual(petra.OwnedHouses.Count, 1);
            Assert.AreEqual(petra.PreviouslyOwnedVehicles.Count, 2);
            foreach (var wheel in petra.CurrentOwnedVehicles.First().Wheels)
            {
                Assert.AreEqual(wheel.Width, 235);
            }
        }

        protected void FillData()
        {
            var ana = new EQBPerson {Age = 23, Name = "Ana"};
            var rok = new EQBPerson {Age = 24, Name = "Rok"};
            var simon = new EQBPerson {Age = 25, Name = "Simon"};
            var petra = new EQBPerson {Age = 22, Name = "Petra"};

            //Setting best friends
            petra.BestFriend = ana;
            ana.BestFriend = simon;
            simon.BestFriend = rok;
            rok.BestFriend = petra;

            //Setting Identity card
            ana.IdentityCard = new EQBIdentityCard {Code = "1", Owner = ana};
            rok.IdentityCard = new EQBIdentityCard { Code = "2", Owner = rok };
            simon.IdentityCard = new EQBIdentityCard { Code = "3", Owner = simon };
            petra.IdentityCard = new EQBIdentityCard { Code = "4", Owner = petra };

            //Setting Driving licence
            rok.DrivingLicence = new EQBDrivingLicence { Code = "1", Owner = rok };
            simon.DrivingLicence = new EQBDrivingLicence { Code = "2", Owner = simon };
            petra.DrivingLicence = new EQBDrivingLicence { Code = "3", Owner = petra };

            //Setting MerriedWith
            rok.MarriedWith = ana;
            ana.MarriedWith = rok;

            petra.MarriedWith = simon;
            simon.MarriedWith = petra;

            //Setting Vehicles
            var ferrari = new EQBVehicle {BuildYear = 2002, Model = "Ferrari"};
            ferrari.Wheels.Add(new TestEQBWheel {Diameter = 45, Width = 320, Vehicle = ferrari});
            ferrari.Wheels.Add(new TestEQBWheel { Diameter = 45, Width = 320, Vehicle = ferrari });
            ferrari.Wheels.Add(new TestEQBWheel { Diameter = 45, Width = 260, Vehicle = ferrari });
            ferrari.Wheels.Add(new TestEQBWheel { Diameter = 45, Width = 260, Vehicle = ferrari });

            var audi = new EQBVehicle {BuildYear = 2009, Model = "Audi"};
            audi.Wheels.Add(new TestEQBWheel { Diameter = 45, Width = 235, Vehicle = audi });
            audi.Wheels.Add(new TestEQBWheel { Diameter = 45, Width = 235, Vehicle = audi });
            audi.Wheels.Add(new TestEQBWheel { Diameter = 45, Width = 235, Vehicle = audi });
            audi.Wheels.Add(new TestEQBWheel { Diameter = 45, Width = 235, Vehicle = audi });

            var bmw = new EQBVehicle {BuildYear = 1993, Model = "Bmw"};
            bmw.Wheels.Add(new TestEQBWheel { Diameter = 45, Width = 205, Vehicle = bmw });
            bmw.Wheels.Add(new TestEQBWheel { Diameter = 45, Width = 205, Vehicle = bmw });
            bmw.Wheels.Add(new TestEQBWheel { Diameter = 45, Width = 205, Vehicle = bmw });
            bmw.Wheels.Add(new TestEQBWheel { Diameter = 45, Width = 205, Vehicle = bmw });

            var vw = new EQBVehicle {BuildYear = 2002, Model = "Vw"};
            vw.Wheels.Add(new TestEQBWheel { Diameter = 45, Width = 195, Vehicle = vw });
            vw.Wheels.Add(new TestEQBWheel { Diameter = 45, Width = 195, Vehicle = vw });
            vw.Wheels.Add(new TestEQBWheel { Diameter = 45, Width = 195, Vehicle = vw });
            vw.Wheels.Add(new TestEQBWheel { Diameter = 45, Width = 195, Vehicle = vw });

            petra.PreviouslyOwnedVehicles.Add(vw);
            petra.PreviouslyOwnedVehicles.Add(bmw);
            petra.CurrentOwnedVehicles.Add(audi);
            audi.CurrentOwner = petra;

            simon.PreviouslyOwnedVehicles.Add(bmw);
            simon.PreviouslyOwnedVehicles.Add(audi);
            simon.CurrentOwnedVehicles.Add(ferrari);
            ferrari.CurrentOwner = simon;

            //Setting Houses
            var house1 = new EQBHouse {Address = "Address1"};
            var house2 = new EQBHouse {Address = "Address2"};

            house1.Owners.Add(ana);
            ana.OwnedHouses.Add(house1);
            house1.Owners.Add(rok);
            rok.OwnedHouses.Add(house1);

            house2.Owners.Add(simon);
            simon.OwnedHouses.Add(house2);
            house2.Owners.Add(petra);
            petra.OwnedHouses.Add(house2);

            //Saving entities
            using (var session = NHConfig.OpenSession())
            {
                session.Save(petra);
                session.Save(rok);
                session.Save(simon);
                session.Save(ana);
                session.Flush();
            }
        }
    }
}
