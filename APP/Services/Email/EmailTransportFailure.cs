using System.Net.Sockets;

namespace APP.Services.Email;

internal static class EmailTransportFailure
{
    internal static bool Is(Exception error) => error is IOException or SocketException or TimeoutException
        or OperationCanceledException or MailKit.CommandException or MailKit.ProtocolException
        or MailKit.Security.AuthenticationException or System.Security.Authentication.AuthenticationException;
}
