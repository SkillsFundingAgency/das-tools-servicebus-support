using System;
using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace SFA.DAS.Tools.Servicebus.Support.Infrastructure
{
    [Obsolete("These extensions are never used", true)]
    public static class SessionExtensions
    {
        [Obsolete("These extensions are never used", true)]
        public static void Set<T>(this ISession session, string key, T value) => session.SetString(key, JsonSerializer.Serialize(value));

        [Obsolete("These extensions are never used", true)]
        public static T Get<T>(this ISession session, string key)
        {
            var value = session.GetString(key);
            return value == null ? default : JsonSerializer.Deserialize<T>(value);
        }
    }
}
