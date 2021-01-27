using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NHibernate.Collection;
using NHibernate.Engine;
using NHibernate.Extensions.Internal;
using NHibernate.Persister.Entity;
using NHibernate.Proxy;
using NHibernate.Type;
using NHibernate.Util;

namespace NHibernate.Extensions
{
    public static partial class DeepCloneSessionExtension
    {
        static DeepCloneSessionExtension()
        {
        }

        #region ISession DeepClone

        public static IList<T> DeepClone<T>(this ISession session, IEnumerable<T> entities, Func<DeepCloneOptions, DeepCloneOptions> optsFn = null)
        {
            var opts = new DeepCloneOptions();
            if (optsFn != null)
                opts = optsFn(opts);
            return DeepClone(session, entities, opts);
        }

        public static IList<T> DeepClone<T>(this ISession session, IEnumerable<T> entities, DeepCloneOptions opts)
        {
            var resolvedEntities = new Dictionary<object, object>();
            var result = new List<T>();
            foreach (var entity in entities)
            {
                var clone = (T) DeepClone(session.GetSessionImplementation(), entity, opts, null, resolvedEntities);
                result.Add(clone);
            }

            return result;
        }

        public static T DeepClone<T>(this ISession session, T entity, Func<DeepCloneOptions, DeepCloneOptions> optsAction = null)
        {
            var opts = new DeepCloneOptions();
            if (optsAction != null)
                opts = optsAction(opts);
            return DeepClone(session, entity, opts);
        }

        public static T DeepClone<T>(this ISession session, T entity, DeepCloneOptions opts)
        {
            return (T)DeepClone(session.GetSessionImplementation(), entity, opts, null, new Dictionary<object, object>());
        }

        public static object DeepClone(this ISession session, object entity, System.Type entityType = null, Func<DeepCloneOptions, DeepCloneOptions> optsFn = null)
        {
            var opts = new DeepCloneOptions();
            if (optsFn != null)
                opts = optsFn(opts);
            return DeepClone(session, entity, entityType, opts);
        }

        public static object DeepClone(this ISession session, object entity, System.Type entityType, DeepCloneOptions opts)
        {
            return DeepClone(session.GetSessionImplementation(), entity, opts, entityType, new Dictionary<object, object>());
        }

        public static IEnumerable DeepClone(this ISession session, IEnumerable entities, Func<DeepCloneOptions, DeepCloneOptions> optsFn = null)
        {
            var opts = new DeepCloneOptions();
            if (optsFn != null)
                opts = optsFn(opts);
            return DeepClone(session, entities, opts);
        }

        public static IEnumerable DeepClone(this ISession session, IEnumerable entities, DeepCloneOptions opts)
        {
            var collection = (dynamic) CreateNewCollection(entities.GetType());
            var resolvedEntities = new Dictionary<object, object>();
            foreach (var entity in entities)
            {
                var item = DeepClone(session.GetSessionImplementation(), entity, opts, null, resolvedEntities, null);
                collection.Add((dynamic) item);
            }

            return collection;
        }

        #endregion

        #region IStatelessSession DeepClone

        public static IList<T> DeepClone<T>(this IStatelessSession session, IEnumerable<T> entities, Func<DeepCloneOptions, DeepCloneOptions> optsFn = null)
        {
            var opts = new DeepCloneOptions();
            if (optsFn != null)
                opts = optsFn(opts);
            return DeepClone(session, entities, opts);
        }

        public static IList<T> DeepClone<T>(this IStatelessSession session, IEnumerable<T> entities, DeepCloneOptions opts)
        {
            var resolvedEntities = new Dictionary<object, object>();
            var result = new List<T>();
            foreach (var entity in entities)
            {
                var clone = (T) DeepClone(session.GetSessionImplementation(), entity, opts, null, resolvedEntities);
                result.Add(clone);
            }

            return result;
        }

        public static T DeepClone<T>(this IStatelessSession session, T entity, Func<DeepCloneOptions, DeepCloneOptions> optsFn = null)
        {
            var opts = new DeepCloneOptions();
            if (optsFn != null)
                opts = optsFn(opts);
            return DeepClone(session, entity, opts);
        }

        public static T DeepClone<T>(this IStatelessSession session, T entity, DeepCloneOptions opts)
        {
            return (T)DeepClone(session.GetSessionImplementation(), entity, opts, null, new Dictionary<object, object>(), null);
        }

        public static object DeepClone(this IStatelessSession session, object entity, System.Type entityType = null, Func<DeepCloneOptions, DeepCloneOptions> optsFn = null)
        {
            var opts = new DeepCloneOptions();
            if (optsFn != null)
                opts = optsFn(opts);
            return DeepClone(session, entity, entityType, opts);
        }

