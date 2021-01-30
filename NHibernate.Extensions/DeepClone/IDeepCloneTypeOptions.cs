using System;
using System.Linq.Expressions;
using System.Reflection;

namespace NHibernate.Extensions
{
    public interface IDeepCloneTypeOptions
    {
        IDeepCloneTypeOptions CloneIdentifier(bool value);

        IDeepCloneTypeOptions ForMember(string memberName, Action<IDeepCloneMemberOptions> action);

        IDeepCloneTypeOptions ForMembers(Func<PropertyInfo, bool> func, Action<IDeepCloneMemberOptions> action);
    }

    public interface IDeepCloneTypeOptions<TType>
    {
        IDeepCloneTypeOptions<TType> CloneIdentifier(bool value);

        IDeepCloneTypeOptions<TType> ForMember<TMember>(Expression<Func<TType, TMember>> memberExpr,
            Action<IDeepCloneMemberOptions<TType, TMember>> action);

        IDeepCloneTypeOptions<TType> ForMembers(Func<PropertyInfo, bool> func, Action<IDeepCloneMemberOptions<TType, object>> action);
    }
}
