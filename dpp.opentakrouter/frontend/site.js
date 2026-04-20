import "./site.css";
import "leaflet/dist/leaflet.css";
import L from "leaflet";
import ms from "milsymbol";
import QRCode from "qrcode";

const mapRoot = document.querySelector("[data-page='map']");
const dataPackagesRoot = document.querySelector("[data-page='datapackages']");

if (mapRoot) {
  const wsPort = mapRoot.dataset.wsPort;
  const state = {
    map: null,
    markers: new Map(),
    events: new Map(),
    selectedUid: null,
    ws: null,
    reconnectTimer: null,
  };

  const elements = {
    map: document.getElementById("map"),
    totalCount: document.querySelector("[data-role='total-count']"),
    sourceCount: document.querySelector("[data-role='source-count']"),
    selectedCallsign: document.querySelector("[data-role='selected-callsign']"),
    selectedType: document.querySelector("[data-role='selected-type']"),
    selectedSource: document.querySelector("[data-role='selected-source']"),
    selectedLatLon: document.querySelector("[data-role='selected-latlon']"),
    selectedUpdated: document.querySelector("[data-role='selected-updated']"),
    feed: document.querySelector("[data-role='event-feed']"),
    status: document.querySelector("[data-role='socket-status']"),
  };

  initMap();
  hydrate();
  connect();

  function initMap() {
    state.map = L.map(elements.map, {
      zoomControl: true,
      preferCanvas: true,
    }).setView([30, -30], 3);

    L.tileLayer("https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png", {
      attribution: "&copy; OpenStreetMap contributors",
      maxZoom: 18,
    }).addTo(state.map);
  }

  async function hydrate() {
    try {
      const response = await fetch("/api/ui/events", { headers: { Accept: "application/json" } });
      if (!response.ok) {
        throw new Error(`bootstrap failed: ${response.status}`);
      }

      const payload = await response.json();
      for (const evt of payload) {
        upsertEvent(evt);
      }
      renderFeed();
      renderStats();
    } catch (error) {
      console.error(error);
      setStatus("bootstrap failed", "degraded");
    }
  }

  function connect() {
    setStatus("connecting", "pending");
    const proto = window.location.protocol === "https:" ? "wss" : "ws";
    state.ws = new WebSocket(`${proto}://${window.location.hostname}:${wsPort}`);
    state.ws.addEventListener("open", () => setStatus("live", "online"));
    state.ws.addEventListener("message", (msg) => {
      try {
        upsertEvent(JSON.parse(msg.data));
        renderFeed();
        renderStats();
      } catch (error) {
        console.error(error);
      }
    });
    state.ws.addEventListener("close", () => {
      setStatus("reconnecting", "pending");
      state.ws = null;
      state.reconnectTimer = window.setTimeout(connect, 3000);
    });
  }

  function upsertEvent(evt) {
    if (!evt || !evt.uid) {
      return;
    }

    state.events.set(evt.uid, evt);
    const marker = state.markers.get(evt.uid);
    const icon = buildIcon(evt.type);
    const latLng = [evt.lat || 0, evt.lon || 0];
    const popup = buildPopup(evt);

    if (!marker) {
      const nextMarker = L.marker(latLng, { icon })
        .addTo(state.map)
        .bindPopup(popup)
        .on("click", () => selectEvent(evt.uid));
      state.markers.set(evt.uid, nextMarker);
    } else {
      marker.setLatLng(latLng);
      marker.setIcon(icon);
      marker.setPopupContent(popup);
    }

    if (!state.selectedUid) {
      state.selectedUid = evt.uid;
    }

    if (state.selectedUid === evt.uid) {
      selectEvent(evt.uid);
    }
  }

  function renderStats() {
    const events = Array.from(state.events.values());
    const sources = new Set(events.map((evt) => evt.source).filter(Boolean));
    elements.totalCount.textContent = `${events.length}`;
    elements.sourceCount.textContent = `${sources.size}`;
  }

  function renderFeed() {
    const entries = Array.from(state.events.values())
      .sort((left, right) => new Date(right.time || right.stale || 0) - new Date(left.time || left.stale || 0))
      .slice(0, 25);

    elements.feed.innerHTML = "";
    for (const evt of entries) {
      const item = document.createElement("button");
      item.type = "button";
      item.className = `otr-feed-item${evt.uid === state.selectedUid ? " is-active" : ""}`;
      item.addEventListener("click", () => selectEvent(evt.uid));
      item.innerHTML = `
        <span class="otr-feed-item__title">${escapeHtml(evt.callsign || evt.uid)}</span>
        <span class="otr-feed-item__meta">${escapeHtml(evt.type || "unknown")} · ${escapeHtml(evt.source || "unspecified")}</span>
      `;
      elements.feed.appendChild(item);
    }
  }

  function selectEvent(uid) {
    state.selectedUid = uid;
    const evt = state.events.get(uid);
    if (!evt) {
      return;
    }

    elements.selectedCallsign.textContent = evt.callsign || evt.uid || "Unknown";
    elements.selectedType.textContent = evt.type || "Unknown";
    elements.selectedSource.textContent = evt.source || "Unknown";
    elements.selectedLatLon.textContent = `${Number(evt.lat || 0).toFixed(5)}, ${Number(evt.lon || 0).toFixed(5)}`;
    elements.selectedUpdated.textContent = evt.time || evt.stale || "Unknown";

    const marker = state.markers.get(uid);
    if (marker) {
      marker.openPopup();
      state.map.panTo(marker.getLatLng(), { animate: true });
    }

    renderFeed();
  }

  function setStatus(text, tone) {
    elements.status.textContent = text;
    elements.status.dataset.tone = tone;
  }

  function buildPopup(evt) {
    return `<strong>${escapeHtml(evt.callsign || evt.uid || "Unknown")}</strong><br>${escapeHtml(evt.type || "")}<br>${escapeHtml(evt.source || "")}`;
  }

  function buildIcon(type) {
    const sidc = toSidc(type);
    const symbol = new ms.Symbol(sidc, { size: 26 });
    return L.icon({
      iconUrl: symbol.toDataURL(),
      iconAnchor: [symbol.getAnchor().x, symbol.getAnchor().y],
      popupAnchor: [0, -symbol.getAnchor().y],
      className: "otr-map-icon",
    });
  }

  function toSidc(type) {
    if (!type) {
      return "SUZP-----------";
    }

    const parts = type.toUpperCase().split("-");
    parts[0] = "S";
    parts.splice(3, 0, "P");
    return parts.join("").padEnd(12, "-");
  }

  function escapeHtml(value) {
    return String(value ?? "")
      .replaceAll("&", "&amp;")
      .replaceAll("<", "&lt;")
      .replaceAll(">", "&gt;");
  }
}

if (dataPackagesRoot) {
  const qrRoot = dataPackagesRoot.querySelector("[data-role='itak-qr-code']");
  const status = dataPackagesRoot.querySelector("[data-role='itak-qr-status']");

  hydrateItakQr();

  async function hydrateItakQr() {
    try {
      const response = await fetch("/Marti/api/provisioning/itakqr", {
        headers: { Accept: "application/json" },
      });
      if (!response.ok) {
        throw new Error(`qr failed: ${response.status}`);
      }

      const payload = await response.json();
      const svg = await QRCode.toString(payload.payload, {
        type: "svg",
        margin: 1,
        width: 192,
      });

      qrRoot.innerHTML = svg;
      status.textContent = payload.payload;
    } catch (error) {
      console.error(error);
      status.textContent = "QR unavailable. Set server.public_endpoint to generate iTAK quick connect.";
    }
  }
}
