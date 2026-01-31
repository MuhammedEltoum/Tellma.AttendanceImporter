using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Tellma.AttendanceImporter.Tests
{
    public class TellmaImporterTest
    {
        readonly TellmaOptions _options;
        public TellmaImporterTest()
        {
            var builder = new ConfigurationBuilder()
                .AddUserSecrets<TellmaImporterTest>();
            var configuration = builder.Build();
            _options = new TellmaOptions();
            configuration.GetSection("Tellma").Bind(_options);
        }
        [Fact(DisplayName = "Test Tellma Service")]
        public async Task TestTellmaService()
        {
            // Arrange
            var importer = new TellmaAttendanceImporter(
                new MockDeviceServiceFactory(),
                new NullLogger<TellmaAttendanceImporter>(),
                new MockTellmaSerivce(),
                Options.Create(_options)
                );

            // Act
            await importer.ImportToTellma(CancellationToken.None);

            // Assert
        }
        
        // Add another test

    }
}