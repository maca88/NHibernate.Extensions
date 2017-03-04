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
            Assert.AreEqual(results[0][0], "BestFriend");
            Assert.AreEqual(results[0][1], "BestFriend.IdentityCard");

            Assert.AreEqual(results[1][0], "BestFriend");
            Assert.AreEqual(results[1][1], "BestFriend.BestFriend");
            Assert.AreEqual(results[1][2], "BestFriend.BestFriend.BestFriend");
            Assert.AreEqual(results[1][3], "BestFriend.BestFriend.BestFriend.BestFriend");
        }

        [TestMethod]
        public void Test2()
        {
            var tree = new QueryRelationTree();
            Expression<Func<EQBPerson, object>> AB = person => person.BestFriend.IdentityCard;
            Expression<Func<EQBPerson, object>> AAAA = person => person.BestFriend.BestFriend.BestFriend.BestFriend;
            Expression<Func<EQBPerson, object>> CD = person => person.CurrentOwnedVehicles.First().Wheels;

            //Input
            tree.AddNode(AB);
            tree.AddNode(AAAA);
            tree.AddNode(CD);

            var results = tree.DeepFirstSearch();
            //Output
            Assert.AreEqual(results[0][0], "BestFriend");
            Assert.AreEqual(results[0][1], "BestFriend.IdentityCard");

            Assert.AreEqual(results[1][0], "BestFriend");
            Assert.AreEqual(results[1][1], "BestFriend.BestFriend");
            Assert.AreEqual(results[1][2], "BestFriend.BestFriend.BestFriend");
            Assert.AreEqual(results[1][3], "BestFriend.BestFriend.BestFriend.BestFriend");

            Assert.AreEqual(results[2][0], "CurrentOwnedVehicles");
            Assert.AreEqual(results[2][1], "CurrentOwnedVehicles.Wheels");
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
            Assert.AreEqual(results[0][0], "Identity");
            Assert.AreEqual(results[1][0], "IdentityCard");
        }
    }
}
