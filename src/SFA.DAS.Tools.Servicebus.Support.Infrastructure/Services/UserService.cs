using Microsoft.AspNetCore.Http;
using System.Linq;

namespace SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services
{
    public class UserService : IUserService
    {        
        private readonly IHttpContextAccessor _httpContextAccessor;
        public UserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public string GetUserId()
        {
            return _httpContextAccessor.HttpContext?.User.Claims.Where(x => x.Type.Contains("nameidentifier")).Select(x => x.Value).FirstOrDefault();
        }

        public string GetName()
        {
            return _httpContextAccessor.HttpContext?.User.Claims.Where(x => x.Type == "name").Select(x => x.Value).FirstOrDefault();
        }

        public void Configure(string userId, string userName)
        {
            throw new System.NotImplementedException();
        }
    }
}
