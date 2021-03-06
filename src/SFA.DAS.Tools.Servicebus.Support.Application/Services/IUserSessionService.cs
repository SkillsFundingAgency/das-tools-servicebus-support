﻿using SFA.DAS.Tools.Servicebus.Support.Domain;
using System.Threading.Tasks;

namespace SFA.DAS.Tools.Servicebus.Support.Application.Services
{
    public interface IUserSessionService
    {
        Task<UserSession> UpsertUserSession(string queue);
        Task<UserSession> GetUserSession();
        Task DeleteUserSession();
    }
}