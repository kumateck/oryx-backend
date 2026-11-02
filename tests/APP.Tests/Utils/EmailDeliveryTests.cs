using System.Net;
using System.Net.Sockets;
using System.Text;
using MailKit;
using MimeKit;
using APP.Services.Email;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace APP.Tests.Utils;

public class EmailDeliveryTests
{
    [Fact]
    public void Connection_failure_preserves_the_transport_exception_and_stack()
    {
        using var client = new ConnectionProbe();
        var error = Record.Exception(() => Service(client).SendMail("Recipient", "to@example.invalid", "Reset", "body", []));
        Assert.Same(client.Failure, error);
        Assert.Contains(nameof(ConnectionProbe.Connect), error!.StackTrace);
    }

    [Theory]
    [InlineData(465, SecureSocketOptions.SslOnConnect)]
    [InlineData(587, SecureSocketOptions.StartTlsWhenAvailable)]
    public void Connection_uses_the_tls_mode_for_the_configured_port(int port, SecureSocketOptions expected)
    {
        using var client = new ConnectionProbe();
        Record.Exception(() => Service(client, port).SendMail("Recipient", "to@example.invalid", "Reset", "body", []));
        Assert.Equal(expected, client.Security);
    }

    [Fact]
    public void Connection_allows_operating_system_tls_negotiation_including_tls13()
    {
        using var client = new ConnectionProbe();
        Record.Exception(() => Service(client).SendMail("Recipient", "to@example.invalid", "Reset", "body", []));
        Assert.Equal(System.Security.Authentication.SslProtocols.None, client.SslProtocols);
    }

    [Fact]
    public void Connection_has_a_deadline_and_bounded_socket_waits()
    {
        using var client = new ConnectionProbe();
        Record.Exception(() => Service(client).SendMail("Recipient", "to@example.invalid", "Reset", "body", []));
        Assert.True(client.ConnectionToken.CanBeCanceled);
        Assert.InRange(client.Timeout, 1, 30_000);
    }

    [Fact]
    public void Failed_connections_dispose_the_smtp_client()
    {
        var client = new ConnectionProbe();
        Record.Exception(() => Service(client).SendMail("Recipient", "to@example.invalid", "Reset", "body", []));
        Assert.True(client.Disposed);
    }

    [Theory]
    [InlineData("authenticating")]
    [InlineData("sending")]
    public void Later_transport_failures_are_not_reported_as_success(string stage)
    {
        using var client = new ConnectionProbe(stage);
        var error = Record.Exception(() => Service(client).SendMail("Recipient", "to@example.invalid", "Reset", "body", []));
        Assert.Same(client.Failure, error);
        Assert.True(client.Disposed);
    }

    [Fact]
    public void Quit_failure_after_smtp_acceptance_does_not_report_a_failed_send()
    {
        using var client = new ConnectionProbe("disconnecting");
        var error = Record.Exception(() => Service(client).SendMail("Recipient", "to@example.invalid", "Reset", "body", []));
        Assert.Null(error);
        Assert.Equal(1, client.Accepted);
        Assert.True(client.Disposed);
    }

    private static EmailService Service(ConnectionProbe client, int port = 587) => new(
        NullLogger<EmailService>.Instance,
        () => new SmtpSettings("mail.example.invalid", port, "sender@example.invalid", "not-a-real-secret", "sender@example.invalid"),
        () => client);

    private sealed class ConnectionProbe(string failureStage = "connecting") : SmtpClient
    {
        internal SocketException Failure { get; } = new((int)SocketError.TimedOut);
        internal SecureSocketOptions Security { get; private set; }
        internal CancellationToken ConnectionToken { get; private set; }
        internal bool Disposed { get; private set; }
        internal int Accepted { get; private set; }

        public override void Connect(string host, int port = 0, SecureSocketOptions options = SecureSocketOptions.Auto,
            CancellationToken cancellationToken = default)
        {
            Security = options;
            ConnectionToken = cancellationToken;
            if (failureStage == "connecting") throw Failure;
        }

        public override void Authenticate(Encoding encoding, ICredentials credentials, CancellationToken cancellationToken = default)
        {
            if (failureStage == "authenticating") throw Failure;
        }

        public override string Send(FormatOptions options, MimeMessage message, CancellationToken cancellationToken = default,
            ITransferProgress? progress = null)
        {
            if (failureStage == "sending") throw Failure;
            Accepted++;
            return "250 queued";
        }

        public override void Disconnect(bool quit, CancellationToken cancellationToken = default)
        {
            if (failureStage == "disconnecting") throw Failure;
        }

        protected override void Dispose(bool disposing)
        {
            Disposed = true;
            base.Dispose(disposing);
        }
    }
}
