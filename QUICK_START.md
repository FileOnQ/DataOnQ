# DataOnQ - Quick Start
DataOnQ is a complex offline synchroinzation library, but if you want to get started as quickly as possible you are in the right place! The quick start guide doesn't go into any detail on the Middleware, please take a look at the [Specification](SPECIFICATION.md) for more details.

## Middleware
Use the `HttpOfflineAvailableMiddleware` to implement an offline available service layer. The Middleware implements the following Binary Tree

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

## Contract IToDoService
The Quick Start guide documents how to create a simple `IToDoService` contract for retrieving `ToDoItem`. 

```c#
public interface IToDoService
{
    IEnumerable<ToDoItem> GetItems();
}
```

# Create Service Implementation
All service implementations need to complete the following ations
* Middleware definition (Attribute)
* Proxy Callback

DataOnQ utilizes a callback expression that is executed at each step of the Binary Tree. The proxy is defined in a `ServiceWrapper<TContract>` which all Services need to inherit from.

```c#
public class ToDoService : ServiceWrapper<IToDoService>, IToDoService
{
    public IEnumerable<ToDoItem> GetItems()
    {
        return Proxy<IEnumerable<ToDoItem>>(x => x.GetItems());
    }
}
```

Now you can add the Middleware to either the method or class both are valid. In the Quick Start we are going to define the Middleware at the class level, this means all APIs in our `ToDoService` will use this Middleware.

```c#
[Middleware(typeof(HttpOfflineAvailableMiddleware))]
```

Just apply the Attribute to the `ToDoService`
```c#
[Middleware(typeof(HttpOfflineAvailableMiddleware))]
public class ToDoService : ServiceWrapper<IToDoService>, IToDoService
{
    public Task<IEnumerable<ToDoItem>> GetItems()
    {
        return Proxy<Task<IEnumerable<ToDoItem>>>(x => x.GetItems());
    }
}
```

# Create Service Handlers
Now that the service is defined, you will need to implement several handlers to caputre the business rules for
* Read
* HTTP Request
* Write

There is no need to implement any handlers for Network Connectivity, that is automatically handled in the DataOnQ provided Middleware.

## Read Handler
Assuming you are using SQLite database to store the data, the Read Handler will query the local database and return the data.

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

## Http Handler
Using the `HttpClient` from .NET access the backend service and return the result

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

## Write Handler
As the current execution processes through the Binary Tree, it is useful to get the Previous Result from another Handler. There is a `IAttachProxy` interface that can be applied to any Handler. This is will attach the `IHandlerResponse` which will store information about what happened including the entire response payload

```c#
public interface IAttachProxy
{
    void Attach(IMessageProxy proxy);
}
```

```c#
public interface IMessageProxy
{
    IHandlerResponse PreviousResponse { get; set; }
}

public interface IMessageProxy<TService> : IMessageProxy
{
    IHandlerResponse Execute(TService service);
}
```

```c#
public interface IHandlerResponse
{
    bool IsSuccess { get; }
    TResult GetResult<TResult>();
}
```

Now that we understand the additional interfaces that provide access to previous executed handlers, we just attach them to the current handler.

```c#
public class WriteToDoHandler : IToDoService
{
    private SQLiteConnection _database;
    public WriteToDoHandler(SQLiteConnection database)
    {
        _database = database;
    }

    public async Task<IEnumerable<ToDoItem>> GetItems()
    {
        // TODO - Write the Previous Response
    }
}
```

Update the `WriteToDoHandler` to implement the `IAttachProxy` and store the Previous Response.

```c#
public class WriteToDoHandler : IToDoService, IAttachProxy
{
    protected IHandlerResponse PreviousResponse { get; private set; }

    private SQLiteConnection _database;
    public WriteToDoHandler(SQLiteConnection database)
    {
        _database = database;
    }

    void IAttachProxy.Attach(IMessageProxy proxy)
    {
        PreviousResponse = proxy.PreviousResponse;
    }

    public async Task<IEnumerable<ToDoItem>> GetItems()
    {
        // TODO - Write the Previous Response
    }
}
```

Finally, we can create the implementation of `GetItems()` since we have stored the Previous Response.

```c#
public class WriteToDoHandler : IToDoService, IAttachProxy
{
    protected IHandlerResponse PreviousResponse { get; private set; }

    private SQLiteConnection _database;
    public WriteToDoHandler(SQLiteConnection database)
    {
        _database = database;
    }

    void IAttachProxy.Attach(IMessageProxy proxy)
    {
        PreviousResponse = proxy.PreviousResponse;
    }

    public async Task<IEnumerable<ToDoItem>> GetItems()
    {
        if (PreviousResponse == null)
            throw new InvalidOperationException("Unable to write to local database, there is no PreviousResult");

        var items = PreviousResponse.GetResult<IEnumerable<ToDoItem>>();
        _database.InsertRange(items);

        return items;
    }
}
```

# Configure Handlers
Now that we have defined our `ToDoService` and all of the necessary handlers to implement the `HttpOfflineAvailableMiddleware`, we can put it all together and build our system. This is done by mapping our handlers to the Middleware steps.

During the App Startup routine add the following code
```c#
DataOnQ
    .RegisterHandler<HttpOfflineAvailableMiddleware.ReadHandler>()
    .From<IToDoService>()
    .To<QueryToDoHandler>();

DataOnQ
    .RegisterHandler<HttpOfflineAvailableMiddleware.HttpHandler>()
    .From<IToDoService>()
    .To<HtppToDoHandler>()

DataOnQ.
    .RegisterHandler<HttpOfflineAvailableMiddleware.WriteHandler>()
    .From<IToDoService>()
    .To<WriteToDoHandler>();

DataOnQ.Register<IToDoService, ToDoService>();
```

# View Model Usage
The goal of DataOnQ is to decouple the offline data access rules from the View Model as well as the Service Implementation. Each part of the offline data access is isolated to enforce the Single Responsbility Principle.

Consider a `ToDoViewModel` which has a collection of Items which are rendered on a Page.
```c#
public class ToDoViewModel
{
    public ToDoViewModel()
    {
        Items = new ObservableCollection<ToDoItem>();
    }

    public ObservableCollection<ToDoItem> Items { get; }
}
```

To access the service, use the special Service Locator built into the DataOnQ API.
```c#
IToDoService service = DataOnQ.Get<IToDoService>();
```

Adding this into the `ToDoViewModel` will look like this
```c#
public class ToDoViewModel
{
    public ToDoViewModel()
    {
        IToDoService service = DataOnQ.Get<IToDoService>();
        var myItems = service.GetItems();

        Items = new ObservableCollection<ToDoItem>(myItems);
    }

    public ObservableCollection<ToDoItem> Items { get; }
}
```