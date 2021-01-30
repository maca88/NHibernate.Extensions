using System;
using System.Collections.Generic;
using System.Linq;
using NHibernate.Extensions.DeepClone;
using NHibernate.Extensions.Internal;
using NHibernate.Persister.Entity;

namespace NHibernate.Extensions
{
    public class DeepCloneOptions
    {
        internal bool CloneIdentifierValue { get; set; } = true;

        internal bool UseSessionLoadFunction { get; set; }

        internal bool? SkipEntityTypesValue { get; set; }

        internal Func<System.Type, bool> CanCloneAsReferenceFunc { get; set; }

        internal List<IEntityResolver> EntityResolvers { get; } = new List<IEntityResolver>();

        internal Dictionary<System.Type, DeepCloneTypeOptions> TypeOptions { get; } = new Dictionary<System.Type, DeepCloneTypeOptions>();

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

        public DeepCloneOptions AddEntityResolver(IEntityResolver resolver)
        {
            EntityResolvers.Add(resolver);
            return this;
        }

        public DeepCloneOptions AddEntityResolver(Predicate<System.Type> entityTypePredicate, Func<object, AbstractEntityPersister, object> resolver)
        {
            EntityResolvers.Add(new DelegateEntityResolver(entityTypePredicate, resolver));
            return this;
        }

        public DeepCloneOptions AddEntityResolver(Func<System.Type, object, bool> canResolveFunc, Func<object, AbstractEntityPersister, object> resolver)
        {
            EntityResolvers.Add(new DelegateEntityResolver(canResolveFunc, resolver));
            return this;
        }

        public DeepCloneOptions ForType<TType>(Action<IDeepCloneTypeOptions<TType>> action)
        {
            var type = typeof(TType);
            if (!TypeOptions.ContainsKey(typeof(TType)))
            {
                var typeOpts = new DeepCloneTypeOptions<TType>
                {
                    CloneIdentifier = CloneIdentifierValue
                };
                TypeOptions.Add(type, typeOpts);
            }
            action(TypeOptions[type] as IDeepCloneTypeOptions<TType>);
            return this;
        }

        public DeepCloneOptions ForTypes(IEnumerable<System.Type> types, Action<System.Type, IDeepCloneTypeOptions> action)
        {
            foreach (var type in types)
            {
                if (!TypeOptions.ContainsKey(type))
                {
                    var typeOpts = new DeepCloneTypeOptions(type)
                    {
                        CloneIdentifier = CloneIdentifierValue
                    };
                    TypeOptions.Add(type, typeOpts);
                }
                action(type, TypeOptions[type]);
            }
            return this;
        }

        private readonly Dictionary<System.Type, HashSet<string>> _cachedIgnoredMembersResults = new Dictionary<System.Type, HashSet<string>>();

        internal HashSet<string> GetIgnoreMembers(System.Type type)
        {
            if (_cachedIgnoredMembersResults.ContainsKey(type))
            {
                return _cachedIgnoredMembersResults[type];
            }
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
                    .Where(o => o.MemberName == propName && o.ResolveUsing != null))
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

        private readonly Dictionary<System.Type, Dictionary<string, Func<object, object>>> _cachedFilterFunctions =
            new Dictionary<System.Type, Dictionary<string, Func<object, object>>>();

        internal Func<object, object> GetFilterFunction(System.Type type, string propName)
        {
            if (_cachedFilterFunctions.ContainsKey(type) && _cachedFilterFunctions[type].ContainsKey(propName))
                return _cachedFilterFunctions[type][propName];

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
                    .Where(o => o.MemberName == propName && o.Filter != null))
                {
                    result = memberOpt.Filter;
                }
            }

            if (!_cachedFilterFunctions.ContainsKey(type))
                _cachedFilterFunctions.Add(type, new Dictionary<string, Func<object, object>>());
            if (!_cachedFilterFunctions[type].ContainsKey(propName))
                _cachedFilterFunctions[type].Add(propName, null);

            _cachedFilterFunctions[type][propName] = result;
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
