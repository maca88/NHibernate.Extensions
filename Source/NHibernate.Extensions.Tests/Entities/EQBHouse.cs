using System.Collections.Generic;
using FluentNHibernate.Automapping;
using FluentNHibernate.Automapping.Alterations;

namespace NHibernate.Extensions.Tests.Entities
{
    public partial class EQBHouse : Entity
    {
        public EQBHouse()
        {
            Owners = new HashSet<EQBPerson>();
        }

        public virtual string Address { get; set; }

        public virtual ISet<EQBPerson> Owners { get; set; }
    }

    public class EQBHouseMapping : IAutoMappingOverride<EQBHouse>
    {
        public void Override(AutoMapping<EQBHouse> mapping)
        {
            mapping.HasManyToMany(o => o.Owners).Inverse();
        }
    }
}
