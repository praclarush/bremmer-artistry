using Npgsql;
using PotteryJournal.Infrastructure.Services;

namespace PotteryJournal.Infrastructure.Tests.Services
{
    [TestFixture]
    public class ConnectionStringNormalizerTests
    {
        private const string KEY_VALUE_CONNECTION_STRING = "Host=db;Port=5432;Database=potteryjournal;Username=potteryjournal;Password=secret";

        [Test]
        public void Normalize_KeyValueConnectionString_ReturnsItUnchanged()
        {
            string? result = ConnectionStringNormalizer.Normalize(KEY_VALUE_CONNECTION_STRING);

            Assert.That(result, Is.EqualTo(KEY_VALUE_CONNECTION_STRING));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void Normalize_NullOrBlank_ReturnsItUnchanged(string? connectionString)
        {
            string? result = ConnectionStringNormalizer.Normalize(connectionString);

            Assert.That(result, Is.EqualTo(connectionString));
        }

        [TestCase("postgresql")]
        [TestCase("postgres")]
        public void Normalize_PostgresUri_ConvertsToKeyValueConnectionString(string scheme)
        {
            string result = ConnectionStringNormalizer.Normalize($"{scheme}://potteryjournal:secret@dpg-abc123-a:5433/potteryjournal")!;

            NpgsqlConnectionStringBuilder builder = new NpgsqlConnectionStringBuilder(result);
            Assert.That(builder.Host, Is.EqualTo("dpg-abc123-a"));
            Assert.That(builder.Port, Is.EqualTo(5433));
            Assert.That(builder.Database, Is.EqualTo("potteryjournal"));
            Assert.That(builder.Username, Is.EqualTo("potteryjournal"));
            Assert.That(builder.Password, Is.EqualTo("secret"));
        }

        [Test]
        public void Normalize_PostgresUriWithoutPort_UsesDefaultPostgresPort()
        {
            string result = ConnectionStringNormalizer.Normalize("postgresql://potteryjournal:secret@dpg-abc123-a/potteryjournal")!;

            NpgsqlConnectionStringBuilder builder = new NpgsqlConnectionStringBuilder(result);
            Assert.That(builder.Port, Is.EqualTo(5432));
        }

        [Test]
        public void Normalize_PostgresUriWithEscapedPassword_DecodesIt()
        {
            string result = ConnectionStringNormalizer.Normalize("postgresql://potteryjournal:p%40ss%2Fword%3A1@dpg-abc123-a:5432/potteryjournal")!;

            NpgsqlConnectionStringBuilder builder = new NpgsqlConnectionStringBuilder(result);
            Assert.That(builder.Password, Is.EqualTo("p@ss/word:1"));
        }
    }
}
