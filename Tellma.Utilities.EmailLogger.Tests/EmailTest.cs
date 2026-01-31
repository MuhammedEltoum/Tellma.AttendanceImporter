using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;

namespace Tellma.Utilities.EmailLogger.Tests
{
    public class EmailTest
    {
        private readonly EmailOptions _options;

        public EmailTest()
        {
            var builder = new ConfigurationBuilder()
                .AddUserSecrets<EmailTest>();
            var configuration = builder.Build();

            _options = new EmailOptions();
            configuration.GetSection("Email").Bind(_options);
        }

        [Fact(DisplayName = "Test Email Logger")]
        public void TestEmailLogger()
        {
            // Arrange
            // Create IOptions<EmailOptions> wrapper
            var optionsWrapper = Options.Create(_options);
            var emailLogger = new EmailLogger(optionsWrapper);
            var exception = new Exception($"Testing exception at {DateTime.Now:HH:mm:ss}");

            // Act
            emailLogger.Log(LogLevel.Error, new EventId(50000), "", exception,
                formatter: (s, e) => e?.Message ?? "");

            // Assert
            // Check your inbox :)
        }
    }
}