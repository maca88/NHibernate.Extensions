using System;
using System.Collections.Generic;
using System.Linq;

namespace NHibernate.Extensions
{
    public class DeepCloneOptions
    {
        public DeepCloneOptions()
        {
            CloneIdentifierValue = true;
            TypeOptions = new Dictionary<System.Type, DeepCloneTypeOptions>();
        }

        internal bool CloneIdentifierValue { get; set; }

        internal bool UseSessionLoadFunction { get; set; }

        internal bool? SkipEntityTypesValue { get; set; }

        internal Func<System.Type, bool> CanCloneAsReferenceFunc { get; set; }

        internal Dictionary<System.Type, DeepCloneTypeOptions> TypeOptions { get; set; }

        public DeepCloneOptions CloneIdentifier(bool value)
        {
            CloneIdentifierValue = value;
            return this;
        }

        public DeepCloneOptions SkipEntityTypes(bool value = true)
        {
            SkipEntityTypesValue = value;
            return this;
        }

        public DeepCloneOptions UseSessionLoad(bool value = true)
        {
            UseSessionLoadFunction = value;
            return this;
        }

        public DeepCloneOptions CanCloneAsReference(Func<System.Type, bool> func)
        {
            CanCloneAsReferenceFunc = func;
            return this;
        }

        public DeepCloneOptions ForType<TType>(Action<IDeepCloneTypeOptions<TType>> action)
        {
            var type = typeof(TType);
            if (!TypeOptions.ContainsKey(typeof(TType)))
            {
                var typeOpts = new DeepCloneTypeOptions<TType>();
                typeOpts.CloneIdentifier(CloneIdentifierValue);
                TypeOptions.Add(type, typeOpts);
            }
            action(TypeOptions[type] as IDeepCloneTypeOptions<TType>);
            return this;
        }

        private readonly Dictionary<System.Type, HashSet<string>> _cachedIgnoredMembersResults = new Dictionary<System.Type, HashSet<string>>();

        internal HashSet<string> GetIgnoreMembers(System.Type type)
        {
            if (_cachedIgnoredMembersResults.ContainsKey(type)) return _cachedIgnoredMembersResults[type];
            var result = new HashSet<string>();
            var pairs = TypeOptions.Where(pair => pair.Key.IsAssignableFrom(type)).ToList();
            if (pairs.Any())
            {
                //subclasses have higher priority
                pairs.Sort((pair, valuePair) => pair.Key.IsAssignableFrom(valuePair.Key) ? -1 : 1);
                foreach (var member in pairs
                    .Select(o => o.Value)
                    .SelectMany(o => o.Members)
                    .Select(o => o.Value))
                {
                    if (member.Ignore)
                        result.Add(member.MemberName);
                    else
                        result.Remove(member.MemberName);
                }
            }
            _cachedIgnoredMembersResults.Add(type, result);
            return result;
        }

        private readonly Dictionary<System.Type, Dictionary<string, Func<object, object>>> _cachedResolveFunctions =
            new Dictionary<System.Type, Dictionary<string, Func<object, object>>>();

        internal Func<object, object> GetResolveFunction(System.Type type, string propName)
        {
            if (_cachedResolveFunctions.ContainsKey(type) && _cachedResolveFunctions[type].ContainsKey(propName))
                return _cachedResolveFunctions[type][propName];

            var pairs = TypeOptions.Where(pair => pair.Key.IsAssignableFrom(type)).ToList();
            Func<object, object> result = null;
            if (pairs.Any())
            {
                //subclasses have higher priority
                pairs.Sort((pair, valuePair) => pair.Key.IsAssignableFrom(valuePair.Key) ? -1 : 1);
                foreach (var memberOpt in pairs
                    .Select(o => o.Value)
                    .SelectMany(o => o.Members)
                    .Select(o => o.Value)
                    .Where(o => o.MemberName == propName && o.IsResolveUsingSet))
                {
                    result = memberOpt.ResolveUsing;
                }
            }

            if (!_cachedResolveFunctions.ContainsKey(type))
                _cachedResolveFunctions.Add(type, new Dictionary<string, Func<object, object>>());
            if (!_cachedResolveFunctions[type].ContainsKey(propName))
                _cachedResolveFunctions[type].Add(propName, null);

            _cachedResolveFunctions[type][propName] = result;
            return result;
        }

        internal bool CanCloneIdentifier(System.Type entityType)
        {
            return TypeOptions.ContainsKey(entityType) && TypeOptions[entityType].CloneIdentifier.HasValue
                ? TypeOptions[entityType].CloneIdentifier.Value
                : CloneIdentifierValue;
        }

        internal bool CanCloneAsReference(System.Type entityType, string propertyName)
        {
            if (!TypeOptions.ContainsKey(entityType) || !TypeOptions[entityType].Members.ContainsKey(propertyName)) return false;
            return TypeOptions[entityType].Members[propertyName].CloneAsReference;
        }
    }
}
