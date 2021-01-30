using System;

namespace NHibernate.Extensions
{
    public interface IDeepCloneMemberOptions
    {
        IDeepCloneMemberOptions ResolveUsing(Func<object, object> func);

        IDeepCloneMemberOptions Ignore(bool value = true);

        IDeepCloneMemberOptions CloneAsReference(bool value = true);

        IDeepCloneMemberOptions Filter(Func<object, object> func);
    }

    public interface IDeepCloneMemberOptions<out TType, TMember>
    {
        IDeepCloneMemberOptions<TType, TMember> ResolveUsing(Func<TType, TMember> func);

        IDeepCloneMemberOptions<TType, TMember> Ignore(bool value = true);

        IDeepCloneMemberOptions<TType, TMember> CloneAsReference(bool value = true);

        IDeepCloneMemberOptions<TType, TMember> Filter(Func<TMember, TMember> func);
    }
}
