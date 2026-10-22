using FreightEstApp35;
using Microsoft.Data.SqlClient;
using NUnit.Framework;

namespace FreightEstimator_Console_NUnit
{
    public class ConnectionStringsTests
    {
        [Test]
        public void TestMode_UsesIntegratedSecurityAndNoCredentials()
        {
            string text = ConnectionStrings.Build(RunMode.Test, "SANDBOX", "sa", "ignored-secret");
            var parsed = new SqlConnectionStringBuilder(text);

            Assert.That(parsed.IntegratedSecurity, Is.True);
            Assert.That(parsed.DataSource, Is.EqualTo("SANDBOX"));
            Assert.That(parsed.InitialCatalog, Is.EqualTo("UpsRate"));
            Assert.That(parsed.UserID, Is.Empty);
            Assert.That(parsed.Password, Is.Empty);
            Assert.That(text, Does.Not.Contain("ignored-secret"));
        }

        [Test]
        public void ProductionHost_KeepsSqlAuthentication()
        {
            string text = ConnectionStrings.Build(RunMode.Production, "dbhost01", "svc_user", "p@ss");
            var parsed = new SqlConnectionStringBuilder(text);

            Assert.That(parsed.IntegratedSecurity, Is.False);
            Assert.That(parsed.DataSource, Is.EqualTo("dbhost01"));
            Assert.That(parsed.UserID, Is.EqualTo("svc_user"));
            Assert.That(parsed.Password, Is.EqualTo("p@ss"));
            Assert.That(parsed.ConnectTimeout, Is.EqualTo(300));
            Assert.That(parsed.Encrypt, Is.EqualTo(SqlConnectionEncryptOption.Optional));
            Assert.That(parsed.TrustServerCertificate, Is.True);
        }
    }
}
