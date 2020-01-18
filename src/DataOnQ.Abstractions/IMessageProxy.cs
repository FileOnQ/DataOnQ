namespace DataOnQ.Abstractions
{
    public interface IMessageProxy
    {
        IHandlerResponse PreviousResponse { get; set; }
    }

    public interface IMessageProxy<TService> : IMessageProxy
    {
        IHandlerResponse Execute(TService service);
    }
}
