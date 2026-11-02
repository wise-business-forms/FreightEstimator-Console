using Microsoft.Data.SqlClient;

namespace FreightEstApp35
{
    /// <summary>Builds the carrier-log database connection string for a run mode. No I/O.</summary>
    public static class ConnectionStrings
    {
        // Microsoft.Data.SqlClient defaults to Encrypt=true; these match the old System.Data.SqlClient behavior (see ADR 0003).
        const string SqlSecurityOptions = "Encrypt=False;TrustServerCertificate=True;";

        /// <summary>
        /// Test Mode uses Windows Integrated Security, so no SQL user or password is ever read or sent.
        /// A Production Host keeps SQL authentication as before.
        /// </summary>
        public static string Build(RunMode mode, string server, string user, string password)
        {
            if (mode == RunMode.Test)
            {
                return "Server=" + server + "; Initial Catalog=UpsRate;Integrated Security=True; Connection Timeout=300;" + SqlSecurityOptions;
            }

            return "Server=" + server + "; Initial Catalog=UpsRate;uid=" + user + ";pwd=" + password + "; Connection Timeout=300;" + SqlSecurityOptions;
        }
    }
}
