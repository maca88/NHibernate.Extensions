namespace NHibernate.Extensions.Tests.Entities
{
    public partial class EQBDrivingLicence : Entity
    {
        public virtual string Code { get; set; }

        public virtual EQBPerson Owner { get; set; }
    }
}
