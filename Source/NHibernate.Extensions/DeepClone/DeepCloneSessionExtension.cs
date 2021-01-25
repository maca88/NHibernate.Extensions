using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using NHibernate.Collection;
using NHibernate.Engine;
using NHibernate.Extensions.Internal;
using NHibernate.Impl;
using NHibernate.Persister.Entity;
using NHibernate.Proxy;
using NHibernate.Type;
using NHibernate.Util;

namespace NHibernate.Extensions
{
    public static partial class DeepCloneSessionExtension
    {
        private static readonly PropertyInfo IsAlreadyDisposedPropInfo;
        private static readonly MethodInfo StatelessSessionGetAsyncMethod;
        private static readonly MethodInfo StatelessSessionGetMethod;


        static DeepCloneSessionExtension()
        {
            IsAlreadyDisposedPropInfo = typeof(SessionImpl).GetProperty("IsAlreadyDisposed", BindingFlags.Instance | BindingFlags.NonPublic);
            StatelessSessionGetMethod = typeof(IStatelessSession).GetMethods()
                .First(o => o.Name == "Get" && o.IsGenericMethod && o.GetParameters().Length == 1);
            StatelessSessionGetAsyncMethod = typeof(IStatelessSession).GetMethods()
                .First(o => o.Name == "GetAsync" && o.IsGenericMethod && o.GetParameters().Length == 2);
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
            return entities.Select(entity => (T)DeepClone(session.GetSessionImplementation(), entity, opts, null, resolvedEntities)).ToList();
        }

        public static T DeepClone<T>(this ISession session, T entity, Func<DeepCloneOptions, DeepCloneOptions> optsAction = null)
        {
            var opts = new DeepCloneOptions();
            if (optsAction != null)
                opts = optsAction(opts);
            return DeepClone(session, entity, opts);
        }

        public static Task<T> DeepCloneAsync<T>(this ISession session, T entity, Func<DeepCloneOptions, DeepCloneOptions> optsAction = null, CancellationToken cancellationToken = default)
        {
            var opts = new DeepCloneOptions();
            if (optsAction != null)
                opts = optsAction(opts);
            return DeepCloneAsync(session, entity, opts, cancellationToken);
        }

        public static T DeepClone<T>(this ISession session, T entity, DeepCloneOptions opts)
        {
            return (T)DeepClone(session.GetSessionImplementation(), entity, opts, null, new Dictionary<object, object>());
        }

        public static async Task<T> DeepCloneAsync<T>(this ISession session, T entity, DeepCloneOptions opts, CancellationToken cancellationToken = default)
        {
            T clone = (T)await DeepCloneAsync(session.GetSessionImplementation(), entity, opts, null, new Dictionary<object, object>(), parentEntity: null, cancellationToken);
            return clone;
        }

        public static object DeepClone(this ISession session, object entity, System.Type entityType = null, Func<DeepCloneOptions, DeepCloneOptions> optsFn = null)
        {
            var opts = new DeepCloneOptions();
            if (optsFn != null)
                opts = optsFn(opts);
            return DeepClone(session, entity, entityType, opts);
        }

        public static Task<object> DeepCloneAsync(this ISession session, object entity, System.Type entityType = null, Func<DeepCloneOptions, DeepCloneOptions> optsFn = null, CancellationToken cancellationToken = default)
        {
            var opts = new DeepCloneOptions();
            if (optsFn != null)
                opts = optsFn(opts);
            return DeepCloneAsync(session, entity, entityType, opts, cancellationToken);
        }

        public static object DeepClone(this ISession session, object entity, System.Type entityType, DeepCloneOptions opts)
        {
            return DeepClone(session.GetSessionImplementation(), entity, opts, entityType, new Dictionary<object, object>());
        }

