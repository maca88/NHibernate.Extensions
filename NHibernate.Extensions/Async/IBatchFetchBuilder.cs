﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by AsyncGenerator.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------


using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace NHibernate.Extensions
{
    using System.Threading.Tasks;
    using System.Threading;

    public partial interface IBatchFetchBuilder<TEntity, TKey>
    {

        Task<List<TEntity>> ExecuteAsync(CancellationToken cancellationToken = default(CancellationToken));
    }

    public partial interface IBatchFetchBuilder<TEntity, TKey, TResult>
    {

        Task<List<TResult>> ExecuteAsync(CancellationToken cancellationToken = default(CancellationToken));
    }
}
