using System;

namespace NHibernate.Extensions
{
    public class DeepCloneMemberOptions : IDeepCloneMemberOptions
    {
        public string MemberName { get; set; }

        public Func<object, object> ResolveUsing { get; set; }

        public bool Ignore { get; set; }

        public bool CloneAsReference { get; set; }

        public Func<object, object> Filter { get; set; }
    }

    public class DeepCloneMemberOptions<TType, TMember> : DeepCloneMemberOptions, IDeepCloneMemberOptions<TType, TMember>
    {
        public new IDeepCloneMemberOptions<TType, TMember> ResolveUsing(Func<TType, TMember> func)
        {
            base.ResolveUsing = func == null ? (Func<object, object>)null : o => func((TType)o);
            return this;
        }

        public new IDeepCloneMemberOptions<TType, TMember> Ignore(bool value = true)
        {
            base.Ignore = value;
            return this;
        }

        public new IDeepCloneMemberOptions<TType, TMember> CloneAsReference(bool value = true)
        {
            base.CloneAsReference = value;
            return this;
        }

        public new IDeepCloneMemberOptions<TType, TMember> Filter(Func<TMember, TMember> func)
        {
            base.Filter = func == null ? (Func<object, object>)null : o => func((TMember)o);
            return this;
        }
    }
}
