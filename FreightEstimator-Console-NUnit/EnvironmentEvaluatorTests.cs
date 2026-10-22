using FreightEstApp35;
using NUnit.Framework;

namespace FreightEstimator_Console_NUnit
{
    public class EnvironmentEvaluatorTests
    {
        static readonly string[] ProductionHosts = { "PRODHOST01", "PRODHOST02" };

        [Test]
        public void UnlistedMachine_PointedAtSandbox_RunsInTestMode()
        {
            var result = EnvironmentEvaluator.Evaluate("DEV-LAPTOP", ProductionHosts, "SANDBOX", "SANDBOX");

            Assert.That(result.IsRefused, Is.False);
            Assert.That(result.Mode, Is.EqualTo(RunMode.Test));
            Assert.That(result.Reason, Does.Contain("DEV-LAPTOP"));
        }

        [Test]
        public void UnlistedMachine_PointedAtAnotherDatabaseServer_RefusesNamingTheSetting()
        {
            var result = EnvironmentEvaluator.Evaluate("DEV-LAPTOP", ProductionHosts, "dbhost01", "SANDBOX");

            Assert.That(result.IsRefused, Is.True);
            Assert.That(result.Refusal, Does.Contain("PROD_Server"));
            Assert.That(result.Refusal, Does.Contain("dbhost01"));
        }

        [Test]
        public void UnlistedMachine_PointedAtAnotherRequestQueueServer_RefusesNamingTheSetting()
        {
            var result = EnvironmentEvaluator.Evaluate("DEV-LAPTOP", ProductionHosts, "SANDBOX", "ALPERP");

            Assert.That(result.IsRefused, Is.True);
            Assert.That(result.Refusal, Does.Contain("RemoteServerName"));
            Assert.That(result.Refusal, Does.Contain("ALPERP"));
        }

        [Test]
        public void ListedMachine_WithProductionServers_RunsAsProductionHost()
        {
            var result = EnvironmentEvaluator.Evaluate("PRODHOST01", ProductionHosts, "dbhost01", "ALPERP");

            Assert.That(result.IsRefused, Is.False);
            Assert.That(result.Mode, Is.EqualTo(RunMode.Production));
            Assert.That(result.Reason, Does.Contain("PRODHOST01"));
        }

        [TestCase("prodhost01")]
        [TestCase("  ProdHost02 ")]
        public void MachineNameMatch_IgnoresCaseAndSurroundingSpace(string machineName)
        {
            var result = EnvironmentEvaluator.Evaluate(machineName, ProductionHosts, "dbhost01", "ALPERP");

            Assert.That(result.Mode, Is.EqualTo(RunMode.Production));
        }

        [Test]
        public void ListedMachine_PointedAtSandboxDatabase_Refuses()
        {
            var result = EnvironmentEvaluator.Evaluate("PRODHOST01", ProductionHosts, "SANDBOX", "ALPERP");

            Assert.That(result.IsRefused, Is.True);
            Assert.That(result.Refusal, Does.Contain("PROD_Server"));
        }

        [Test]
        public void ListedMachine_PointedAtSandboxRequestQueue_Refuses()
        {
            var result = EnvironmentEvaluator.Evaluate("PRODHOST01", ProductionHosts, "dbhost01", "sandbox");

            Assert.That(result.IsRefused, Is.True);
            Assert.That(result.Refusal, Does.Contain("RemoteServerName"));
        }

        [Test]
        public void NoProductionHostsConfigured_IsTestMode_AndRefusesANonSandboxServer()
        {
            var nullList = EnvironmentEvaluator.Evaluate("PRODHOST01", null, "dbhost01", "ALPERP");
            var emptyList = EnvironmentEvaluator.Evaluate("PRODHOST01", new string[0], "dbhost01", "ALPERP");

            Assert.That(nullList.IsRefused, Is.True);
            Assert.That(emptyList.IsRefused, Is.True);
            Assert.That(EnvironmentEvaluator.Evaluate("PRODHOST01", null, "SANDBOX", "SANDBOX").Mode, Is.EqualTo(RunMode.Test));
        }

        [Test]
        public void ParseHostList_SplitsOnCommaAndSemicolon_TrimsAndDropsBlanks()
        {
            Assert.That(EnvironmentEvaluator.ParseHostList(" A1, B2;;C3 ,"), Is.EqualTo(new[] { "A1", "B2", "C3" }));
            Assert.That(EnvironmentEvaluator.ParseHostList(null), Is.Empty);
            Assert.That(EnvironmentEvaluator.ParseHostList("  "), Is.Empty);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void UnlistedMachine_ServerNotSet_Refuses(string server)
        {
            var result = EnvironmentEvaluator.Evaluate("DEV-LAPTOP", ProductionHosts, server, "SANDBOX");

            Assert.That(result.IsRefused, Is.True);
            Assert.That(result.Refusal, Does.Contain("PROD_Server"));
        }
    }
}
