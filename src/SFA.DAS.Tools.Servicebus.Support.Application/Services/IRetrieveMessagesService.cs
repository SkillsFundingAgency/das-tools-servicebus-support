using System.Threading.Tasks;

namespace SFA.DAS.Tools.Servicebus.Support.Application.Services
{
    public interface IRetrieveMessagesService
    {
        Task GetMessages(string queueName, long count, int getQty);
    }
}
