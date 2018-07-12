
NHibernate.Extensions
=====================

Various additions for NHibernate like the Include method from EntityFramework and a smart deep cloning method.

## Install via NuGet

If you want to include NHibernate.Extensions in your project, you can [install it directly from NuGet](https://www.nuget.org/packages/NHibernate.Extensions).

To install NHibernate.Extensions, run the following command in the Package Manager Console.

```
PM> Install-Package NHibernate.Extensions
```

## Include method

Is an extension method for Linq used to eager load entity relations without worrying about cartesian product in sql. Under the hood this method uses NHibernate Fetch methods in conjunction with NHibernate Future methods. The order of the Include methods is not important as there is a logic that calculates the minimum number of queries that are needed to fetch all relations without having any cartesian product. Let's look at an example:

```cs
var people = session.Query<EQBPerson>()
	.Include(o => o.BestFriend.IdentityCard) /* nested many to one relations */
	.Include(o => o.BestFriend.BestFriend.BestFriend.BestFriend) /* nested many to one relations */
	.Include(o => o.CurrentOwnedVehicles).ThenInclude(o => o.Wheels) /* nested one to many relations */
	.Include(o => o.DrivingLicence) /* many to one relation */
	.Include(o => o.IdentityCard) /* many to one relation */
	.Include(o => o.MarriedWith) /* many to one relation */
	.Include(o => o.OwnedHouses) /* one to many relation */
	.Include(o => o.PreviouslyOwnedVehicles) /* one to many relation */
	.ToList();
```

In the above example we are eager loading a lot relations but if we want to calculate the minimum number of queries we need to worry about one to many relations in order to prevent cartesian products. When we are eager loading nested one to many relations we won't have a cartesian product so we can load them in one query. So for the above example the minimum number of queries to fetch all relations without having cartesian products is 3. Let's see now the equivalent code using Fetch and Future methods:

```cs
var query = session.Query<EQBPerson>()
	.Fetch(o => o.BestFriend)
		.ThenFetch(o => o.IdentityCard)
	.Fetch(o => o.BestFriend)
		.ThenFetch(o => o.BestFriend)
		.ThenFetch(o => o.BestFriend)
		.ThenFetch(o => o.BestFriend)
	.FetchMany(o => o.CurrentOwnedVehicles)
		.ThenFetchMany(o => o.Wheels)
	.Fetch(o => o.DrivingLicence)
	.Fetch(o => o.IdentityCard)
	.Fetch(o => o.MarriedWith)
	.ToFuture();
session.Query<EQBPerson>()
	.FetchMany(o => o.OwnedHouses)
	.ToFuture();
session.Query<EQBPerson>()
	.FetchMany(o => o.PreviouslyOwnedVehicles)
	.ToFuture();
var people = query.ToList();
```

The whole idea of the Include method is to simplify eager loading in NHibernate.


## DeepClone

Is a extension method for NHibernate Session for deep cloning an entity with its relations that are currently loaded. This method can be used in various scenarios. Serialization is one of them as we can serialize a deep cloned entity without worrying about lazy loadings upon serialization as by default the deep cloned entity does not contain any proxies. Let's see a simple example:

```cs
EQBPerson petra;
using (var session = NHConfig.OpenSession())
{
	petra = session.Query<EQBPerson>()
		.First(o => o.Name == "Petra");
	clone = session.DeepClone(petra);
	// Lazy load some relations after cloning
	var friend = petra.BestFriend;
	var card = petra.IdentityCard;

}
Assert.AreEqual(petra.Id, clone.Id);
Assert.AreEqual(petra.Name, clone.Name);
Assert.AreEqual(petra.LastName, clone.LastName);
Assert.IsNotNull(petra.BestFriend);
Assert.IsNotNull(petra.IdentityCard);
Assert.IsNull(clone.BestFriend);
Assert.IsNull(clone.IdentityCard);
```

In the above example we fetched a person without including any additional relation so the DeepClone method will only clone the person itself. In addition the cloned entity relations are not proxies so it can be easily serialized. The DeepClone method accepts an additional parameter in order to tune the cloning. Let's take a look:

```cs
EQBPerson clone;
EQBPerson petra;
using (var session = NHConfig.OpenSession())
{
	petra = session.Query<EQBPerson>()
		.Include(o => o.IdentityCard)
		.First(o => o.Name == "Petra");
	clone = session.DeepClone(petra, o => o
		.ForType<EQBPerson>(t => t
			.ForMember(m => m.Name, opts => opts.Ignore())
			.CloneIdentifier(false)
		)
		.CanCloneAsReference(type => type == typeof(EQBIdentityCard))
		);

}
Assert.AreEqual(clone.Id, default(int));
Assert.IsNull(clone.Name);
Assert.AreEqual(petra.LastName, clone.LastName);
Assert.AreEqual(petra.IdentityCard, clone.IdentityCard);
```

As seen in the above example we can tune cloning on a global level or type level. The type level tuning will override the global one when using both.
