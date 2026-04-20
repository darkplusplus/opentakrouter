# Example Config Notes

The example configs in this directory assume four distinct certificate artifacts.

## Required Files

### 1. Server certificate

Used by:
- `server.api.cert`
- `server.links[*].cert`

Typical file:
- `/certs/tls.crt`

This should be the leaf certificate presented by `opentakrouter` for:
- the HTTPS admin/API listener on `8443`
- the TAK TLS listener on `8089`

### 2. Server private key

Used by:
- `server.api.key`
- `server.links[*].key`

Typical file:
- `/certs/tls.key`

This is the private key matching the server certificate.

### 3. Trust store certificate

Used by:
- `server.provisioning.trustStoreCertificate`

Typical file:
- `/certs/ca.crt`

This should be the CA certificate that signed the OTR server certificate, not the server leaf cert itself.

When `opentakrouter` generates a provisioning ZIP, it packages this CA material as the trust store ATAK/iTAK import.

### 4. Client certificate bundle

Used by:
- `server.provisioning.clientCertificate`

Typical file:
- `/certs/client.p12`

This is the PKCS#12 client identity bundle imported onto the EUD for soft-cert connection.

Its password is configured with:
- `server.provisioning.clientCertificatePassword`

## Field Mapping

### `examples/opentakrouter.json`

Local laptop-oriented example:

- `server.public_endpoint`
  - must match the hostname or IP the EUD actually uses
- `server.api.cert`
  - `/tmp/opentakrouter.crt`
- `server.api.key`
  - `/tmp/opentakrouter.key`
- `server.provisioning.trustStoreCertificate`
  - `/tmp/opentakrouter-ca.crt`
- `server.provisioning.clientCertificate`
  - `/tmp/opentakrouter-client.p12`

### `examples/opentakrouter.links.example.json`

Container/Kubernetes-oriented example:

- `server.api.cert`
  - `/certs/tls.crt`
- `server.api.key`
  - `/certs/tls.key`
- `server.provisioning.trustStoreCertificate`
  - `/certs/ca.crt`
- `server.provisioning.clientCertificate`
  - `/certs/client.p12`

## Important Constraints

- `server.public_endpoint` must match the hostname or IP clients use.
- The server certificate SAN must include that same hostname or IP.
- The trust store must contain the CA that signed the server certificate.
- The client certificate bundle must be a `.p12` file.
- The generated ATAK/iTAK package will not fix a bad hostname, SAN mismatch, or wrong CA.

## Local Test Shape

For a local CA-backed setup, generate:

1. a CA key/cert
2. a server key/cert signed by that CA
3. a client key/cert signed by that CA
4. a client `.p12` bundle

Then configure:

- `trustStoreCertificate` -> CA cert
- `clientCertificate` -> client `.p12`
- `server.api.cert` / `server.links[*].cert` -> server cert
- `server.api.key` / `server.links[*].key` -> server key
