namespace NHibernate.Extensions.Tests.Entities
{
    public partial class TestEQBWheel : Entity
    {
        public virtual int Diameter { get; set; }

        public virtual int Width { get; set; }

        public virtual EQBVehicle Vehicle { get; set; }
    }
}
