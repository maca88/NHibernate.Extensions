using System;
using System.CodeDom;
using System.Collections.Generic;
using FluentNHibernate.Automapping;
using FluentNHibernate.Automapping.Alterations;

namespace NHibernate.Extensions.Tests.Entities
{
    public partial class EQBVehicle : Entity
    {
        public EQBVehicle()
        {
            Wheels = new HashSet<TestEQBWheel>();
            PreviousUsers = new HashSet<EQBPerson>();
            RoadworthyTests = new Dictionary<DateTime, EQBRoadworthyTest>();
            MileageHistory = new SortedDictionary<DateTime, double>();
        }

        public virtual string Model { get; set; }

        public virtual int BuildYear { get; set; }

        public virtual EQBPerson CurrentOwner { get; set; }

        public virtual ISet<EQBPerson> PreviousUsers { get; set; }

        public virtual ISet<TestEQBWheel> Wheels { get; set; }

        public virtual IDictionary<DateTime, EQBRoadworthyTest> RoadworthyTests { get; set; }

        public virtual IDictionary<DateTime, double> MileageHistory { get; set; }
    }

    public class EQBVehicleMapping : IAutoMappingOverride<EQBVehicle>
    {
        public void Override(AutoMapping<EQBVehicle> mapping)
        {
            mapping.HasManyToMany(o => o.PreviousUsers).Inverse();
            mapping.HasMany(o => o.Wheels).KeyColumn("VehicleId");
            mapping.HasMany(o => o.RoadworthyTests).AsMap<DateTime>(rwt => rwt.TestDate);
            mapping.HasMany(o => o.MileageHistory).AsMap<DateTime>("ObservationDate").Element("Mileage");
        }
    }
}
