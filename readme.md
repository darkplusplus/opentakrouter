# OpenTakRouter [![ci](https://github.com/darkplusplus/opentakrouter/actions/workflows/ci.yml/badge.svg)](https://github.com/darkplusplus/opentakrouter/actions/workflows/ci.yml)

An opensource router of cursor-on-target messages with support for [ATAK](https://github.com/deptofdefense/AndroidTacticalAssaultKit-CIV).

## Features

- Cross platform emphasizing ease of use.
- Support for both TCP and TLS server modes.
- Datapackages server.
- Live operator map with websocket-fed track updates.
- Basic federation capabilities.
- More to come!

You can track our current roadmap here: https://github.com/darkplusplus/opentakrouter/projects/1

## Quickstart

1. Go grab the latest release zip or tarball.
2. Unarchive to your directory of choice.
3. Review `opentakrouter.json` to see the default configuration. 
4. Run `opentakrouter`.
5. Browse to https://localhost:8443 to see the admin pages.
6. Connect your EUD to your host on port `8089`.
7. Set `server.public_endpoint` before using the generated server package download.

Default compatibility-oriented ports now assume:
- `8443` for the HTTPS admin/API surface
- `8089` for the TAK TLS listener

If you use the default config, provide a certificate at `/certs/tls.crt` and `/certs/tls.key`, or adjust the config to your own paths.

## Enrollment Package

`opentakrouter` can now generate a soft-cert TAK server package from the admin surface at `/datapackages` or directly from `/Marti/api/provisioning/serverpackage`.

It uses:
- `server.public_endpoint` for the client-facing hostname
- the active TAK TLS listener port, preferring `server.links`
- `server.provisioning.trustStoreCertificate` as the preferred trust bundle source, falling back to the API certificate when not set
- `server.provisioning.clientCertificate` for the operator client identity bundle

The generated ZIP keeps the ATAK/WinTAK mission-package layout, includes a client certificate for soft-cert onboarding, and also includes root-level manifest and preference files for iTAK-oriented import flows.

The data packages page also exposes an iTAK quick-connect QR code. That QR path is only suitable when the device already trusts the server certificate chain; the ZIP package remains the safer path for self-signed or private CA deployments.

Set `server.public_endpoint` explicitly in container or Kubernetes deployments. Falling back to an internal hostname will produce a bad package for EUD onboarding.

## Frontend Development

The operator UI now uses a small npm-managed bundle instead of LibMan/AdminLTE/jQuery.

For UI changes:

1. Run `npm install`
2. Run `npm run build:ui`
3. Start or rebuild `opentakrouter`

The compiled browser assets are emitted to `dpp.opentakrouter/wwwroot/assets/`.

## Example Configuration

Example configs live under `examples/`:

- `examples/opentakrouter.links.example.json`
- `examples/opentakrouter.postgres.example.json`
- `examples/opentakrouter.routing.example.json`
- `examples/opentakrouter.json`
- `examples/README.md`

Use them as starting points for:
- the preferred `server.links` TAK transport/federation model
- `postgres`-backed container deployments
- inbound/outbound routing policy definitions
- local laptop testing with generated cert material

## TLS And Key Material

Operator runbook:

- [docs/certificate-runbook.md](/Users/gbuehler/src/github.com/darkplusplus/opentakrouter/docs/certificate-runbook.md)

There are two distinct certificate roles in `opentakrouter`:

1. Server identity for the router itself
2. Provisioning material for EUD onboarding packages

### Server identity

The HTTPS API and the TAK TLS listener both need a server certificate and private key:

- `server.api.cert`
- `server.api.key`
- `server.links[*].cert`
- `server.links[*].key`

In the common case, the API and the TAK TLS listener can reuse the same server certificate and key pair.

That server certificate must:
- chain to a CA your clients trust
- include the configured `server.public_endpoint` in the certificate SAN
- match the hostname or IP that EUDs actually use in the generated connect string

For example, if `server.public_endpoint` is `tak.example.com`, the server certificate should include `DNS:tak.example.com`.

### Provisioning package material

The generated server package uses separate provisioning inputs:

- `server.provisioning.publicApiScheme`
- `server.provisioning.publicApiPort`
- `server.provisioning.trustStoreCertificate`
- `server.provisioning.trustStorePassword`
- `server.provisioning.clientCertificate`
- `server.provisioning.clientCertificatePassword`

These are not the same thing as the server key pair:

- `publicApiScheme` / `publicApiPort` control where the package download URL points
- `trustStoreCertificate` should be the CA certificate or CA PKCS#12 that clients should trust
- `clientCertificate` should be the PKCS#12 client identity bundle to import onto the EUD

For a public CA deployment such as Let's Encrypt:

- the server/API/TAK listener certificate can be a public CA-issued leaf certificate
- the provisioning trust bundle should still be set deliberately for the enrollment package
- the provisioning client certificate must still be supplied as a separate `.p12`

In other words, Let's Encrypt solves the public server identity problem. It does not create the client identity bundle that this package-based EUD onboarding flow needs.

If your API is published through ingress or another proxy, set `server.provisioning.publicApiScheme` and `server.provisioning.publicApiPort` to the externally reachable values. That keeps generated package URLs aligned with the public entrypoint even when the app itself listens on a different internal port.

Recommended shape:

- server cert/key:
  - leaf server certificate signed by your CA
- trust store:
  - CA certificate exported as `.crt`, `.p12`, or `.pfx`
- client certificate:
  - client identity exported as `.p12`

If you need exact generation and Kubernetes publishing commands, use the runbook above. It includes:

- OpenSSL examples for public-CA and private-CA deployments
- `kubectl create secret` examples matching the Helm chart
- a Helm values example for ingress-backed deployments

For local testing, a simple private CA works well:

- create a local CA
- sign the OTR server certificate with that CA
- sign the EUD client certificate with that CA
- point `trustStoreCertificate` at the CA cert
- point `clientCertificate` at the client `.p12`

`examples/README.md` includes a concrete mapping of config fields to the files you need.

## Storage

`opentakrouter` now supports provider-backed persistence through `server.storage`:

- `sqlite` for simple local deployments
- `postgres` for containerized/stateless deployments

Example:

```json
{
  "server": {
    "storage": {
      "provider": "postgres",
      "postgres": {
        "connectionString": "Host=postgres;Port=5432;Database=opentakrouter;Username=opentakrouter;Password=change-me"
      }
    }
  }
}
```

## Helm

A basic Helm chart is included under `helm/opentakrouter/`.

It assumes:
- container deployment
- externalized configuration
- `postgres` as the preferred persistent store
- TLS material mounted from a Kubernetes secret

The chart now treats TLS material and cert-manager ownership separately:

- `tls.secretName` selects the Kubernetes secret mounted at `/certs`
- `tls.certFile` / `tls.keyFile` select the filenames within that secret
- `certManager.enabled=true` creates a `Certificate` resource that writes into that same TLS secret unless `certManager.secretName` overrides it

Provisioning material is mounted through a projected volume and can come from separate secrets:

- `provisioning.trustStore.secretName` / `provisioning.trustStore.secretKey`
- `provisioning.clientCertificate.secretName` / `provisioning.clientCertificate.secretKey`
- `provisioning.secretName` remains as a shared-secret fallback when both artifacts live in one Kubernetes secret
- `provisioning.publicApiScheme` / `provisioning.publicApiPort` can override the generated package download URL
- when ingress is enabled, the chart defaults that download URL toward the ingress-facing scheme and port

The chart default for the provisioning trust artifact is now `trust.p12`. That aligns with the recommended production shape where the enrollment trust bundle is a prebuilt PKCS#12 instead of a single PEM certificate.

The chart can also generate those provisioning secrets when explicitly enabled:

- `provisioning.trustStore.create=true` with base64 content in `provisioning.trustStore.data`
- `provisioning.clientCertificate.create=true` with base64 content in `provisioning.clientCertificate.data`

That in-chart secret creation is useful for bootstrap and local testing. For production, separate secret management is still the better default: `ExternalSecret`, Sealed Secrets, SOPS, or pre-created Kubernetes secrets keep binary material and rotations out of normal Helm values flow.

For trust bundles, the most reliable way to ship a complete intermediate/root chain is to provide `server.provisioning.trustStoreCertificate` as a prebuilt `.p12`/`.pfx` bundle. `opentakrouter` passes PKCS#12 trust bundles through unchanged into the generated package.

Certificate passphrases should not be rendered into `values.yaml` or the generated ConfigMap. Use secret-backed refs instead:

- `tls.passphraseSecretName` / `tls.passphraseSecretKey`
- `provisioning.trustStorePasswordSecretName` / `provisioning.trustStorePasswordSecretKey`
- `provisioning.clientCertificatePasswordSecretName` / `provisioning.clientCertificatePasswordSecretKey`

For backward compatibility, the chart also still honors literal `provisioning.trustStorePassword` and `provisioning.clientCertificatePassword` values when secret refs are not configured. Secret refs take precedence.

## Operator UI Direction

The current browser UI is intended to become an operator console, not a generic admin dashboard.

Near-term operator concerns:
- full-screen map with live normalized CoT updates
- selected-track inspector with source/type/position freshness
- recent-feed visibility for debugging routing behavior
- lightweight views for clients and data packages

Future operator-console considerations:
- live link and federation status
- per-rule allow/deny hit counters
- source/type/callsign filter chips
- selected track history and trail rendering
- mission/data package workflow pane
- alerts for stale links, dropped traffic, and abnormal routing patterns


## Want to run this on AWS?

We have a shortcut to get you online quickly at https://github.com/darkplusplus/opentakrouter-ops.
