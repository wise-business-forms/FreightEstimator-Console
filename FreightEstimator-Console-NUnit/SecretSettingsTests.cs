using System.Collections.Generic;
using System.Linq;
using FreightEstApp35;
using NUnit.Framework;

namespace FreightEstimator_Console_NUnit
{
    public class SecretSettingsTests
    {
        static Dictionary<string, string> AllPresent()
        {
            return SecretSettings.RequiredKeys.ToDictionary(k => k, k => "value-for-" + k);
        }

        static string Lookup(Dictionary<string, string> settings, string key)
        {
            string value;
            return settings.TryGetValue(key, out value) ? value : null;
        }

        [Test]
        public void AllRequiredKeysSet_NothingMissing()
        {
            var settings = AllPresent();

            Assert.That(SecretSettings.FindMissing(k => Lookup(settings, k)), Is.Empty);
        }

        [Test]
        public void AKeyThatIsNotSet_IsReportedByName()
        {
            var settings = AllPresent();
            settings.Remove("UPS_ClientSecret");

            Assert.That(SecretSettings.FindMissing(k => Lookup(settings, k)), Is.EqualTo(new[] { "UPS_ClientSecret" }));
        }

        [TestCase("")]
        [TestCase("   ")]
        public void ABlankKey_CountsAsMissing(string blank)
        {
            var settings = AllPresent();
            settings["TI_ApiPassword"] = blank;

            Assert.That(SecretSettings.FindMissing(k => Lookup(settings, k)), Does.Contain("TI_ApiPassword"));
        }

        [Test]
        public void EveryMissingKey_IsReported_NotJustTheFirst()
        {
            var missing = SecretSettings.FindMissing(k => null);

            Assert.That(missing, Is.EquivalentTo(SecretSettings.RequiredKeys));
        }

        [Test]
        public void RequiredKeys_HaveNoDuplicatesAndNoDemoOnlyM33Token()
        {
            Assert.That(SecretSettings.RequiredKeys, Is.Unique);
            Assert.That(SecretSettings.RequiredKeys, Does.Not.Contain("M33_DemoToken"));
        }
    }
}
