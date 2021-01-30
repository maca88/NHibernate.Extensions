using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
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

        IDeepCloneTypeOptions IDeepCloneTypeOptions.CloneIdentifier(bool value)
        {
            CloneIdentifier = value;
            return this;
        }

        IDeepCloneTypeOptions IDeepCloneTypeOptions.ForMember(string memberName, Action<IDeepCloneMemberOptions> action)
        {
            if (!Members.ContainsKey(memberName))
                Members.Add(memberName, new DeepCloneMemberOptions
                {
                    MemberName = memberName
                });
            action(Members[memberName]);
            return this;
        }

        IDeepCloneTypeOptions IDeepCloneTypeOptions.ForMembers(Func<PropertyInfo, bool> func, Action<IDeepCloneMemberOptions> action)
        {
            foreach (var propertyInfo in Type.GetProperties())
            {
                if (!func(propertyInfo))
                {
                    continue;
                }
                var memberName = propertyInfo.Name;
                if (!Members.ContainsKey(memberName))
                    Members.Add(memberName, new DeepCloneMemberOptions
                    {
                        MemberName = memberName
                    });
                action(Members[memberName]);
            }
            return this;
        }
    }

    public class DeepCloneTypeOptions<TType> : DeepCloneTypeOptions, IDeepCloneTypeOptions<TType>
    {
        public DeepCloneTypeOptions()
            : base(typeof(TType))
        {
        }

        IDeepCloneTypeOptions<TType> IDeepCloneTypeOptions<TType>.CloneIdentifier(bool value)
        {
            CloneIdentifier = value;
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

        public IDeepCloneTypeOptions<TType> ForMembers(Func<PropertyInfo, bool> func, Action<IDeepCloneMemberOptions<TType, object>> action)
        {
            foreach (var propertyInfo in Type.GetProperties())
            {
                if (!func(propertyInfo))
                {
                    continue;
                }
                var memberName = propertyInfo.Name;
                if (!Members.ContainsKey(memberName))
                    Members.Add(memberName, new DeepCloneMemberOptions<TType, object>
                    {
                        MemberName = memberName
                    });
                action(Members[memberName] as IDeepCloneMemberOptions<TType, object>);
            }
            return this;
        }
    }
}
