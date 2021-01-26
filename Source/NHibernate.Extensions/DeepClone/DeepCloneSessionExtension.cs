using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
            var collection = (IEnumerable) CreateNewCollection(entities.GetType(), out var addMethod);
            var resolvedEntities = new Dictionary<object, object>();
            foreach (var entity in entities)
            {
                var item = DeepClone(session.GetSessionImplementation(), entity, opts, null, resolvedEntities, null);
                addMethod.Invoke(collection, new[] {item});
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
            var collection = (IEnumerable) CreateNewCollection(entities.GetType(), out var addMethod);
            var resolvedEntities = new Dictionary<object, object>();
            foreach (var entity in entities)
            {
                var item = DeepClone(session.GetSessionImplementation(), entity, opts, null, resolvedEntities, null);
                addMethod.Invoke(collection, new[] {item});
            }
            return collection;
        }

        #endregion

        private static object DeepClone(this ISessionImplementor session, object entity, DeepCloneOptions opts, System.Type entityType,
            IDictionary<object, object> resolvedEntities, DeepCloneParentEntity parentEntity = null)
        {
            opts = opts ?? new DeepCloneOptions();
            if (entity == null || !NHibernateUtil.IsInitialized(entity))
                return entityType.GetDefaultValue();
            entityType = entityType ?? entity.GetUnproxiedType(true);

            if (entityType.IsSimpleType())
                return entity;

            AbstractEntityPersister entityMetadata;
            try
            {
                entityMetadata = (AbstractEntityPersister)session.Factory.GetClassMetadata(entityType);
            }
            catch (Exception)
            {
                return entityType.GetDefaultValue();
            }

            if (resolvedEntities.ContainsKey(entity) && parentEntity != null)
                return CopyOnlyForeignKeyProperties(resolvedEntities[entity], entityType, entityMetadata, opts, parentEntity);

            if (resolvedEntities.ContainsKey(entity))
                return resolvedEntities[entity];

            if (opts.CanCloneAsReferenceFunc != null && opts.CanCloneAsReferenceFunc(entityType))
                return entity;

            var propertyInfos = entityType.GetProperties();
            var copiedEntity = ReflectHelper.GetDefaultConstructor(entityType).Invoke(new object[0]);
            resolvedEntities.Add(entity, copiedEntity);

            foreach (var propertyInfo in propertyInfos
                .Where(p => opts.CanCloneIdentifier(entityType) || entityMetadata.IdentifierPropertyName != p.Name)
                .Where(p => !opts.GetIgnoreMembers(entityType).Contains(p.Name))
                .Where(p => p.GetSetMethod(true) != null))
            {
                IType propertyType;
                try
                {
                    propertyType = entityMetadata.GetPropertyType(propertyInfo.Name);
                }
                catch (Exception)
                {
                    continue;
                }

                var resolveFn = opts.GetResolveFunction(entityType, propertyInfo.Name);
                if (resolveFn != null)
                {
                    propertyInfo.SetValue(copiedEntity, resolveFn(entity), null);
                    continue;
                }

                if (propertyType.IsEntityType && opts.SkipEntityTypesValue.HasValue && opts.SkipEntityTypesValue.Value)
                    continue;

                //TODO: verify: false only when entity is a proxy or lazy field/property that is not yet initialized
                if (!NHibernateUtil.IsPropertyInitialized(entity, propertyInfo.Name))
                    continue;

                var propertyValue = propertyInfo.GetValue(entity, null);
                if (!NHibernateUtil.IsInitialized(propertyValue))
                {
                    //Use session load for proxy, works only for references (collections are not supported) 
                    if (
                        propertyValue != null &&
                        propertyValue.IsProxy() &&
                        !(propertyValue is IPersistentCollection) &&
                        opts.UseSessionLoadFunction
                        )
                    {
                        var lazyInit = ((INHibernateProxy)propertyValue).HibernateLazyInitializer;
                        propertyInfo.SetValue(copiedEntity, LoadEntity(session, lazyInit.PersistentClass, lazyInit.Identifier), null);
                    }
                    continue;
                }

                var filterFn = opts.GetFilterFunction(entityType, propertyInfo.Name);
                if (filterFn != null)
                    propertyValue = filterFn(propertyValue);

                var colNames = entityMetadata.GetPropertyColumnNames(propertyInfo.Name);
                var propType = propertyInfo.PropertyType;
                var cloneAsReference = opts.CanCloneAsReference(entityType, propertyInfo.Name);
                if (propertyType.IsCollectionType)
                {
                    var newCollection = CreateNewCollection(propertyType, out var addMethod);
                    propertyInfo.SetValue(copiedEntity, newCollection, null);
                    var colParentEntity = new DeepCloneParentEntity(copiedEntity, entityMetadata, propertyType, ((CollectionType)propertyType).GetReferencedColumns(session.Factory));
                    CloneCollection(session, opts, colParentEntity, resolvedEntities, newCollection, propertyValue, addMethod, cloneAsReference);
                }
                else if (propertyType.IsEntityType)
                {
                    if (cloneAsReference)
                        propertyInfo.SetValue(copiedEntity, propertyValue, null);
                    //Check if we have a parent entity and that is bidirectional related to the current property (one-to-many)
                    else if (parentEntity != null && parentEntity.ReferencedColumns.SequenceEqual(colNames))
                        propertyInfo.SetValue(copiedEntity, parentEntity.Entity, null);
                    else
                        propertyInfo.SetValue(copiedEntity, session.DeepClone(propertyValue, opts, propType, resolvedEntities), null);
                }
                else if (propType.IsSimpleType())
                {
                    //Check if we have a parent entity and that is bidirectional related to the current property (one-to-many)
                    //we dont want to set FKs to the parent entity as the parent is cloned
                    if (parentEntity != null && parentEntity.ReferencedColumns.Contains(colNames.First()))
                        continue;
                    propertyInfo.SetValue(copiedEntity, propertyValue, null);
                }
            }
            return copiedEntity;
        }

        private static object LoadEntity(ISessionImplementor sessionImpl, System.Type type, object identifier)
        {
            if (sessionImpl is IStatelessSession statelessSession)
            {
                return statelessSession.Get(sessionImpl.Factory.TryGetGuessEntityName(type), identifier);
            }

            return sessionImpl is ISession session ? session.Load(type, identifier) : null;
        }

        private static void CloneCollection(
            ISessionImplementor session,
            DeepCloneOptions opts,
            DeepCloneParentEntity parentEntity,
            IDictionary<object, object> resolvedEntities,
            object newCollection,
            object collection,
            MethodInfo addMethod,
            bool cloneAsReference)
        {
            var enumerable = collection as IEnumerable;
            if (enumerable != null)
            {
                var enumerableType = enumerable.GetType();
                if (enumerableType.IsAssignableToGenericType(typeof(IDictionary<,>)))
                {
                    foreach (dynamic pair in enumerable)
                    {
                        var clone = cloneAsReference
                            ? (object)pair.Value
                            : session.DeepClone((object)pair.Value, opts, null, resolvedEntities, parentEntity);
                        addMethod.Invoke(newCollection, new[] {pair.Key, clone});
                    }
                }
                else
                {
                    foreach (var item in enumerable)
                    {
                        var clone = cloneAsReference
                            ? item
                            : session.DeepClone(item, opts, null, resolvedEntities, parentEntity);
                        addMethod.Invoke(newCollection, new[] {clone});
                    }
                }
            }
        }

        private static object CopyOnlyForeignKeyProperties(object entity, System.Type entityType,
            AbstractEntityPersister entityMetadata, DeepCloneOptions opts, DeepCloneParentEntity parentEntity)
        {
            var propertyInfos = entityType.GetProperties();

            //Copy only Fks
            foreach (var propertyInfo in propertyInfos
                .Where(p => opts.CanCloneIdentifier(entityType) || entityMetadata.IdentifierPropertyName != p.Name)
                .Where(p => !opts.GetIgnoreMembers(entityType).Contains(p.Name))
                .Where(p => p.GetSetMethod(true) != null))
            {
                IType propertyType;
                try
                {
                    propertyType = entityMetadata.GetPropertyType(propertyInfo.Name);
                }
                catch (Exception)
                {
                    continue;
                }
                if (!NHibernateUtil.IsPropertyInitialized(entity, propertyInfo.Name))
                    continue;
                var propertyValue = propertyInfo.GetValue(entity, null);
                if (!NHibernateUtil.IsInitialized(propertyValue))
                    continue;

                var colNames = entityMetadata.GetPropertyColumnNames(propertyInfo.Name);
                if (!propertyType.IsEntityType) continue;
                //Check if we have a parent entity and that is bidirectional related to the current property (one-to-many)
                if (parentEntity.ReferencedColumns.SequenceEqual(colNames))
                {
                    propertyInfo.SetValue(entity, parentEntity.Entity, null);
                }
            }
            return entity;
        }

        //can be an interface
        private static object CreateNewCollection(System.Type collectionType, out MethodInfo addMethod)
        {
            var concreteCollType = GetCollectionImplementation(collectionType);
            if (collectionType.IsGenericType)
            {
                concreteCollType = concreteCollType.MakeGenericType(collectionType.GetGenericArguments()[0]);
            }

            addMethod = concreteCollType.GetInterfaces().SelectMany(o => o.GetMethods()).First(o => o.Name == "Add");
            return Activator.CreateInstance(concreteCollType);
        }

        private static object CreateNewCollection(IType collectionProperty, out MethodInfo addMethod)
        {
            var concreteCollType = GetCollectionImplementation(collectionProperty);
            if (collectionProperty.ReturnedClass.IsGenericType)
            {
                concreteCollType = concreteCollType.MakeGenericType(collectionProperty.ReturnedClass.GetGenericArguments());
            }

            addMethod = concreteCollType.GetInterfaces().SelectMany(o => o.GetMethods()).First(o => o.Name == "Add");
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
