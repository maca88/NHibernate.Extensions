﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by AsyncGenerator.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------


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
    public partial class LinqIncludeTests : BaseIncludeTest
    {

        [Test]
        public async Task TestUsingAndThenIncludeAsync()
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
                petra = await (future.GetValueAsync());
                CheckStatistics(stats, 5);
            }
            ValidateGetEntityResult(petra);
        }

        #region FutureValue

        #endregion
        #region SingleOrDefault

        #endregion
        #region Single

        #endregion
        #region FirstOrDefault

        #endregion
        #region First

        #endregion
        #region LastOrDefault

        #endregion
    }
}
