using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services;

namespace SFA.DAS.Tools.Servicebus.Support.Functions
{
    public class FunctionUserService : IUserService
    {
        private string _userId;
        private string _name;

        public void Configure(string userId, string userName)
        {
            _userId = userId;
            _name = userName;
        }

        public string GetName()
        {
            return _name;
        }

        public string GetUserId()
        {
            return _userId;
        }

        public bool IsAdmin()
        {
            throw new System.NotImplementedException();
        }
    }
}
