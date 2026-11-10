# Capture formula draft 500 logs on a customer server

This read-only helper captures the backend API container's logs for one
reproduction of a formula draft-save failure. It does not read or print
environment variables, credentials, formula contents or database records.

From the backend release directory, run:

```sh
./deploy/diagnose-formula-draft-500.sh
```

When prompted, reproduce the draft save once, wait for the 500, then press
Enter in the terminal. The script writes a timestamped log under
`~/.local/state/oryx-backend/diagnostics/`, with directory mode `0700` and
file mode `0600`. If the current user cannot access Docker, use the server's
approved sudo procedure; running it as root stores the capture under root's
state directory. Set `ORYX_BACKEND_API_CONTAINER` if the API container has a
different name.

The helper prints matching exception and trace lines but retains the restricted
full capture. Compare the trace ID with the failed HTTP response if available.
If no relevant lines appear, preserve the capture and check the server's
configured container logging destination with its administrator. Treat the
capture as customer data and share it only through the approved private support
channel.