        public static Task<object> DeepCloneAsync(this ISession session, object entity, System.Type entityType, DeepCloneOptions opts, CancellationToken cancellationToken = default)
        {
            return DeepCloneAsync(session.GetSessionImplementation(), entity, opts, entityType, new Dictionary<object, object>(), parentEntity: null, cancellationToken);
        }

        public static IEnumerable DeepClone(this ISession session, IEnumerable entities, Func<DeepCloneOptions, DeepCloneOptions> optsFn = null)
        {
            var opts = new DeepCloneOptions();
            if (optsFn != null)
                opts = optsFn(opts);
            return DeepClone(session, entities, opts);
        }

        public static Task<IEnumerable> DeepCloneAsync(this ISession session, IEnumerable entities, Func<DeepCloneOptions, DeepCloneOptions> optsFn = null, CancellationToken cancellationToken = default)
        {
            var opts = new DeepCloneOptions();
            if (optsFn != null)
                opts = optsFn(opts);
            return DeepCloneAsync(session, entities, opts, cancellationToken);
        }

        public static IEnumerable DeepClone(this ISession session, IEnumerable entities, DeepCloneOptions opts)
        {
            var collection = (IEnumerable)CreateNewCollection(entities.GetType());
            var resolvedEntities = new Dictionary<object, object>();
            foreach (var entity in entities)
            {
                AddItemToCollection(collection, DeepClone(session.GetSessionImplementation(), entity, opts, null, resolvedEntities));
            }
            return collection;
        }

