using System;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NHibernate.Extensions.Tests.Entities;

namespace NHibernate.Extensions.Tests
{
    [TestClass]
    public class QueryRelationTreeTests
    {
        [TestMethod]
        public void Test1()
        {
            var tree = new QueryRelationTree();
            Expression<Func<EQBPerson, object>> A = person => person.BestFriend;
            Expression<Func<EQBPerson, object>> AB = person => person.BestFriend.IdentityCard;
            Expression<Func<EQBPerson, object>> AA = person => person.BestFriend.BestFriend;
            Expression<Func<EQBPerson, object>> AAA = person => person.BestFriend.BestFriend.BestFriend;
            Expression<Func<EQBPerson, object>> AAAA = person => person.BestFriend.BestFriend.BestFriend.BestFriend;

            //Input
            tree.AddNode(A);
            tree.AddNode(AB);
            tree.AddNode(AA);
            tree.AddNode(AAA);
            tree.AddNode(AAAA);

            var results = tree.DeepFirstSearch();
            //Output
            Assert.AreEqual("BestFriend", results[0][0]);
            Assert.AreEqual("BestFriend.IdentityCard", results[0][1]);

            Assert.AreEqual("BestFriend", results[1][0]);
            Assert.AreEqual("BestFriend.BestFriend", results[1][1]);
            Assert.AreEqual("BestFriend.BestFriend.BestFriend", results[1][2]);
            Assert.AreEqual("BestFriend.BestFriend.BestFriend.BestFriend", results[1][3]);
        }

        [TestMethod]
        public void Test2()
        {
            var tree = new QueryRelationTree();
            Expression<Func<EQBPerson, object>> AB = person => person.BestFriend.IdentityCard;
            Expression<Func<EQBPerson, object>> AAAA = person => person.BestFriend.BestFriend.BestFriend.BestFriend;
            Expression<Func<EQBPerson, object>> CD = person => person.CurrentOwnedVehicles.First().Wheels;
            Expression<Func<EQBPerson, object>> CE = person => person.CurrentOwnedVehicles.First().RoadworthyTests;

            //Input
            tree.AddNode(AB);
            tree.AddNode(AAAA);
            tree.AddNode(CD);
            tree.AddNode(CE);

            var results = tree.DeepFirstSearch();
            //Output
            Assert.AreEqual("BestFriend", results[0][0]);
            Assert.AreEqual("BestFriend.IdentityCard", results[0][1]);

            Assert.AreEqual("BestFriend", results[1][0]);
            Assert.AreEqual("BestFriend.BestFriend", results[1][1]);
            Assert.AreEqual("BestFriend.BestFriend.BestFriend", results[1][2]);
            Assert.AreEqual("BestFriend.BestFriend.BestFriend.BestFriend", results[1][3]);

            Assert.AreEqual("CurrentOwnedVehicles", results[2][0]);
            Assert.AreEqual("CurrentOwnedVehicles.Wheels", results[2][1]);

            Assert.AreEqual("CurrentOwnedVehicles", results[3][0]);
            Assert.AreEqual("CurrentOwnedVehicles.RoadworthyTests", results[3][1]);
        }

        [TestMethod]
        public void Test3()
        {
            var tree = new QueryRelationTree();
            Expression<Func<EQBPerson, object>> AB = person => person.Identity;
            Expression<Func<EQBPerson, object>> CD = person => person.IdentityCard;

            //Input
            tree.AddNode(AB);
            tree.AddNode(CD);

            var results = tree.DeepFirstSearch();
            //Output
            Assert.AreEqual("Identity", results[0][0]);
            Assert.AreEqual("IdentityCard", results[1][0]);
        }
    }
}
