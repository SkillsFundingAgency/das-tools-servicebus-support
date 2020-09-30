namespace SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services
{
    public interface IUserService
    {
        string GetUserId();
        string GetName();

        void Configure(string userId, string userName);
    }
}
