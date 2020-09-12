using System;
using System.Collections.Generic;
using System.Text;

namespace SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services
{
    public static class UserService
    {
        private static string userId = "123456";

        public static string GetUserId()
        {
            return userId;
        }

    
    }
}
