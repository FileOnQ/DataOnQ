# DataOnQ - Specification
The DataOnQ Specification is built off of a real-world internal library that [Andrew Hoefling (@ahoefling)](https://github.com/ahoefling) built for FileOnQ on a Xamarin.Forms project. If you are interested in why this library was built or some of the background leading up to the project, check out the [Problem Space](PROBLEM_SPACE.md).

# The DataOnQ Vision
Please take a look at our [Vision](VISION.md) which goes into the basic example of DataOnQ and why the specification is designed the way it is.

# Middleware
DataOnQ is built using a Middleware Architecture ([Chain of Responsibility](https://en.wikipedia.org/wiki/Chain-of-responsibility_pattern)) which is very similar to the ASP.NET Core Middleware that many .NET Core developers are using today. At the most basic level, DataOnQ provides a middleware for application developers to program exactly how their Data Access works with a [Binary Tree](https://en.wikipedia.org/wiki/Binary_tree).

When DataOnQ v1 is released there will be pre-loaded middlewares that you can use right out of the box. However, if the user has custom rules which is the case in many offline capable apps the development team can write their own set of rules via the DataOnQ Middleware. Without the Middleware, a DataOnQ usage would turn into a mess and would create no benefit to the developers and their product.

## Basic HTTP Middleware
Most applications have a basic HTTP request model for retrieving their data. This would require the application to make HTTP requests and typically receive JSON data in return that the application can then process.

Here is an example of what the HTTP Middleware would look like
```
            (Can Access Internet or Server)
            /                             \
   false   /                               \   true
          /                                 \
(Read Local Database)               (Invoke HTTP Request)
                                    /                   \
                           false   /                     \ true
                                  /                       \
                (Read Local Database)      (Update Local Database)
```

### Scenario 1: No Internet
If the device does not have any internet DataOnQ will read your data directly from the local database and return immediately.

### Scenario 2: Internet, Failed HTTP Request
If the device has internet but is unable to receive a 200 OK response from the backend service, it will read from the Local Database. DataOnQ understands that it is trying to receive data but when it fails, the fallback logic programmed into the Middleware tells it to read from the local database.

This fallback logic, keeps the contract to return proper data and the consuming View Model doesn't know the difference.

### Scenario 3: Internet With Valid HTTP Request
If the device has internet and is able to receive a proper 200 OK response from the backend server the final middleware is invoked. In this workflow it will return the data, but at the same time it will update the local database. If the next request there is no internet, the local database is up-to date and there is no problems with the invocation.

## WCF Middleware
In the [Problem Space](PROBLEM_SPACE.md), it describes how DataOnQ was designed to work with a WCF XML SOAP library. There is a custom Middleware built specifically for this and it follows the exact same rules as the HTTP Middleware. 

The only difference is instead of a simple HTTP request it creates a WCF HTTP Request.

## Custom Middlewares
Each step in the execution of the Middleware can be customized and applied to any individual API or an entire class. If you have special pre-conditions that your app is okay allowing, you can write a custom step into the middleware to check that.

For example, if your app is okay using 10 day old data, you can add a custom check as the first step on the middleware and have it return local data.

# Service Implementation
Every library will need to implement some type of Service to start accessing data. DataOnQ provides a `ServiceWrapper` which helps build a Proxy to invoke the Middleware. Using our sample app from the [Vision](VISION.md).

All Service's should know inherit from the `ServiceWrapper<T>` which creates a Proxy to easily invoke any action.

```c#
public abstract ServiceWrapper<TContract>
{
    protected virtual T Proxy<T>(Expression<Func<TContract, object>> action);
}
```

```c#
[Middleware(typeof(OfflineAvailableMiddleware)]
public class ToDoService : ServiceWrapper<IToDoService>, IToDoService
{
    public object GetItems()
    {
        return Proxy<object>(x => x.GetItems());
    }
}
```

## Middleware Attribute
The `MiddlewareAttribute` defines the type of Middleware process to invoke. The Middleware can be configured at the class level or method level, which allows the project to handle different scenarios for different APIs.

In the example above the `OfflineAvailableMiddlewareAttribute` is an implementation of the standard middleware defined in the Binary Tree above. A project can customize their own Middlewares, and documentation on how that works is below in the Middleware Implementation Section.

# Service Handlers
Now that the Service Implementation is defined, you will need to implement a series of `IServiceHandler`'s. The `IServiceHandler` defines the contract that will be executed at each step of the Middleware.

```c#
public interface IServiceHandler
{
    IHandlerResponse Handle<TService>(IMessageProxy<TService> proxy);
}

public interface IHandlerResponse
{
    bool IsSuccess { get; }
    TResult GetResult<TResult>();
}

public interface IMessageProxy
{
    IHandlerResponse PreviousResponse { get; set; }
}

public interface IMessageProxy<TService> : IMessageProxy
{
    IHandlerResponse Execute(TService service);
}
```

Most implementation of Service Handlers do not need to use the `ServiceHandler` or the `IServiceHandler` these APIs are referenced here to better explain the `NetworkServiceHandler` in the next section. Most Service Handlers will just implement your `IToDoService`.

Using our pre-built Middleware described earlier, the `ToDoService` will need a custom implementation for
* Access Internet
* Read Local Database
* Invoke HTTP Request
* Write Local Database

## Can Access Internet Service
The root node checks if the device has access to the internet. This can be as simple or complicated as you want to make it. In any custom Service Handler 

```c#
public class NetworkServiceHandler : IServiceHandler
{
    public IHandlerResponse Handle<TService>(IMessageProxy<TService> proxy)
    {
        // Using Xamarin Essentials check if the device has internet access,
        // then return the value in the response.
        bool hasInternet = Connectivity.NetworkAccess == NetworkAccess.Internet);

        return new HandlerResponse(null, hasInternet);
    }
}
```

The `NetworkServiceHandler` is a special case service handler that doesn't do anything with the API excpet determine if we have internet or not. 

## Read Local Database


```c#
public class QueryToDoHandler : IToDoService
{
    private SQLiteConnection _database;
    public QueryToDoHandler(SQLiteConnection database)
    {
        _database = database;
    }

    public object GetItems()
    {
        return _database
            .Table<ToDo>()
            ToArray();
    }
}
```

## Invoke HTTP Request
When retrieving the data from the backend service it will need to be stored

```c#
public class HttpToDoHandler : IToDoService
{
    public object GetItems()
    {
        return SomeApi.GetItems();
    }
}
```

## Write Local Database
When it is time to write to the local database, you will need to get the data stored in the Middleware. There is another interface `IAttachProxy` which is invoked prior to your API. This is the hook to store current result.

```c#
public interface IAttachProxy
{
    void Attach(IMessageProxy proxy);
}
```

```c#
public class WriteToDoHandler : IToDoService, IAttachProxy
{
    // Property to store the previous repsonse saved earlier in
    // the middleware workflow
    protected IHandlerResponse PreviousResponse { get; private set; }

    private SQLiteConnection _database;
    public WriteToDoHandler(SQLiteConnection database)
    {
        _database = database;
    }

    public void Attach(IMessageProxy proxy)
    {
        PreviousResponse = proxy.PreviousResponse;
    }

    public object GetItems()
    {
        if (PreviousResponse)
            throw new InvalidOperationException("Unable to write to local database, there is no PreviousResult");

        var items = PreviousResponse.GetResult<object>();
        _database.InsertRange(items);

        return items;
    }
}
```

# Middleware Implementation
The middleware implementation is broken into a series of Handlers that process each step in the Binary Tree of the Middleware. 

```c#
public interface IServiceHandler
{
    IHandlerResponse Handle<TService>(IMessageProxy<TService> proxy);
}
```

DataOnQ will ship with several pre-defined `IServiceHandler` implementations for the standard operations:
* Network Available
* Query Local Database
* Write to Local Database
* HTTP Request
* WCF Request
* etc.

The `IServiceHandler` provides a proxy to perform specific actions. There is a default implementation called `ServiceHandler` which is intended to be used with basic CRUD operations including HTTP request

## Service Handler
A `ServiceHandler` is an abstract class that provides an easy way to run specific operations that are for the API such as CRUD (Create, Read, Update and Delete) operations.

## Custom Rule Handler
A custom rule handler provides a mechanism to process non-CRUD operations such as:
* Determining if the device has internet
* Checking connectivity to a specific server
* Checking if local data is stale
* etc.

--
