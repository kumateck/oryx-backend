# Email timeout review — 2026-11-02

## Evidence and limits

The two Entrance error reports at 08:47:47 GMT have the same request trace
`89cef46c2158f28f4bde4d1802ff299a`: one failed `POST /api/v1/auth/forgot-password`,
logged by `EmailService` and then by `ExceptionHandlingMiddleware`. They are not
independent delivery attempts. The request took 120,260 ms.

Reviewed `APP/Services/Email/EmailService.cs` and `APP/Repository/AuthRepository.cs`
at reported commit `cf031ac36411e9509e54be047baa7a2307051818`, plus refreshed
`origin/main` at `3638e466`. Neither file was fixed in the intervening commits.
The local `main` branch was stale, so the review also checked fetched `origin/main`.

The shared sender performed synchronous Connect/Authenticate/Send/Disconnect
without a cancellation deadline, retained MailKit 4.16.0's two-minute socket timeout,
and replaced failures with a generic Exception without an inner exception. That
explains the long failed request and loss of transport diagnostics. Password reset
saved its key before sending and did not revoke it when sending failed.

The report has only the generic message `Connection timed out`; it cannot establish
whether the failure occurred in TCP connection, greeting/TLS, authentication,
message submission, or shutdown. A firewall, provider/network outage, wrong address
or a TLS/port mismatch remain possible. Missing configuration is a separate case
already handled by `Email.NotConfigured`. A 535 authentication rejection is not
proof of a connection timeout.

The old `Connect(host, port, false)` selected STARTTLS when available even for 465.
An implicit-TLS endpoint on 465 waits for a TLS client hello while this client waits
for a plaintext SMTP greeting. This is a concrete code defect, but it is not the
production trigger: the operator subsequently confirmed
`SMTP_HOST=entrancepharmaceuticals.com` and `SMTP_PORT=587` in the running API
container. Port 587 matches the existing STARTTLS negotiation mode.

The operator's container DNS probe resolved the host to `66.29.149.67`. A verified
STARTTLS probe succeeded with TLS 1.3, the expected hostname/certificate and exit 0.
A subsequent probe forced to TLS 1.2 exited 124 after 15 seconds without successful
handshake output. The application hardcoded `SslProtocols.Tls12`, excluding the TLS
1.3 protocol that worked in the probe. This is a strong lead for the timeout, not
proof of a permanent TLS-1.2 server restriction: a single timeout can occur before
TLS negotiation because of transient network/server conditions. Paired repeat
probes with `-state` and an authorized application attempt are needed to localize
any remaining production failure. Neither probe tested SMTP authentication.

An operator-triggered password-reset request failed again at
`2026-11-02T14:51:40.546Z`, trace `b21e8ebed7fa742166718fe6b702cb0a`.
The new trace differs from the morning incident, confirming a recurrence.
The `EmailService.cs:56` generic throw and `AuthRepository.cs:110` caller match
the unchanged production sender, not the locally prepared fix. The new log still
omits the original SMTP exception/stage, so it does not localize the network stall.
Deployment and one authorized successful password-reset send remain outstanding.

The fix removes the hardcoded protocol pin and retains MailKit's documented
OS-managed TLS defaults, allowing TLS 1.3 without disabling certificate validation.
This change was added after the initial prepared fix; the first test run explicitly
reproduced the TLS pin regression before removing it.

