using System;

namespace NHibernate.Extensions
{
    public interface IDeepCloneMemberOptions
    {
        string MemberName { get; set; }

        Func<object, object> ResolveUsing { get; set; }

        bool IsResolveUsingSet { get; }

        bool Ignore { get; set; }

        bool CloneAsReference { get; set; }
    }

    public interface IDeepCloneMemberOptions<out TType>
    {
        IDeepCloneMemberOptions<TType> ResolveUsing(Func<TType, object> func);

        IDeepCloneMemberOptions<TType> Ignore(bool value = true);

        IDeepCloneMemberOptions<TType> CloneAsReference(bool value = true);
    }
}
