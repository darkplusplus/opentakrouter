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
- the API certificate as the trust bundle source
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

Use them as starting points for:
- the preferred `server.links` TAK transport/federation model
- `postgres`-backed container deployments
- inbound/outbound routing policy definitions

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

If `certManager.enabled=true`, the chart will create a `Certificate` resource and mount the resulting secret at `/certs`.

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