References: [MailKit 4.16.0 source](https://github.com/jstedfast/MailKit/blob/4.16.0/MailKit/Net/Smtp/SmtpClient.cs),
[network timeout](https://mimekit.net/docs/html/P_MailKit_Net_Smtp_SmtpClient_Timeout.htm),
[connection security and cancellation](https://mimekit.net/docs/html/M_MailKit_Net_Smtp_SmtpClient_Connect_2.htm),
[OS-managed TLS protocol defaults](https://mimekit.net/docs/html/P_MailKit_MailService_SslProtocols.htm).

## Changes

- SMTP Connect has a 30-second cancellation deadline, including connection/greeting/
  TLS work. Socket reads/writes use a 30-second inactivity timeout. This is not a
  30-second deadline for an entire message: an actively progressing attachment may
  take longer. No customer latency measurements were available for tuning this policy.
- Port 465 uses `SslOnConnect`. Other ports retain `StartTlsWhenAvailable`.
  MailKit's OS-managed TLS defaults replace the TLS-1.2-only pin; certificate
  validation remains enabled. No obsolete protocol is explicitly enabled.
- The client and message/attachment streams are disposed on success and failure.
- Error logs retain the original exception and identify the SMTP stage, host, port
  and exception type. No protocol transcript, password, reset URL or email body is
  added to logs. The existing recipient success log remains.
- After SMTP accepts a message, a transport failure during QUIT is a warning;
  it cannot falsely report an unsuccessful send and provoke a duplicate.
- Forgot-password maps expected SMTP/socket/authentication failures to
  `Email.Unavailable` (HTTP 503). It soft-deletes only the newly created reset key
  and keeps the audit row and previously delivered keys. Programming/database
  failures are not masked as SMTP outages. No automatic SMTP retry is added.

SMTP acceptance is not proof of inbox delivery. A timeout during submission can
have an ambiguous outcome; the failed reset key is revoked even if a mail server
ultimately delivers that attempt. The user should request a new link after SMTP
connectivity is restored.

## Other callers reviewed

The same shared sender is used by procurement documents, vendor quotations,
employee welcome/setup/invitations and notification delivery, so they receive the
connection/security/resource fixes too. Procurement callers checked send success
before updating sent/workflow state; their business transitions are unchanged.

The notification outbox already retries failed email delivery independently of
in-app notifications. Its cancellation token is checked before synchronous email
work but is not propagated through every recipient send. A large recipient batch
can therefore outlive the outbox lease/deadline; this remains separate follow-up
work. SMTP delivery remains at least once.

Employee welcome/setup and service-proforma paths intentionally catch some email
failures after business records are committed. They can leave a valid business
record without a delivered email; do not replay those business actions merely to
resend mail. A delivery-status/resend workflow is outside this focused fix.

## Customer terminal diagnosis (read-only; sends no email)

Print only non-secret configuration:

```sh
docker exec oryx_api sh -c 'printf "SMTP_HOST=%s\nSMTP_PORT=%s\n" "$SMTP_HOST" "$SMTP_PORT"'
```

Check DNS/TCP reachability from the actual API container network, without logging
credentials or changing installed packages:

```sh
docker exec oryx_api sh -c 'getent hosts "$SMTP_HOST"'
docker exec oryx_api sh -c 'command -v timeout; command -v openssl; command -v nc'
```

If `timeout` and `openssl` are available in the API container, check the configured
TLS endpoint below. This does not authenticate or submit a message. For another
SMTP port requiring a different policy, confirm that policy with the provider first.

```sh
docker exec oryx_api sh -c '
  smtp_port=${SMTP_PORT:-587}
  if [ "$smtp_port" = 465 ]; then
    timeout 15 openssl s_client -brief -verify_return_error -verify_hostname "$SMTP_HOST" \
      -connect "$SMTP_HOST:$smtp_port" -servername "$SMTP_HOST" </dev/null
  else
    timeout 15 openssl s_client -brief -verify_return_error -verify_hostname "$SMTP_HOST" \
      -starttls smtp -connect "$SMTP_HOST:$smtp_port" -servername "$SMTP_HOST" </dev/null
  fi
'
```

A missing diagnostic tool is not evidence of an SMTP outage. A successful host-only
probe is not proof the API container can connect. Do not disable TLS validation or
print the container's full environment. Check provider credentials only after
TCP/TLS connectivity works; no credential changes are justified by this report alone.

Follow logs while an authorized user requests a reset:

```sh
docker logs --follow --since 30m --timestamps oryx_api 2>&1 \
  | grep --line-buffered -iE 'email|smtp|mailkit|notification.*(failed|attempt|unavailable)'
```

## Adjacent alert retry warnings

The pasted log also shows two `AlertRequestDispatcher` records at attempt 20 with
`InvalidOperationException`. This dispatcher resolves alert configuration and
recipients before notification delivery; it is distinct from the direct
forgot-password SMTP call. Its explicit invalid-operation branches include missing
alert configuration and no eligible recipients, but the warning records only the
exception type, so the exact cause cannot be established from this output.
Do not attribute these warnings to the SMTP timeout or clear their retry records
without investigating their configured alert type/audience. No alert records were
changed as part of this email fix.

## Data and deployment

No schema migration or customer database repair is required. Failed historical
requests may have left active password-reset rows; existing links expire after two
days, and a successful password reset invalidates the underlying Identity token.
Do not bulk-delete reset records or change passwords. If incident-specific revocation
is needed, correlate the user and exact failure time first and use a reviewed,
auditable operation; no production data was changed during this work.

Deploy through the existing reviewed backend release process, then verify SMTP
reachability and a successful authorized password reset on the customer server.
This code change cannot repair network/firewall/provider availability.

## Verification

The regression tests inject transport failures without contacting any customer
SMTP server or sending email. Before the fix, six assertions failed across the
socket/IO timeout, security-mode, cancellation, disposal and exception-preservation
cases; the existing configured/unconfigured paths stayed covered. The focused suite
also covers failed authentication, successful delivery, post-acceptance QUIT failure
and unrelated programming errors. All 40 selected email, SMTP configuration,
notification delivery/state, purchase-order send and password-administration tests
passed with `dotnet test tests/APP.Tests/APP.Tests.csproj --no-restore` and a focused
`--filter`; this also compiled the backend API and test projects. Existing repository
warnings remain. `git diff --check` passed. The full test suite was not run. Customer DNS and non-authenticating OpenSSL
STARTTLS probes were supplied by the operator as described above; a live
application password-reset send after deployment has not been verified.
