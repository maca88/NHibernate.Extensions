using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NHibernate.Extensions.Tests.Entities
{
    public class EQBUser : Entity, IUser
    {
        public virtual string UserName { get; set; }

        public virtual EQBUser Related { get; set; }
    }
}
