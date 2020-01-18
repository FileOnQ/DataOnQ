namespace DataOnQ.Abstractions
{
    public interface IServiceHandler
    {
        IHandlerResponse Handle<TService>(IMessageProxy<TService> payload);
    }
}