        public static object DeepClone(this IStatelessSession session, object entity, System.Type entityType, DeepCloneOptions opts)
        {
            return DeepClone(session.GetSessionImplementation(), entity, opts, entityType, new Dictionary<object, object>());
        }

        public static IEnumerable DeepClone(this IStatelessSession session, IEnumerable entities, Func<DeepCloneOptions, DeepCloneOptions> optsFn = null)
        {
            var opts = new DeepCloneOptions();
            if (optsFn != null)
                opts = optsFn(opts);
            return DeepClone(session, entities, opts);
        }

        public static IEnumerable DeepClone(this IStatelessSession session, IEnumerable entities, DeepCloneOptions opts)
        {
            var collection = (dynamic) CreateNewCollection(entities.GetType());
            var resolvedEntities = new Dictionary<object, object>();
            foreach (var entity in entities)
            {
                var item = DeepClone(session.GetSessionImplementation(), entity, opts, null, resolvedEntities, null);
                collection.Add((dynamic) item);
            }

            return collection;
        }

        #endregion

        private static object DeepClone(this ISessionImplementor session, object entity, DeepCloneOptions opts, System.Type entityType,
            IDictionary<object, object> resolvedEntities, DeepCloneParentEntity parentEntity = null)
        {
            opts = opts ?? new DeepCloneOptions();
            if (entity == null || !NHibernateUtil.IsInitialized(entity))
            {
                return entityType?.GetDefaultValue();
            }

            entityType = entityType ?? entity.GetUnproxiedType(true);
            if (entityType.IsSimpleType())
            {
                return entity;
            }

            if (!(session.Factory.GetClassMetadata(entityType) is AbstractEntityPersister entityPersister))
            {
                return entityType.GetDefaultValue();
            }

            if (resolvedEntities.ContainsKey(entity))
            {
                return parentEntity != null
                    ? CopyOnlyForeignKeyProperties(resolvedEntities[entity], entityType, entityPersister, opts, parentEntity)
                    : resolvedEntities[entity];
            }

            if (opts.CanCloneAsReferenceFunc != null && opts.CanCloneAsReferenceFunc(entityType))
            {
                return entity;
            }

            var id = opts.CanCloneIdentifier(entityType)
                ? CloneIdentifier(session, entityPersister, entity, opts, resolvedEntities)
                : GetDefaultIdentifier(entityPersister);
            var clone = entityPersister.Instantiate(id);
            resolvedEntities.Add(entity, clone);

            var propertyNames = entityPersister.PropertyNames;
            DeepCloneProperties(
                session,
                entity,
                clone,
                opts,
                entityType,
                resolvedEntities,
                parentEntity,
                propertyNames,
                entityPersister.PropertyTypes,
                entityPersister.PropertyLaziness,
                entityPersister.GetPropertyColumnNames,
                entityPersister.GetPropertyValue,
                entityPersister.SetPropertyValue
            );

            return clone;
        }

