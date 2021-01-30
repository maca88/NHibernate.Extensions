using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NHibernate.Extensions.Tests.Entities
{
    public class Animal : Entity
    {
        public virtual string Name { get; set; }

        public virtual AnimalType Type { get; set; }
    }
}
