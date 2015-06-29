using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NHibernate.Extensions.Tests.Entities;
using NHibernate.Linq;
using T4FluentNH.Tests;

namespace NHibernate.Extensions.Tests
{
    [TestClass]
    public class DeepCloneTests : BaseIncludeTest
    {
        [TestMethod]
        public void deep_clone_simple_properties()
        {
            EQBPerson clone;
            EQBPerson petra;

            using (var session = NHConfig.OpenSession())
            {
                petra = session.Query<EQBPerson>()
                    .First(o => o.Name == "Petra");

                clone = session.DeepClone(petra);
            }

            Assert.AreEqual(petra.Id, clone.Id);
            Assert.AreEqual(petra.Name, clone.Name);
            Assert.AreEqual(petra.LastName, clone.LastName);
            Assert.IsNull(clone.MarriedWith);
            Assert.IsNull(clone.BestFriend);
            Assert.IsNull(clone.IdentityCard);
            Assert.AreEqual(0, clone.OwnedHouses.Count);
            Assert.AreEqual(0, clone.CurrentOwnedVehicles.Count);
        }


        [TestMethod]
        public void deep_clone_refereces()
        {
            EQBPerson clone;
            EQBPerson petra;

            using (var session = NHConfig.OpenSession())
            {
                petra = session.Query<EQBPerson>()
                    .Include(o => o.BestFriend.IdentityCard)
                    .Include(o => o.BestFriend.BestFriend)
                    .First(o => o.Name == "Petra");

                clone = session.DeepClone(petra);
            }

            Assert.AreEqual(petra.Id, clone.Id);
            Assert.IsNotNull(clone.BestFriend);
            Assert.IsNotNull(clone.BestFriend.IdentityCard);
            Assert.AreEqual(clone.BestFriend, clone.BestFriend.IdentityCard.Owner);
            Assert.IsNotNull(clone.BestFriend.BestFriend);

            Assert.IsNull(clone.BestFriend.BestFriend.BestFriend);
            Assert.IsNull(clone.IdentityCard);
            Assert.AreEqual(0, clone.OwnedHouses.Count);
            Assert.AreEqual(0, clone.CurrentOwnedVehicles.Count);
        }

        [TestMethod]
        public void deep_clone_collections()
        {
            EQBPerson clone;
            EQBPerson petra;

            using (var session = NHConfig.OpenSession())
            {
                petra = session.Query<EQBPerson>()
                    .Include(o => o.CurrentOwnedVehicles.First().Wheels)
                    .Include(o => o.PreviouslyOwnedVehicles)
                    .First(o => o.Name == "Petra");

                clone = session.DeepClone(petra);
            }

            Assert.IsNull(clone.BestFriend);
            Assert.IsNull(clone.IdentityCard);
            Assert.IsNull(clone.MarriedWith);
            Assert.AreEqual(1, clone.CurrentOwnedVehicles.Count);
            Assert.AreEqual(clone, clone.CurrentOwnedVehicles.First().CurrentOwner);
            Assert.AreEqual(4, clone.CurrentOwnedVehicles.First().Wheels.Count);
            Assert.AreEqual(clone.CurrentOwnedVehicles.First(), clone.CurrentOwnedVehicles.First().Wheels.First().Vehicle);

            Assert.AreEqual(clone.PreviouslyOwnedVehicles.Count, 2);
            Assert.AreEqual(clone, clone.CurrentOwnedVehicles.First().CurrentOwner);
        }

        [TestMethod]
        public void deep_clone_with_skip_entity_types()
        {
            EQBPerson clone;
            EQBPerson petra;

            using (var session = NHConfig.OpenSession())
            {
                petra = session.Query<EQBPerson>()
                    .Include(o => o.IdentityCard)
                    .Include(o => o.BestFriend.IdentityCard)
                    .Include(o => o.CurrentOwnedVehicles.First().Wheels)
                    .First(o => o.Name == "Petra");

                clone = session.DeepClone(petra, o => o
                    .SkipEntityTypes());
            }

            Assert.IsNull(clone.IdentityCard);
            Assert.IsNull(clone.BestFriend);
            Assert.AreEqual(1, clone.CurrentOwnedVehicles.Count);
            Assert.IsNull(clone.CurrentOwnedVehicles.First().CurrentOwner);
            Assert.AreEqual(4, clone.CurrentOwnedVehicles.First().Wheels.Count);
            Assert.IsNull(clone.CurrentOwnedVehicles.First().Wheels.First().Vehicle);
        }

        [TestMethod]
        public void deep_clone_without_identifier()
        {
            EQBPerson clone;

            using (var session = NHConfig.OpenSession())
            {
                var petra = session.Query<EQBPerson>()
                    .Include(o => o.IdentityCard)
                    .Include(o => o.BestFriend.IdentityCard)
                    .Include(o => o.CurrentOwnedVehicles.First().Wheels)
                    .First(o => o.Name == "Petra");

                clone = session.DeepClone(petra, o => o
                    .CloneIdentifier(false));
            }

            Assert.AreEqual(0, clone.Id);
            Assert.AreEqual(0, clone.IdentityCard.Id);
            Assert.AreEqual(0, clone.IdentityCard.Owner.Id);
            Assert.AreEqual(0, clone.BestFriend.Id);
            Assert.AreEqual(0, clone.BestFriend.IdentityCard.Id);
            Assert.AreEqual(1, clone.CurrentOwnedVehicles.Count);
            Assert.AreEqual(0, clone.CurrentOwnedVehicles.First().Id);
            Assert.AreEqual(0, clone.CurrentOwnedVehicles.First().CurrentOwner.Id);
            Assert.AreEqual(4, clone.CurrentOwnedVehicles.First().Wheels.Count);
            Assert.AreEqual(0, clone.CurrentOwnedVehicles.First().Wheels.First().Id);
            Assert.AreEqual(0, clone.CurrentOwnedVehicles.First().Wheels.First().Vehicle.Id);
        }


    }
}
