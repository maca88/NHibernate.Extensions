using System;

namespace NHibernate.Extensions
{
    public class DeepCloneMemberOptions : IDeepCloneMemberOptions
    {
        public string MemberName { get; set; }

        public Func<object, object> ResolveUsing { get; set; }

        public bool IsResolveUsingSet { get; set; }

        public bool Ignore { get; set; }

        public bool CloneAsReference { get; set; }
    }

    public class DeepCloneMemberOptions<TType> : DeepCloneMemberOptions, IDeepCloneMemberOptions<TType>
    {
        public IDeepCloneMemberOptions<TType> ResolveUsing(Func<TType, object> func)
        {
            base.ResolveUsing = o => func((TType)o);
            IsResolveUsingSet = true;
            return this;
        }

        public IDeepCloneMemberOptions<TType> Ignore(bool value = true)
        {
            base.Ignore = value;
            return this;
        }

        public IDeepCloneMemberOptions<TType> CloneAsReference(bool value = true)
        {
            base.CloneAsReference = value;
            return this;
        }
    }
}
