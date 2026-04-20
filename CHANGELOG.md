# Changelog

## 2.0.0

This release refreshes `opentakrouter` around a normalized CoT routing model, modern TAK transport handling, and a rebuilt operator UI.

### Added

- normalized `CotMessageEnvelope`-based routing flow across TAK listeners, federation peers, API ingress, and UI updates
- inbound and outbound routing policy engine with source, destination, type, and bounding-box rules
- destination-aware fanout instead of simple broadcast-to-all behavior
- protobuf-capable TAK transport negotiation and shared TAK stream protocol handling
- internal operator UI JSON feed and `/api/ui/events`
- operator-console map UI built from a small npm-managed bundle using `leaflet` and `milsymbol`
- normalized TAK link model through `server.links`
- SQLite WAL enablement on startup
- router flow tests covering outbound fanout and richer route policy matching

### Changed

- upgraded `dpp.cot` integration to `2.0.0`
- moved the app runtime to `.NET 10`
- refreshed Docker and publish scripts to match the current runtime and RIDs
- split Swagger into internal and MARTI compatibility surfaces
- modernized the web UI away from `LibMan`, `jQuery`, and `AdminLTE`
- updated the map page to a richer operator layout with recent feed, selected track, and connection state
- updated MARTI compatibility endpoints to return more realistic client and datapackage behavior

### Fixed

- `TakService.StopAsync()` stopping the wrong websocket servers
- repository upserts that failed to copy updated values into persisted entities
- websocket browser path still pretending to be TAK transport instead of a UI feed
- stale container/runtime definitions still pinned to `.NET 5`

### Config Notes

- `server.links` is now the preferred TAK transport/federation configuration model
- legacy `server.tak.*` and `server.peers` settings are still read as fallback
- websocket settings remain separate because they serve the internal operator UI, not TAK transport

### Upgrade Notes

- rebuild frontend assets with `npm install` and `npm run build:ui` when modifying the operator UI
- if you previously relied on `LibMan`, that path has been removed
- if you are shipping containers, use the updated `.NET 10` Dockerfile rather than the old `.NET 5` flow
