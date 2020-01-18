using System;
using System.Linq.Expressions;

namespace DataOnQ.Abstractions
{
    public interface IMessageProxyCommand<TService>
    {
        Expression<Func<TService, object>> Command { get; }
    }
}
