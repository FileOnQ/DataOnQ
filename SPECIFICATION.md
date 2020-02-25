# DataOnQ - Specification
The DataOnQ Specification is built off of a real-world internal library that [Andrew Hoefling (@ahoefling)](https://github.com/ahoefling) built for FileOnQ on a Xamarin.Forms project. If you are interested in why this library was built or some of the background leading up to the project, check out the [Problem Space](PROBLEM_SPACE.md) and our [Vision](VISION.md).

# Middleware
DataOnQ is built using a Middleware Architecture ([Chain of Responsibility](https://en.wikipedia.org/wiki/Chain-of-responsibility_pattern)) which is very similar to the ASP.NET Core Middleware that many .NET Core developers are using today. At the most basic level, DataOnQ provides a middleware for application developers to program exactly how their Data Access works with a [Binary Tree](https://en.wikipedia.org/wiki/Binary_tree).

When DataOnQ v1 is released there will be pre-loaded middlewares that you can use right out of the box. However, if the user has custom rules they want to implement, they can write their own set of rules via the DataOnQ Middleware. Without the Middleware, a DataOnQ usage would turn into a mess and would create no benefit to the developers and their product.

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
In FileOnQ's [Problem Space](PROBLEM_SPACE.md), DataOnQ was designed to work with a WCF XML SOAP library. There is a custom Middleware built specifically for this and it follows the exact same rules as the HTTP Middleware. 

The only difference is instead of a simple HTTP request it creates a WCF HTTP Request.

## Custom Middlewares
Each step in the execution of the Middleware can be customized and applied to any individual API or an entire class. If you have special pre-conditions that your app is okay allowing, you can write a custom step into the middleware to check that.

For example, if your app is okay using 10 day old data, you can add a custom check as the first step on the middleware and have it return local data.

# Service Implementation
Every library will need to implement some type of Service to start accessing data. DataOnQ provides a `ServiceWrapper` which helps build a Proxy to invoke the Middleware. All code samples will use our ToDo App Example that is documented in the [Vision](VISION.md).

The ToDo App uses a `IToDoService` which defines an API for retrieving all the items to render onto the page.

```c#
public interface IToDoService
{
    IEnumerable<ToDoItem> GetItems();
}
```

## Implementation
In a traditional Service Implementation that does not use DataOnQ, you may have database queries, network validation and HTTP requests. By leveraging the DataOnQ `ServiceWrapper` the implementation defines an API proxy to invoke.

The `ServiceWrapper` only provides the necessary hooks for defining the API to invoke and starts the middleware.

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
    public IEnumerable<ToDoItem> GetItems()
    {
        return Proxy<IEnumerable<ToDoItem>>(x => x.GetItems());
    }
}
```

To properly invoke the Middleware your service implementation places the correct Lambda Expression as the parameter to the Proxy. This will be invoked by DataOnQ during the Middleware execution
```c#
Proxy<IEnumerable<ToDoItem>>(x => x.GetItems());
```

## Middleware Attribute
The `MiddlewareAttribute` defines the type of Middleware process to invoke. The Middleware can be configured at the class level or method level, which allows the project to handle different scenarios for different APIs.

In the example above the `OfflineAvailableMiddlewareAttribute` is an implementation of the standard middleware defined in the Binary Tree above. A project can customize their own Middlewares, and documentation on how that works is below in the Middleware Implementation Section.

# Service Handlers
Now that the Service Implementation is defined, you will need to implement a series of `IServiceHandler`'s. The `IServiceHandler` defines the contract that will be executed at each step of the Middleware.

Consider the Binary Tree defined earlier, each `IServiceHandler` is the implementation of one step of the workflow. 

Contracts for `IServiceHandler` and related objects:

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

## Standard Service Handler
DataOnQ provides a standard `ServiceHandler` which implements the `IServiceHandler`. This class should be subclassed for each step in your workflow, that will need to resolve an implementation of your `TContract`, in our case `IToDoService`:

* Query Local Database
* Write Local Database
* HTTP Request

```c#
public abstract class ServiceHandler : IServiceHandler
{
    IHandlerResponse Handle(IMessageProxy payload);
    virtual IHandlerResponse Handle<TService>(IMessageProxy<TService> proxy);
}
```

The `Handle()` API is invoked by the Middleware and utilizes the `IHandlerResponse` to decide which direction it should traverse the Binary Tree. 

## Custom IServiceHandler - Can Access Internet Service
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
Every `IServiceHandler` returns a `HandlerResponse` this response defines if the Handler is successful or not which is then processed in the Middleware. The `NetworkServiceHandler` needs to communicate back to the middleware if it can make an API request or needs to query the local database.
```c#
return new HandlerResponse(null, hasInternet);
```
At this point in the payload there is no need to return any data, which is why the first parameter is null.

The `NetworkServiceHandler` is a special case service handler that doesn't do anything with the API excpet determine if we have internet or not. 

## Read Local Database
To properly read the local database, our middleware will need to create special rules that resolve the correct implementation of `IToDoService` that only reads the local database.

Create a `QueryLocalDatabaseServiceHandler` by sub-classing the `ServiceHandler`.

```c#
public class QueryLocalDatabaseServiceHandler : ServiceHandler
{
}
```

There is no need to do anything in this class, unless you want to override any of the default behaviors.

### Contract Implementation
Now that the `ServiceHandler` is defined and implemented for reading the local database, the Contract can be implemented for reading local data. This implementation will only be reading from a SQLite database, focusing on the isolation of this part of the Middleware.

```c#
public class QueryToDoHandler : IToDoService
{
    private SQLiteConnection _database;
    public QueryToDoHandler(SQLiteConnection database)
    {
        _database = database;
    }

    public async Task<IEnumerable<ToDoItem>> GetItems()
    {
        return _database
            .Table<ToDoItem>()
            ToArray();
    }
}
```

### Register Contract & Implementation
Each Contract implementation needs to be registered with DataOnQ to work correctly. 

Add the following code to your App Start Sequence, typically in the `App.xaml.cs`
```c#
DataOnQ
    .RegisterHandler<QueryLocalDatabaseServiceHandler>()
    .From<IToDoService>()
    .To<QueryToDoHandler>();
```

## Invoke HTTP Request
When it is time to retrieve data from a remote API, the Middleware will need to implement necessary Handler and Contract. 

Create a `HttpServiceHandler` by sub-classing the `ServiceHandler`
```c#
public class HttpServiceHandler : ServiceHandler
{
}
```
There is no need to do anything in this class, unless you want to override any of the default behaviors.

### Contract Implementation
Now that the `ServiceHandler` is defined and implemented for making HTTP Requests to a remote server, the Contract can be implemented. This implementation will only be sending HTTP Requests to a remote server, focusing on the isolation of this part of the Middleware.

```c#
public class HttpToDoHandler : IToDoService
{
    public async Task<IEnumerable<ToDoItem>> GetItems()
    {
        HttpClient client = new HttpClient();
        Uri uri = new Uri("Rout/To/Server");
        
        var response = await client.GetAsync(uri);
        if (!response.IsSuccessStatusCode)
            return new ToDoItem[0];

        var content = await response.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<List<ToDoItem>>(content);
    }
}
```

This contract implementation focuses only on making the HTTP request and returning the data. There is no need to worry about anything else and let the Middleware process the data.

### Register Contract & Implementation
Each Contract implementation needs to be registered with DataOnQ to work correctly. 

Add the following code to your App Start Sequence, typically in the `App.xaml.cs`
```c#
DataOnQ
    .RegisterHandler<HttpServiceHandler>()
    .From<IToDoService>()
    .To<HttpDoDoHandler>();
```

## Write Local Database
Our standard Middleware workflow defines a step to write the newly retrieved data from the `HttpServiceHandler` to the local databse. This will help make sure future reads are accurate and the app does not have internet connectivity. The goal of this handler is to take the data from the Previous Handler and save it to the local database.

Create a `WriteLocalDatabaseServiceHandler` by sub-classing the `ServiceHandler`
```c#
public class WriteLocalDatabaseServiceHandler : ServiceHandler
{
}
```
There is no need to do anything in this class, unless you want to override any of the default behaviors.

### Contract Implementation
Now that the `ServiceHandler` is defined and implemented for making writing to a local database, the Contract can be implemented. This implementation needs to complete the following steps:
1. Retrieve previouse handler result via `IAttachProxy`
2. Process data and save to local database
3. Return processed data

The `IAttachProxy` is an optional interface that can be added to any Contract implementation. It provides a special method where the Contract implementation can retrieve the `IMessageProxy` which contains useful information about the last response and other properties on the Middleware

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
        if (PreviousResponse == null)
            throw new InvalidOperationException("Unable to write to local database, there is no PreviousResult");

        var items = PreviousResponse.GetResult<IEnumerable<ToDoItem>>();
        _database.InsertRange(items);

        return items;
    }
}
```

### Register Contract & Implementation
Each Contract implementation needs to be registered with DataOnQ to work correctly. 

Add the following code to your App Start Sequence, typically in the `App.xaml.cs`
```c#
DataOnQ
    .RegisterHandler<WriteLocalDatabaseServiceHandler>()
    .From<IToDoService>()
    .To<WriteToDoHandler>();
```

## Binary Tree Creation
All of the building blocks are in place to create the Middleware, you can now create your system of rules or Binary Tree.

There is a special `BinaryTree<T>` object defined in DataOnQ.
```c#
public class BinaryTree<T>
{
    public T Value { get; set; }
    public BinaryTree<T> Left { get; set; }
    public BinaryTree<T> Right { get; set; }
}
```

To construct your system, create a new implementation of `IServiceHandler` in our case we are going to call it `OfflineAvailableMiddleware`.

```c#
public class OfflineAvailableMiddleware : IServiceHandler
{
    protected BinaryTree<IServiceHandler> HandlerTree { get; set; }
    public OfflineAvailableMiddleware()
    {
        var queryLocal = new BinaryTree<IServiceHandler>
        {
            Value = ResolveHandler<QueryLocalDatabaseServiceHandler>()
        };

        HandlerTree = new BinaryTree<IServiceHandler>
        {
            Value = ResolveHandler<NetworkServiceHandler>(),
            Left = new BinaryTree<IServiceHandler>()
            {
                Value = ResolveHandler<HttpServiceHandler>(),
                Left = new BinaryTree<IServiceHandler>
                {
                    Value = ResolveHandler<WriteLocalDatabaseServiceHandler>()
                },
                Right = queryLocal
            },
            Right = queryLocal
        };
    }
}
```
The constructed Middleware is a code definition of our Binary Tree represented below:
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

### Generic Structure
Note, that the entire `OfflineAvailableMiddleware` uses the `ServiceHandler` implementations instead of the `IToDoService` Contract implementations. This is by design, which allows this Middleware to be used on any service we want. We will just need to implement the exact same Handlers and register them like before.

### Use the Middleware
To use this Middleware, you will just decorate a method or class
```c#
[Middleware(typeof(OfflineAvailableMiddleware))]
```


# Simplified Middlewares
Currently DataOnQ doesn't have any out of the box Middlewares that a developer can use. The goal is to define several common use-cases for the MVP delivery of the project. Some examples of standard Middlewares will be

* HttpMiddleware
* WCFMiddleware

## HttpMiddleware
The customized solution above describes an implementation of a custom `HttpMiddleware`. This will be a Middleware that ships with DataOnQ to simplify an implementation. Instead of building each