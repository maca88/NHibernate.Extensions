namespace NHibernate.Extensions.Tests.Entities
{
    public partial class EQBIdentityCard : Entity
    {
        public virtual string Code { get; set; }

        public virtual EQBPerson Owner { get; set; }
    }
}