        private static void DeepCloneProperties(
            ISessionImplementor session,
            object entity,
            object clone,
            DeepCloneOptions opts,
            System.Type entityType,
            IDictionary<object, object> resolvedEntities,
            DeepCloneParentEntity parentEntity,
            string[] propertyNames,
            IType[] propertyTypes,
            bool[] propertyLaziness,
            Func<int, string[]> getPropertyColumnNames,
            Func<object, int, object> getPropertyValueFunc,
            Action<object, int, object> setPropertyValueFunc
            )
        {
            for (var i = 0; i < propertyNames.Length; i++)
            {
                var propertyName = propertyNames[i];
                if (opts.GetIgnoreMembers(entityType).Contains(propertyName))
                {
                    continue;
                }

                var propertyType = propertyTypes[i];
                var resolveFn = opts.GetResolveFunction(entityType, propertyName);
                if (resolveFn != null)
                {
                    setPropertyValueFunc(clone, i, resolveFn(entity));
                    continue;
                }

                if (propertyType.IsEntityType && opts.SkipEntityTypesValue == true)
                {
                    continue;
                }

                var isLazy = propertyLaziness != null && propertyLaziness[i];
                if (isLazy && !NHibernateUtil.IsPropertyInitialized(entity, propertyName))
                {
                    continue;
                }

                var propertyValue = getPropertyValueFunc(entity, i);
                if (!NHibernateUtil.IsInitialized(propertyValue))
                {
                    // Use session load for proxy, works only for references (collections are not supported) 
                    if (
                        propertyValue != null &&
                        propertyValue.IsProxy() &&
                        !(propertyValue is IPersistentCollection) &&
                        opts.UseSessionLoadFunction
                    )
                    {
                        var lazyInit = ((INHibernateProxy) propertyValue).HibernateLazyInitializer;
                        setPropertyValueFunc(clone, i,
                            LoadEntity(session, lazyInit.PersistentClass, lazyInit.Identifier));
                    }

                    continue;
                }

                var filterFn = opts.GetFilterFunction(entityType, propertyName);
                if (filterFn != null)
                {
                    propertyValue = filterFn(propertyValue);
                }

                var colNames = getPropertyColumnNames(i);
                var cloneAsReference = opts.CanCloneAsReference(entityType, propertyName);
                if (propertyType is ComponentType componentType)
                {
                    setPropertyValueFunc(clone, i, CloneComponent(session, componentType, propertyValue, clone, getPropertyColumnNames(i), opts, resolvedEntities));
                }
                else if (propertyType.IsCollectionType)
                {
                    var newCollection = CreateNewCollection(propertyType);
                    setPropertyValueFunc(clone, i, newCollection);
                    var colParentEntity = new DeepCloneParentEntity(clone, propertyType, ((CollectionType) propertyType).GetReferencedColumns(session.Factory));
                    CloneCollection(session, opts, colParentEntity, resolvedEntities, newCollection, propertyValue, cloneAsReference);
                }
                else if (propertyType.IsEntityType)
                {
                    object value;
                    if (cloneAsReference)
                    {
                        value = propertyValue;
                    }
                    // Check if we have a parent entity and that is bidirectional related to the current property (one-to-many)
                    else if (parentEntity != null && parentEntity.ReferencedColumns.SequenceEqual(colNames))
                    {
                        value = parentEntity.Entity;
                    }
                    else
                    {
                        value = session.DeepClone(propertyValue, opts, propertyType.ReturnedClass, resolvedEntities);
                    }

                    setPropertyValueFunc(clone, i, value);
                }
                else
                {
                    // Check if we have a parent entity and that is bidirectional related to the current property (one-to-many)
                    // we dont want to set FKs to the parent entity as the parent is cloned
                    if (parentEntity != null && parentEntity.ReferencedColumns.Contains(colNames.First()))
                    {
                        continue;
                    }

                    setPropertyValueFunc(clone, i, propertyType.DeepCopy(propertyValue, session.Factory));
                }
            }
        }

        private static object CloneIdentifier(ISessionImplementor session, AbstractEntityPersister entityPersister, object entity, DeepCloneOptions opts, IDictionary<object, object> resolvedEntities)
        {
            var id = entityPersister.GetIdentifier(entity);
            if (entityPersister.IdentifierType is ComponentType componentType)
            {
                return CloneComponent(session, componentType, id, null, entityPersister.IdentifierColumnNames, opts, resolvedEntities);
            }
            else
            {
                return entityPersister.IdentifierType.DeepCopy(id, session.Factory);
            }
        }

        private static object CloneComponent(
            ISessionImplementor session,
            ComponentType componentType,
            object value,
            object parent,
            string[] columnNames,
            DeepCloneOptions opts,
            IDictionary<object, object> resolvedEntities)
        {
            var names = componentType.PropertyNames;
            var clone = componentType.Instantiate(parent, session);

            DeepCloneProperties(
                session,
                value,
                clone,
                opts,
                componentType.ReturnedClass,
                resolvedEntities,
                null,
                componentType.PropertyNames,
                componentType.Subtypes,
                null,
                i => GetComponentPropertyColumnNames(session.Factory, componentType, i, columnNames),
                componentType.GetPropertyValue,
                (target, i, targetValue) =>
                {
                    var values = componentType.GetPropertyValues(target);
                    values[i] = targetValue;
                    componentType.SetPropertyValues(clone, values);
                }
            );

            return clone;
        }

        private static void CloneCollection(
            ISessionImplementor session,
            DeepCloneOptions opts,
            DeepCloneParentEntity parentEntity,
            IDictionary<object, object> resolvedEntities,
            dynamic newCollection,
            object collection,
            bool cloneAsReference)
        {
            if (!(collection is IEnumerable enumerable))
            {
                return;
            }

            var enumerableType = enumerable.GetType();
            if (enumerableType.IsAssignableToGenericType(typeof(IDictionary<,>)))
            {
                foreach (dynamic pair in enumerable)
                {
                    var clone = cloneAsReference
                        ? (object)pair.Value
                        : session.DeepClone((object)pair.Value, opts, null, resolvedEntities, parentEntity);
                    newCollection.Add(pair.Key, (dynamic) clone);
                }
            }
            else
            {
                foreach (var item in enumerable)
                {
                    var clone = cloneAsReference
                        ? item
                        : session.DeepClone(item, opts, null, resolvedEntities, parentEntity);
                    newCollection.Add((dynamic) clone);
                }
            }
        }

