using System;

namespace NHibernate.Extensions.Tests.Entities
{
    public partial class EQBRoadworthyTest : Entity
    {
        public virtual DateTime TestDate { get; set; }

        public virtual bool Passed { get; set; }

        public virtual string Comments { get; set; }

        public virtual EQBVehicle Vehicle { get; set; }
    }
}
