using Microsoft.AspNetCore.Http;
using System.Linq;

namespace SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services
{
    public class UserService : IUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly string _nameClame;

        public UserService(IHttpContextAccessor httpContextAccessor, string nameClame)
        {
            _httpContextAccessor = httpContextAccessor;
            _nameClame = nameClame;
        }

        public string GetUserId()
        {
            return _httpContextAccessor.HttpContext?.User.Claims.Where(x => x.Type.Contains("nameidentifier")).Select(x => x.Value).FirstOrDefault();
        }

        public string GetName()
        {
            return _httpContextAccessor.HttpContext?.User.Claims.Where(x => x.Type == _nameClame).Select(x => x.Value).FirstOrDefault();
        }

        public bool IsAdmin()
        {
            return _httpContextAccessor.HttpContext != null ? _httpContextAccessor.HttpContext.User.IsInRole("Servicebus Admin") : false;
        }

        public void Configure(string userId, string userName)
        {
            throw new System.NotImplementedException();
        }
    }
}
