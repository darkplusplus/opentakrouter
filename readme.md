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
5. Browse to http://localhost:8080 to see the admin pages.
6. Connect your EUD to your host on port `58087`.

## Frontend Development

The operator UI now uses a small npm-managed bundle instead of LibMan/AdminLTE/jQuery.

For UI changes:

1. Run `npm install`
2. Run `npm run build:ui`
3. Start or rebuild `opentakrouter`

The compiled browser assets are emitted to `dpp.opentakrouter/wwwroot/assets/`.

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
