using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NHibernate.Engine;
using NHibernate.Extensions.Tests.Entities;
using NHibernate.Tool.hbm2ddl;

namespace NHibernate.Extensions.Tests
{
    public abstract class BaseIncludeTest
    {
        protected void ValidateGetEntityResult(EQBPerson petra)
        {
            Assert.AreEqual("Ana", petra.BestFriend.Name);
            Assert.AreEqual("Simon", petra.BestFriend.BestFriend.Name);
            Assert.AreEqual("1", petra.BestFriend.IdentityCard.Code);
            Assert.AreEqual("Rok", petra.BestFriend.BestFriend.BestFriend.Name);
            Assert.AreEqual("Petra", petra.BestFriend.BestFriend.BestFriend.BestFriend.Name);
            Assert.AreEqual("Ana", petra.BestFriend.BestFriend.BestFriend.BestFriend.BestFriend.Name);
            Assert.AreEqual(1, petra.CurrentOwnedVehicles.Count);
            Assert.AreEqual("3", petra.DrivingLicence.Code);
            Assert.AreEqual("4", petra.IdentityCard.Code);
            Assert.AreEqual("System", petra.CreatedBy.UserName);
            Assert.AreEqual(petra.BestFriend.BestFriend, petra.MarriedWith);
            Assert.AreEqual(1, petra.OwnedHouses.Count);
            Assert.AreEqual(2, petra.PreviouslyOwnedVehicles.Count);
            Assert.AreEqual("Audi", petra.CurrentOwnedVehicles.First().Model);
            Assert.AreEqual(2, petra.CurrentOwnedVehicles.First().RoadworthyTests.Count);
            Assert.AreEqual(2, petra.CurrentOwnedVehicles.First().MileageHistory.Count);
            Assert.AreEqual(5000, petra.CurrentOwnedVehicles.First().MileageHistory[new DateTime(2010, 1, 1)]);
            foreach (var wheel in petra.CurrentOwnedVehicles.First().Wheels)
            {
                Assert.AreEqual(235, wheel.Width);
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

        [TestCleanup]
        public void Cleanup()
        {
            var schema = new SchemaExport(NHConfig.Configuration);
            schema.Drop(false, true);
        }

        protected void FillData()
        {
            var system = new EQBUser { UserName = "System" };
            var ana = new EQBPerson("Ana") { Age = 23, CreatedBy = system };
            var rok = new EQBPerson("Rok") { Age = 24, CreatedBy = system };
            var simon = new EQBPerson("Simon") { Age = 25, CreatedBy = system };
            var petra = new EQBPerson("Petra") { Age = 22, CreatedBy = system };

            //Setting best friends
            petra.BestFriend = ana;
            ana.BestFriend = simon;
            simon.BestFriend = rok;
            rok.BestFriend = petra;

            //Setting Identity card
            ana.IdentityCard = new EQBIdentityCard { Code = "1", Owner = ana };
            ana.Identity = new EQBIdentity { Code = "1", Owner = ana };
            rok.IdentityCard = new EQBIdentityCard { Code = "2", Owner = rok };
            rok.Identity = new EQBIdentity { Code = "2", Owner = rok };
            simon.IdentityCard = new EQBIdentityCard { Code = "3", Owner = simon };
            simon.Identity = new EQBIdentity { Code = "3", Owner = simon };
            petra.IdentityCard = new EQBIdentityCard { Code = "4", Owner = petra };
            petra.Identity = new EQBIdentity { Code = "4", Owner = petra };

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
            var ferrari = new EQBVehicle { BuildYear = 2002, Model = "Ferrari" };
            ferrari.Wheels.Add(new TestEQBWheel { Diameter = 45, Width = 320, Vehicle = ferrari });
            ferrari.Wheels.Add(new TestEQBWheel { Diameter = 45, Width = 320, Vehicle = ferrari });
            ferrari.Wheels.Add(new TestEQBWheel { Diameter = 45, Width = 260, Vehicle = ferrari });
            ferrari.Wheels.Add(new TestEQBWheel { Diameter = 45, Width = 260, Vehicle = ferrari });
            ferrari.RoadworthyTests.Add(
                new DateTime(2002, 2, 1),
                new EQBRoadworthyTest
                {
                    Vehicle = ferrari,
                    TestDate = new DateTime(2002, 2, 1),
                    Passed = true,
                    Comments = "I like the shade of red."
                });
            ferrari.MileageHistory.Add(new DateTime(2002, 1, 1), 0);
            ferrari.MileageHistory.Add(new DateTime(2006, 1, 1), 60000);
            ferrari.MileageHistory.Add(new DateTime(2010, 1, 1), 100000);

            var audi = new EQBVehicle { BuildYear = 2009, Model = "Audi" };
            audi.Wheels.Add(new TestEQBWheel { Diameter = 45, Width = 235, Vehicle = audi });
            audi.Wheels.Add(new TestEQBWheel { Diameter = 45, Width = 235, Vehicle = audi });
            audi.Wheels.Add(new TestEQBWheel { Diameter = 45, Width = 235, Vehicle = audi });
            audi.Wheels.Add(new TestEQBWheel { Diameter = 45, Width = 235, Vehicle = audi });
            audi.RoadworthyTests.Add(
                new DateTime(2009, 2, 1),
                new EQBRoadworthyTest
                {
                    Vehicle = audi,
                    TestDate = new DateTime(2009, 2, 1),
                    Passed = false,
                    Comments = "Brakes failing."
                });
            audi.RoadworthyTests.Add(
                new DateTime(2009, 3, 1),
                new EQBRoadworthyTest
                {
                    Vehicle = audi,
                    TestDate = new DateTime(2009, 3, 1),
                    Passed = true,
                    Comments = "All good now."
                });
            audi.MileageHistory.Add(new DateTime(2009, 1, 1), 0);
            audi.MileageHistory.Add(new DateTime(2010, 1, 1), 5000);

            var bmw = new EQBVehicle { BuildYear = 1993, Model = "Bmw" };
            bmw.Wheels.Add(new TestEQBWheel { Diameter = 45, Width = 205, Vehicle = bmw });
            bmw.Wheels.Add(new TestEQBWheel { Diameter = 45, Width = 205, Vehicle = bmw });
            bmw.Wheels.Add(new TestEQBWheel { Diameter = 45, Width = 205, Vehicle = bmw });
            bmw.Wheels.Add(new TestEQBWheel { Diameter = 45, Width = 205, Vehicle = bmw });
            // Deliberately no roadworthy tests or mileage history

            var vw = new EQBVehicle { BuildYear = 2002, Model = "Vw" };
            vw.Wheels.Add(new TestEQBWheel { Diameter = 45, Width = 195, Vehicle = vw });
            vw.Wheels.Add(new TestEQBWheel { Diameter = 45, Width = 195, Vehicle = vw });
            vw.Wheels.Add(new TestEQBWheel { Diameter = 45, Width = 195, Vehicle = vw });
            vw.Wheels.Add(new TestEQBWheel { Diameter = 45, Width = 195, Vehicle = vw });
            vw.RoadworthyTests.Add(
                new DateTime(2002, 3, 1),
                new EQBRoadworthyTest
                {
                    Vehicle = vw,
                    TestDate = new DateTime(2002, 3, 1),
                    Passed = true,
                    Comments = "No problems."
                });
            vw.MileageHistory.Add(new DateTime(2002, 1, 1), 0);
            vw.MileageHistory.Add(new DateTime(2015, 1, 1), 150000);

            petra.PreviouslyOwnedVehicles.Add(vw);
            petra.PreviouslyOwnedVehicles.Add(bmw);
            petra.CurrentOwnedVehicles.Add(audi);
            audi.CurrentOwner = petra;

            simon.PreviouslyOwnedVehicles.Add(bmw);
            simon.PreviouslyOwnedVehicles.Add(audi);
            simon.CurrentOwnedVehicles.Add(ferrari);
            ferrari.CurrentOwner = simon;

            //Setting Houses
            var house1 = new EQBHouse { Address = "Address1" };
            var house2 = new EQBHouse { Address = "Address2" };

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
