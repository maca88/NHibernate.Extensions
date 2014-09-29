namespace NHibernate.Extensions.Tests
{
    public interface IEntity
    {
    }

    public abstract partial class Entity : IEntity
    {
        public virtual int Id { get; set; }

        public virtual bool IsTransient()
        {
            return Id.Equals(default(int));
        }

        public virtual System.Type GetTypeUnproxied()
        {
            return GetType();
        }

        public virtual bool Equals(object obj)
        {
            var compareTo = obj as Entity;
            if (ReferenceEquals(this, compareTo))
                return true;
            if (compareTo == null || GetType() != compareTo.GetTypeUnproxied())
                return false;
            return HasSameNonDefaultIdAs(compareTo);
        }

        public virtual int GetHashCode()
        {
            if(IsTransient())
                return base.GetHashCode();
            return (GetType().GetHashCode() * 31) ^ Id.GetHashCode();
        }

        private bool HasSameNonDefaultIdAs(Entity compareTo)
        {
            return !IsTransient() && !compareTo.IsTransient() && Id.Equals(compareTo.Id);
        }
    }
}
