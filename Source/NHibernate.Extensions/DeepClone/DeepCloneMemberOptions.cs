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

        IDeepCloneMemberOptions IDeepCloneMemberOptions.ResolveUsing(Func<object, object> func)
        {
            ResolveUsing = func;
            return this;
        }

        IDeepCloneMemberOptions IDeepCloneMemberOptions.Ignore(bool value)
        {
            Ignore = value;
            return this;
        }

        IDeepCloneMemberOptions IDeepCloneMemberOptions.CloneAsReference(bool value)
        {
            CloneAsReference = value;
            return this;
        }

        IDeepCloneMemberOptions IDeepCloneMemberOptions.Filter(Func<object, object> func)
        {
            Filter = func;
            return this;
        }
    }

    public class DeepCloneMemberOptions<TType, TMember> : DeepCloneMemberOptions, IDeepCloneMemberOptions<TType, TMember>
    {
        IDeepCloneMemberOptions<TType, TMember> IDeepCloneMemberOptions<TType, TMember>.ResolveUsing(Func<TType, TMember> func)
        {
            ResolveUsing = func == null ? (Func<object, object>)null : o => func((TType)o);
            return this;
        }

        IDeepCloneMemberOptions<TType, TMember> IDeepCloneMemberOptions<TType, TMember>.Ignore(bool value)
        {
            Ignore = value;
            return this;
        }

        IDeepCloneMemberOptions<TType, TMember> IDeepCloneMemberOptions<TType, TMember>.CloneAsReference(bool value)
        {
            CloneAsReference = value;
            return this;
        }

        IDeepCloneMemberOptions<TType, TMember> IDeepCloneMemberOptions<TType, TMember>.Filter(Func<TMember, TMember> func)
        {
            Filter = func == null ? (Func<object, object>)null : o => func((TMember)o);
            return this;
        }
    }
}
