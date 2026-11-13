using Xunit;
using APP.Services.Email;

namespace APP.Tests.Utils;

public class SmtpSettingsTests
{
    // Compose turns a setting missing from .env into an empty string, not an unset variable.
    [Fact]
    public void Blank_port_and_sender_fall_back_to_defaults()
    {
        var settings = SmtpSettings.Read(Env(new()
        {
            ["SMTP_HOST"] = "mail.example.com",
            ["SMTP_PORT"] = "",
            ["SMTP_USERNAME"] = "erp@example.com",
            ["SMTP_PASSWORD"] = "secret",
            ["SMTP_FROM"] = " "
        }));

        Assert.Equal(587, settings.Port);
        Assert.Equal("erp@example.com", settings.Sender);
    }

    [Fact]
    public void Missing_settings_are_named_in_the_error()
    {
        var error = Assert.Throws<InvalidOperationException>(() => SmtpSettings.Read(Env(new()
        {
            ["SMTP_HOST"] = "",
            ["SMTP_USERNAME"] = "erp@example.com",
            ["SMTP_PASSWORD"] = "secret"
        })));

        Assert.Contains("SMTP_HOST", error.Message);
        Assert.DoesNotContain("SMTP_PASSWORD", error.Message);
    }

    [Fact]
    public void Optional_origin_address_is_validated_and_keeps_the_tls_hostname()
    {
        var values = new Dictionary<string, string>
        {
            ["SMTP_HOST"] = "mail.example.com", ["SMTP_USERNAME"] = "erp@example.com",
            ["SMTP_PASSWORD"] = "secret", ["SMTP_CONNECT_ADDRESS"] = "192.0.2.10"
        };
        var settings = SmtpSettings.Read(Env(values));
        Assert.Equal("mail.example.com", settings.Host);
        Assert.Equal(System.Net.IPAddress.Parse("192.0.2.10"), settings.ConnectAddress);
        values["SMTP_CONNECT_ADDRESS"] = "http://mail.example.com";
        Assert.Throws<InvalidOperationException>(() => SmtpSettings.Read(Env(values)));
        values["SMTP_CONNECT_ADDRESS"] = "";
        Assert.Null(SmtpSettings.Read(Env(values)).ConnectAddress);
    }

    private static Func<string, string?> Env(Dictionary<string, string> values) =>
        name => values.GetValueOrDefault(name);

}
