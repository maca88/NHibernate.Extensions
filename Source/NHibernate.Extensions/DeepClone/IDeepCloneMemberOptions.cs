using System;

namespace NHibernate.Extensions
{
    public interface IDeepCloneMemberOptions
    {
        string MemberName { get; set; }

        Func<object, object> ResolveUsing { get; set; }

        bool Ignore { get; set; }

        bool CloneAsReference { get; set; }

        Func<object, object> Filter { get; set; }
    }

    public interface IDeepCloneMemberOptions<out TType, TMember>
    {
        IDeepCloneMemberOptions<TType, TMember> ResolveUsing(Func<TType, TMember> func);

        IDeepCloneMemberOptions<TType, TMember> Ignore(bool value = true);

        IDeepCloneMemberOptions<TType, TMember> CloneAsReference(bool value = true);

        IDeepCloneMemberOptions<TType, TMember> Filter(Func<TMember, TMember> func);
    }
}