        private static object LoadEntity(ISessionImplementor sessionImpl, System.Type type, object identifier)
        {
            if (sessionImpl is IStatelessSession statelessSession)
            {
                return statelessSession.Get(sessionImpl.Factory.TryGetGuessEntityName(type), identifier);
            }

            return sessionImpl is ISession session ? session.Load(type, identifier) : null;
        }

        private static object GetDefaultIdentifier(AbstractEntityPersister entityPersister)
        {
            if (entityPersister.IdentifierType is ComponentType componentType)
            {
                return componentType.Instantiate();
            }
            else
            {
                return entityPersister.IdentifierType.ReturnedClass.GetDefaultValue();
            }
        }

        private static object CopyOnlyForeignKeyProperties(object entity, System.Type entityType,
            AbstractEntityPersister entityMetadata, DeepCloneOptions opts, DeepCloneParentEntity parentEntity)
        {
            var names = entityMetadata.PropertyNames;
            for (var i = 0; i < names.Length; i++)
            {
                var name = names[i];
                if (opts.GetIgnoreMembers(entityType).Contains(name))
                {
                    continue;
                }

                var isLazy = entityMetadata.PropertyLaziness[i];
                if (isLazy && !NHibernateUtil.IsPropertyInitialized(entity, name)) // Avoid lazy load
                {
                    continue;
                }

                var propertyType = entityMetadata.PropertyTypes[i];
                if (!propertyType.IsEntityType)
                {
                    continue;
                }

                var colNames = entityMetadata.GetPropertyColumnNames(i);
                // Check if we have a parent entity and that is bidirectional related to the current property (one-to-many)
                if (parentEntity.ReferencedColumns.SequenceEqual(colNames))
                {
                    entityMetadata.SetPropertyValue(entity, i , parentEntity.Entity);
                }
            }

            return entity;
        }

        private static string[] GetComponentPropertyColumnNames(
            ISessionFactoryImplementor sessionFactory,
            IAbstractComponentType componentType,
            int i,
            string[] allColumnNames)
        {
            var span = componentType.Subtypes[i].GetColumnSpan(sessionFactory);
            var columnNames = new string[span];
            var offset = componentType.Subtypes.Take(i).Select(o => o.GetColumnSpan(sessionFactory)).Sum();
            Array.Copy(allColumnNames, offset, columnNames, 0, span);

            return columnNames;
        }

        // Can be an interface
        private static object CreateNewCollection(System.Type collectionType)
        {
            var concreteCollType = GetCollectionImplementation(collectionType);
            if (collectionType.IsGenericType)
            {
                concreteCollType = concreteCollType.MakeGenericType(collectionType.GetGenericArguments()[0]);
            }

            return Activator.CreateInstance(concreteCollType);
        }

        private static object CreateNewCollection(IType collectionProperty)
        {
            var concreteCollType = GetCollectionImplementation(collectionProperty);
            if (collectionProperty.ReturnedClass.IsGenericType)
            {
                concreteCollType = concreteCollType.MakeGenericType(collectionProperty.ReturnedClass.GetGenericArguments());
            }

            return Activator.CreateInstance(concreteCollType);
        }

        private static System.Type GetCollectionImplementation(System.Type collectionType)
        {
            if (collectionType.IsAssignableToGenericType(typeof(ISet<>)))
                return typeof(HashSet<>);
            if (collectionType.IsAssignableToGenericType(typeof(IList<>)))
                return typeof(List<>);
            if (collectionType.IsAssignableToGenericType(typeof(ICollection<>)))
                return typeof(List<>);
            if (collectionType.IsAssignableToGenericType(typeof(IEnumerable<>)))
                return typeof(List<>);
            throw new NotSupportedException(collectionType.FullName);
        }

        private static System.Type GetCollectionImplementation(IType collectionProperty)
        {
            var collectionType = collectionProperty.GetType();
            if (collectionType.IsAssignableToGenericType(typeof(GenericSetType<>)))
                return typeof(HashSet<>);
            if (collectionType.IsAssignableToGenericType(typeof(GenericListType<>)) ||
                collectionType.IsAssignableToGenericType(typeof(GenericIdentifierBagType<>)) ||
                collectionType.IsAssignableToGenericType(typeof(GenericBagType<>)))
                return typeof(List<>);
            if (collectionType.IsAssignableToGenericType(typeof(GenericMapType<,>)))
                return typeof(Dictionary<,>);
            throw new NotSupportedException(collectionType.FullName);
        }
    }
}
