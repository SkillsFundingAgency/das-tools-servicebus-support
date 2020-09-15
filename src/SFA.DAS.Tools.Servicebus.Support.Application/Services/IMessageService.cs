using System.Threading.Tasks;

namespace SFA.DAS.Tools.Servicebus.Support.Application.Services
{
    public interface IMessageService
    {
        Task ProcessMessages(string queue, Transactional transaction = Transactional.Yes);
    }
}
