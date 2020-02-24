# DataOnQ - The MVVM Vision
In modern Xamarin.Forms, UWP, Uno Platform and WPF applications Model-View-ViewModel (MVVM) architecture is popular to decouple the Views from the Business rules in the View Models. If you aren't familiar with the pattern check out the [Microsoft Docs - MVVM](https://docs.microsoft.com/en-us/xamarin/xamarin-forms/enterprise-application-patterns/mvvm).

Consider a basic ToDo App that displays a list of tasks. The `MainViewModel` may contain an `ObservableCollection` of ToDo items to render to the screen.

```c#
public class MainViewModel
{
    public MainViewModel()
    {
        Items = new ObservableCollection<object>();
    }

    public ObservableCollection<object> Items { get; }
}
```

Now to populate the `Items` collection we are going to use Dependency Injection which will resolve a basic interface of `IToDoService`. 

```c#
public interface IToDoService
{
    IEnumerable<object> GetItems();
}
```

The interface can now be injected into our `MainViewModel` and the data can be resolved

```c#
public class MainViewModel
{
    public MainViewModel(IToDoService todoService)
    {
        todoItems = todoService.GetItems();
        Items = new ObservableCollection<object>(todoItems);
    }

    public ObservableCollection<object> Items { get; }
}
```

## The Power to DataOnQ
Given our example above the interface doesn't implement a standard service that calls an API and returns data. The implementation of `IToDoService` uses a middleware architecture ([Chain of Responbility](https://en.wikipedia.org/wiki/Chain-of-responsibility_pattern)) to automatically handle local database queries for offline mode vs API queries for connected mode. The middleware is powerful enough to even write the data to the local database.

## Why Put All the Power into DataOnQ
Putting all of this power into DataOnQ to handle the offline/online Data Access completely decouples the View Models from any complicated business rules for data access. 

Andrew Hoefling's Opinion
```
As an expereinced Xamarin.Forms developer, I have seen many offline first mobile apps struggle on where the data synchronization routines exist. These rules tend to spread throughout the stack of the application including the View Models, when these rules should be isolated in a Data Access Layer
```

By moving your data access rules into DataOnQ the View Model code remains clean. In our example above the `MainViewModel` invokes the `todoService.GetItems()` API and it doesn't care if it is online or offline. The View Model just needs to know that it has returned some data. 

In the offline disconnected [Problem Space](PROBLEM_SPACE.md) that this library was built for, the simplification of View Models really helped the developement effort succeed on focusing on business rules and not Data Access Rules.
