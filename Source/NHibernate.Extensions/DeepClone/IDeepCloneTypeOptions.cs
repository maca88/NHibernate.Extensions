using System;
using System.Linq.Expressions;

namespace NHibernate.Extensions
{
    public interface IDeepCloneTypeOptions
    {
        bool? CloneIdentifier { get; set; }
    }

    public interface IDeepCloneTypeOptions<TType>
    {
        IDeepCloneTypeOptions<TType> CloneIdentifier(bool value);

        IDeepCloneTypeOptions<TType> ForMember<TMember>(Expression<Func<TType, TMember>> memberExpr,
            Action<IDeepCloneMemberOptions<TType>> action);
    }
}
