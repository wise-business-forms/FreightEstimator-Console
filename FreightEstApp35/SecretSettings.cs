using System;
using System.Collections.Generic;
using System.Linq;

namespace FreightEstApp35
{
    /// <summary>
    /// The carrier credentials the app needs, read from appSettings (the deployed config, or a secrets.config
    /// beside it) and never from source. See ADR 0005. Pure: callers pass in how to look a setting up.
    /// </summary>
    public static class SecretSettings
    {
        // M33_DemoToken is deliberately not required: M33 demo mode is off, so the token is never used.
        public static readonly IReadOnlyList<string> RequiredKeys = new[]
        {
            "UPS_AccessKey", "UPS_UserName", "UPS_Password", "UPS_ClientId", "UPS_ClientSecret",
            "M33_ProdToken",
            "TransPlace_ProdToken", "TransPlace_DemoToken",
            "TI_ApiUsername", "TI_ApiPassword",
        };

        /// <summary>Returns every required key that is not set or is blank.</summary>
        public static IReadOnlyList<string> FindMissing(Func<string, string> getSetting)
        {
            return RequiredKeys.Where(k => string.IsNullOrWhiteSpace(getSetting(k))).ToList();
        }
    }
}
