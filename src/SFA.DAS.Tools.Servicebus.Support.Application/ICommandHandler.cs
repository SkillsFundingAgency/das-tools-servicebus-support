using System.Threading.Tasks;

namespace SFA.DAS.Tools.Servicebus.Support.Application
{
    public interface ICommandHandler<in TIn, TOut>
    {
        Task<TOut> Handle(TIn query);
    }
}