        public static async Task<IEnumerable> DeepCloneAsync(this ISession session, IEnumerable entities, DeepCloneOptions opts, CancellationToken cancellationToken = default)
        {
            var collection = (IEnumerable)CreateNewCollection(entities.GetType());
            var resolvedEntities = new Dictionary<object, object>();
            foreach (var entity in entities)
            {
                var clone = await DeepCloneAsync(session.GetSessionImplementation(), entity, opts, null, resolvedEntities, parentEntity: null, cancellationToken).ConfigureAwait(false);
                await AddItemToCollectionAsync(collection, clone, editBeforeAdding: null, cancellationToken).ConfigureAwait(false);
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

        public static Task<IList<T>> DeepCloneAsync<T>(this IStatelessSession session, IEnumerable<T> entities, Func<DeepCloneOptions, DeepCloneOptions> optsFn = null, CancellationToken cancellationToken = default)
        {
            var opts = new DeepCloneOptions();
            if (optsFn != null)
                opts = optsFn(opts);
            return DeepCloneAsync(session, entities, opts, cancellationToken);
        }

        public static IList<T> DeepClone<T>(this IStatelessSession session, IEnumerable<T> entities, DeepCloneOptions opts)
        {
            var resolvedEntities = new Dictionary<object, object>();
            return entities.Select(entity => (T)DeepClone(session.GetSessionImplementation(), entity, opts, null, resolvedEntities)).ToList();
        }

        public static async Task<IList<T>> DeepCloneAsync<T>(this IStatelessSession session, IEnumerable<T> entities, DeepCloneOptions opts, CancellationToken cancellationToken = default)
        {
            var resolvedEntities = new Dictionary<object, object>();
            var output = new List<T>();
            T clone;
            foreach (var entity in entities)
            {
                clone = (T)(await DeepCloneAsync(session.GetSessionImplementation(), entity, opts, null, resolvedEntities, parentEntity: null, cancellationToken).ConfigureAwait(false));
                output.Add(clone);
            }
            return output;
        }

        public static T DeepClone<T>(this IStatelessSession session, T entity, Func<DeepCloneOptions, DeepCloneOptions> optsFn = null)
        {
            var opts = new DeepCloneOptions();
            if (optsFn != null)
                opts = optsFn(opts);
            return DeepClone(session, entity, opts);
        }

        public static Task<T> DeepCloneAsync<T>(this IStatelessSession session, T entity, Func<DeepCloneOptions, DeepCloneOptions> optsFn = null, CancellationToken cancellationToken = default)
        {
            var opts = new DeepCloneOptions();
            if (optsFn != null)
                opts = optsFn(opts);
            return DeepCloneAsync(session, entity, opts, cancellationToken);
        }

        public static T DeepClone<T>(this IStatelessSession session, T entity, DeepCloneOptions opts)
        {
            return (T)DeepClone(session.GetSessionImplementation(), entity, opts, null, new Dictionary<object, object>());
        }

        public static async Task<T> DeepCloneAsync<T>(this IStatelessSession session, T entity, DeepCloneOptions opts, CancellationToken cancellationToken = default)
        {
            return (T)(await DeepCloneAsync(session.GetSessionImplementation(), entity, opts, null, new Dictionary<object, object>(), parentEntity: null, cancellationToken).ConfigureAwait(false));
        }

        public static object DeepClone(this IStatelessSession session, object entity, System.Type entityType = null, Func<DeepCloneOptions, DeepCloneOptions> optsFn = null)
        {
            var opts = new DeepCloneOptions();
            if (optsFn != null)
                opts = optsFn(opts);
            return DeepClone(session, entity, entityType, opts);
        }

        public static Task<object> DeepCloneAsync(this IStatelessSession session, object entity, System.Type entityType = null, Func<DeepCloneOptions, DeepCloneOptions> optsFn = null, CancellationToken cancellationToken = default)
        {
            var opts = new DeepCloneOptions();
            if (optsFn != null)
                opts = optsFn(opts);
            return DeepCloneAsync(session, entity, entityType, opts, cancellationToken);
        }

        public static object DeepClone(this IStatelessSession session, object entity, System.Type entityType, DeepCloneOptions opts)
        {
            return DeepClone(session.GetSessionImplementation(), entity, opts, entityType, new Dictionary<object, object>());
        }

        public static Task<object> DeepCloneAsync(this IStatelessSession session, object entity, System.Type entityType, DeepCloneOptions opts, CancellationToken cancellationToken = default)
        {
            return DeepCloneAsync(session.GetSessionImplementation(), entity, opts, entityType, new Dictionary<object, object>(), parentEntity: null, cancellationToken);
        }

        public static IEnumerable DeepClone(this IStatelessSession session, IEnumerable entities, Func<DeepCloneOptions, DeepCloneOptions> optsFn = null)
        {
            var opts = new DeepCloneOptions();
            if (optsFn != null)
                opts = optsFn(opts);
            return DeepClone(session, entities, opts);
        }

        public static Task<IEnumerable> DeepCloneAsync(this IStatelessSession session, IEnumerable entities, Func<DeepCloneOptions, DeepCloneOptions> optsFn = null, CancellationToken cancellationToken = default)
        {
            var opts = new DeepCloneOptions();
            if (optsFn != null)
                opts = optsFn(opts);
            return DeepCloneAsync(session, entities, opts, cancellationToken);
        }

        public static IEnumerable DeepClone(this IStatelessSession session, IEnumerable entities, DeepCloneOptions opts)
        {
            var collection = (IEnumerable)CreateNewCollection(entities.GetType());
            var resolvedEntities = new Dictionary<object, object>();
            foreach (var entity in entities)
            {
                AddItemToCollection(collection, DeepClone(session.GetSessionImplementation(), entity, opts, null, resolvedEntities));
            }
            return collection;
        }

        public static async Task<IEnumerable> DeepCloneAsync(this IStatelessSession session, IEnumerable entities, DeepCloneOptions opts, CancellationToken cancellationToken = default)
        {
            var collection = (IEnumerable)CreateNewCollection(entities.GetType());
            var resolvedEntities = new Dictionary<object, object>();
            foreach (var entity in entities)
            {
                var clone = await DeepCloneAsync(session.GetSessionImplementation(), entity, opts, null, resolvedEntities, parentEntity: null, cancellationToken).ConfigureAwait(false);
                await AddItemToCollectionAsync(collection, clone, editBeforeAdding: null, cancellationToken).ConfigureAwait(false);
            }
            return collection;
        }

        #endregion

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

        private static object LoadEntity(ISessionImplementor sessionImpl, System.Type type, object identifier)
        {
            var statelessSession = sessionImpl as IStatelessSession;
            if (statelessSession != null)
                return StatelessSessionGetMethod.MakeGenericMethod(type).Invoke(statelessSession, new[] { identifier });
            var session = sessionImpl as ISession;
            return session != null ? session.Load(type, identifier) : null;
        }

        private static async Task<object> LoadEntityAsync(ISessionImplementor sessionImpl, System.Type type, object identifier, CancellationToken cancellationToken = default)
        {
            var statelessSession = sessionImpl as IStatelessSession;
            if (statelessSession != null)
                return (StatelessSessionGetAsyncMethod.MakeGenericMethod(type)).InvokeAsync(statelessSession, new[] { identifier, cancellationToken });
            var session = sessionImpl as ISession;
            return session != null ? await session.LoadAsync(type, identifier, cancellationToken).ConfigureAwait(false) : null;
        }

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
                var copyAsReference = opts.CanCloneAsReference(entityType, propertyInfo.Name);
                if (propertyType.IsCollectionType)
                {
                    var propertyList = CreateNewCollection(propertyType);
                    propertyInfo.SetValue(copiedEntity, propertyList, null);
                    AddItemToCollection(propertyList, propertyValue, o => copyAsReference
                        ? o
                        : session.DeepClone(o, opts, null, resolvedEntities,
                            new DeepCloneParentEntity
                            {
                                Entity = copiedEntity,
                                EntityPersister = entityMetadata,
                                ChildType = propertyType,
                                ReferencedColumns = ((CollectionType)propertyType)
                                    .GetReferencedColumns(session.Factory)
                            }));
                }
                else if (propertyType.IsEntityType)
                {
                    if (copyAsReference)
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

        private static async Task<object> DeepCloneAsync(this ISessionImplementor session, object entity, DeepCloneOptions opts, System.Type entityType,
            IDictionary<object, object> resolvedEntities, DeepCloneParentEntity parentEntity = null, CancellationToken cancellationToken = default)
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
                        var value = await LoadEntityAsync(session, lazyInit.PersistentClass, lazyInit.Identifier, cancellationToken).ConfigureAwait(false);
                        propertyInfo.SetValue(copiedEntity, value, null);
                    }
                    continue;
                }

                var filterFn = opts.GetFilterFunction(entityType, propertyInfo.Name);
                if (filterFn != null)
                    propertyValue = filterFn(propertyValue);

                var colNames = entityMetadata.GetPropertyColumnNames(propertyInfo.Name);
                var propType = propertyInfo.PropertyType;
                var copyAsReference = opts.CanCloneAsReference(entityType, propertyInfo.Name);
                if (propertyType.IsCollectionType)
                {
                    var propertyList = CreateNewCollection(propertyType);
                    propertyInfo.SetValue(copiedEntity, propertyList, null);
                    await AddItemToCollectionAsync(propertyList, propertyValue, async (o, ct) => copyAsReference
                        ? o
                        : await session.DeepCloneAsync(o, opts, null, resolvedEntities,
                            new DeepCloneParentEntity
                            {
                                Entity = copiedEntity,
                                EntityPersister = entityMetadata,
                                ChildType = propertyType,
                                ReferencedColumns = ((CollectionType)propertyType)
                                    .GetReferencedColumns(session.Factory)
                            }, cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
                }
                else if (propertyType.IsEntityType)
                {
                    if (copyAsReference)
                        propertyInfo.SetValue(copiedEntity, propertyValue, null);
                    //Check if we have a parent entity and that is bidirectional related to the current property (one-to-many)
                    else if (parentEntity != null && parentEntity.ReferencedColumns.SequenceEqual(colNames))
                        propertyInfo.SetValue(copiedEntity, parentEntity.Entity, null);
                    else
                        propertyInfo.SetValue(copiedEntity, await session.DeepCloneAsync(propertyValue, opts, propType, resolvedEntities, parentEntity: null, cancellationToken).ConfigureAwait(false), null);
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

        //can be an interface
        private static object CreateNewCollection(System.Type collectionType)
        {
            var concreteCollType = GetCollectionImplementation(collectionType);
            if (collectionType.IsGenericType)
            {
                concreteCollType = concreteCollType.MakeGenericType(collectionType.GetGenericArguments()[0]);
            }
            var propertyList = Activator.CreateInstance(concreteCollType);
            return propertyList;
        }

        private static object CreateNewCollection(IType collectionProperty)
        {
            var concreteCollType = GetCollectionImplementation(collectionProperty);
            if (collectionProperty.ReturnedClass.IsGenericType)
            {
                concreteCollType = concreteCollType.MakeGenericType(collectionProperty.ReturnedClass.GetGenericArguments());
            }
            var propertyList = Activator.CreateInstance(concreteCollType);
            return propertyList;
        }

        private static void AddItemToCollection(object newCollection, object collection, Func<object, object> editBeforeAdding = null)
        {
            var addMethod = newCollection.GetType().GetInterfaces()
                        .SelectMany(o => o.GetMethods())
                        .First(o => o.Name == "Add");

            var enumerable = collection as IEnumerable;
            if (enumerable != null)
            {
                var enumerableType = enumerable.GetType();
                if (enumerableType.IsAssignableToGenericType(typeof(IDictionary<,>)))
                {
                    foreach (dynamic pair in enumerable)
                    {
                        addMethod.Invoke(newCollection,
                            editBeforeAdding != null
                                ? new[] { pair.Key, editBeforeAdding(pair.Value) }
                                : new[] { pair.Key, pair.Value }
                        );
                    }
                }
                else
                {
                    foreach (var item in enumerable)
                    {
                        addMethod.Invoke(newCollection,
                                         editBeforeAdding != null
                                         ? new[] { editBeforeAdding(item) }
                                         : new[] { item });
                    }
                }
            }
            else
            {
                addMethod.Invoke(newCollection,
                                     editBeforeAdding != null
                                     ? new[] { editBeforeAdding(collection) }
                                     : new[] { collection });
            }
        }

        private static async Task AddItemToCollectionAsync(object newCollection, object collection, Func<object, CancellationToken, Task<object>> editBeforeAdding = null, CancellationToken cancellationToken = default)
        {
            var addMethod = newCollection.GetType().GetInterfaces()
                        .SelectMany(o => o.GetMethods())
                        .First(o => o.Name == "Add");

            var enumerable = collection as IEnumerable;
            if (enumerable != null)
            {
                var enumerableType = enumerable.GetType();
                if (enumerableType.IsAssignableToGenericType(typeof(IDictionary<,>)))
                {
                    foreach (dynamic pair in enumerable)
                    {
                        addMethod.Invoke(newCollection,
                            editBeforeAdding != null
                                ? new[] { pair.Key, await editBeforeAdding(pair.Value, cancellationToken).ConfigureAwait(false) }
                                : new[] { pair.Key, pair.Value }
                        );
                    }
                }
                else
                {
                    foreach (var item in enumerable)
                    {
                        addMethod.Invoke(newCollection,
                                         editBeforeAdding != null
                                         ? new[] { await editBeforeAdding(item, cancellationToken).ConfigureAwait(false) }
                                         : new[] { item });
                    }
                }
            }
            else
            {
                addMethod.Invoke(newCollection,
                                     editBeforeAdding != null
                                     ? new[] { await editBeforeAdding(collection, cancellationToken).ConfigureAwait(false) }
                                     : new[] { collection });
            }
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
