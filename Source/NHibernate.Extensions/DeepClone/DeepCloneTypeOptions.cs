using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using NHibernate.Extensions.Internal;

namespace NHibernate.Extensions
{
    public class DeepCloneTypeOptions : IDeepCloneTypeOptions
    {
        public DeepCloneTypeOptions(System.Type type)
        {
            Members = new Dictionary<string, DeepCloneMemberOptions>();
            CloneIdentifier = true;
            Type = type;
        }

        public System.Type Type { get; set; }

        public Dictionary<string, DeepCloneMemberOptions> Members { get; set; }

        public bool? CloneIdentifier { get; set; }
    }

    public class DeepCloneTypeOptions<TType> : DeepCloneTypeOptions, IDeepCloneTypeOptions<TType>
    {
        public DeepCloneTypeOptions()
            : base(typeof(TType))
        {
        }

        public IDeepCloneTypeOptions<TType> CloneIdentifier(bool value)
        {
            base.CloneIdentifier = value;
            return this;
        }

        public IDeepCloneTypeOptions<TType> ForMember<TMember>(Expression<Func<TType, TMember>> memberExpr,
            Action<IDeepCloneMemberOptions<TType, TMember>> action)
        {
            var memberName = memberExpr.GetFullPropertyName();
            if (!Members.ContainsKey(memberName))
                Members.Add(memberName, new DeepCloneMemberOptions<TType, TMember>
                {
                    MemberName = memberName
                });
            action(Members[memberName] as IDeepCloneMemberOptions<TType, TMember>);
            return this;
        }
    }
}
