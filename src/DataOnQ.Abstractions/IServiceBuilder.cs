namespace DataOnQ.Abstractions
{
    public interface IServiceBuilder
    {
        void Register<T>() where T : IServiceHandler;
    }
}
