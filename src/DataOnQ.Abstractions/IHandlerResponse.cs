namespace DataOnQ.Abstractions
{
    public interface IHandlerResponse
    {
        bool IsSuccess { get; }
        TResult GetResult<TResult>();
    }
}
