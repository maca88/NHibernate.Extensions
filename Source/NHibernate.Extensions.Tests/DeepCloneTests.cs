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
                clone = session.DeepClone(petra, o => o
                    .ForType<EQBPerson>(t => 
                        t.ForMember(m => m.Name, opts => opts.Ignore())
                        ));
                // Lazy load some relations after cloning
                var friend = petra.BestFriend;
                var card = petra.IdentityCard;

            }
            Assert.AreEqual(petra.Id, clone.Id);
            Assert.AreEqual(null, clone.Name);
            Assert.AreEqual(petra.LastName, clone.LastName);
            Assert.IsNotNull(petra.BestFriend);
            Assert.IsNotNull(petra.IdentityCard);
            Assert.IsNull(clone.MarriedWith);
            Assert.IsNull(clone.BestFriend);
            Assert.IsNull(clone.IdentityCard);
            Assert.AreEqual(0, clone.OwnedHouses.Count);
            Assert.AreEqual(0, clone.CurrentOwnedVehicles.Count);
        }

        [TestMethod]
        public void deep_clone_as_reference_and_ignore_properties_identifiers()
        {
            EQBPerson clone;
            EQBPerson petra;

            using (var session = NHConfig.OpenSession())
            {
                petra = session.Query<EQBPerson>()
                    .Include(o => o.IdentityCard)
                    .First(o => o.Name == "Petra");
                clone = session.DeepClone(petra, o => o
                    .ForType<EQBPerson>(t => t
                        .ForMember(m => m.Name, opts => opts.Ignore())
                        .CloneIdentifier(false)
                    )
                    .CloneIdentifier(true)
                    .CanCloneAsReference(type => type == typeof(EQBIdentityCard))
                    );

            }
            Assert.AreEqual(default(int), clone.Id);
            Assert.IsNull(clone.Name);
            Assert.AreEqual(petra.LastName, clone.LastName);
            Assert.AreEqual(petra.IdentityCard, clone.IdentityCard);
        }


        [TestMethod]
        public void deep_clone_references()
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
                    .Include(o => o.CurrentOwnedVehicles.First().RoadworthyTests)
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
            Assert.AreEqual(2, clone.CurrentOwnedVehicles.First().RoadworthyTests.Count);
            Assert.AreEqual(clone.CurrentOwnedVehicles.First(), clone.CurrentOwnedVehicles.First().RoadworthyTests[new DateTime(2009, 2, 1)].Vehicle);

            Assert.AreEqual(2, clone.PreviouslyOwnedVehicles.Count);
            Assert.AreEqual(clone, clone.CurrentOwnedVehicles.First().CurrentOwner);
        }

        [TestMethod]
        public void deep_clone_filter()
        {
            EQBPerson clone;
            EQBPerson petra;

            using (var session = NHConfig.OpenSession())
            {
                petra = session.Query<EQBPerson>()
                    .Include(o => o.PreviouslyOwnedVehicles)
                    .First(o => o.Name == "Petra");

                clone = session.DeepClone(petra, o => o
                    .ForType<EQBPerson>(t => t
                    .ForMember(m => m.Name, m => m.Filter(n => n  + "2"))
                        .ForMember(m => m.PreviouslyOwnedVehicles, m => m
                            .Filter(col => new HashSet<EQBVehicle>(col.Take(1)))
                        )
                    ));
            }

            Assert.AreEqual("Petra2", clone.Name);
            Assert.IsNull(clone.BestFriend);
            Assert.IsNull(clone.IdentityCard);
            Assert.IsNull(clone.MarriedWith);
            Assert.AreEqual(0, clone.CurrentOwnedVehicles.Count);
            Assert.AreEqual(1, clone.PreviouslyOwnedVehicles.Count);
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
                    .Include(o => o.CurrentOwnedVehicles.First().RoadworthyTests)
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
            Assert.AreEqual(2, clone.CurrentOwnedVehicles.First().RoadworthyTests.Count);
            Assert.IsNull(clone.CurrentOwnedVehicles.First().RoadworthyTests.First().Value.Vehicle);
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
                    .Include(o => o.CurrentOwnedVehicles.First().RoadworthyTests)
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
            Assert.AreEqual(2, clone.CurrentOwnedVehicles.First().RoadworthyTests.Count);
            Assert.AreEqual(0, clone.CurrentOwnedVehicles.First().RoadworthyTests.First().Value.Id);
            Assert.AreEqual(0, clone.CurrentOwnedVehicles.First().RoadworthyTests.First().Value.Vehicle.Id);
        }
    }
}
