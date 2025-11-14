window.initMatchPageMap = (lat, lng) => {
    window._matchPageLat = lat;
    window._matchPageLng = lng;

    const el = document.getElementById("matchPageMap");
    if (el) {
        window.initMapOn(el);
    } else {
        console.warn("matchPageMap não encontrado.");
    }
};

window.initMapOn = async (el) => {
    const fallback = [0, 0];

    if (el._leafletMap) {
        el._leafletMap.remove();
        el._leafletMap = null;
    }

    const map = L.map(el).setView(fallback, 6);
    el._leafletMap = map;

    map.setMaxBounds([[-90, -180], [90, 180]]);
    map.setMinZoom(2);

    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        noWrap: true,
        bounds: [[-90, -180], [90, 180]],
        attribution: '&copy; OpenStreetMap contributors'
    }).addTo(map);

    window._matchMap = map;

    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: '&copy; OpenStreetMap contributors'
    }).addTo(map);

    let marker;
    let centeredOnce = false;

    function setPosition(lat, lng, label = 'Location selected') {
        const latlng = [lat, lng];

        if (!marker) {
            marker = L.marker(latlng).addTo(map).bindPopup(label);
            window._matchMarker = marker; 
        } else {
            marker.setLatLng(latlng);
            marker.setPopupContent(label);
        }

        if (!centeredOnce) {
            centeredOnce = true;
            map.setView(latlng, 20);
        }
    }

    function distanceInKm(lat1, lon1, lat2, lon2) {
            const R = 6371; // earth radius in km
            const dLat = (lat2 - lat1) * Math.PI / 180;
            const dLon = (lon2 - lon1) * Math.PI / 180;

            const a =
                Math.sin(dLat / 2) * Math.sin(dLat / 2) +
                Math.cos(lat1 * Math.PI / 180) *
                Math.cos(lat2 * Math.PI / 180) *
                Math.sin(dLon / 2) * Math.sin(dLon / 2);

            const c = 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));

            return R * c; 
    }

    if (el.id === 'matchMap') {
        if ('geolocation' in navigator) {
            navigator.geolocation.getCurrentPosition(
                (pos) => {
                    const { latitude, longitude } = pos.coords;
                    setPosition(latitude, longitude, 'Your location');
                },
                async () => await fallbackToCapital(),
                { enableHighAccuracy: true, timeout: 8000, maximumAge: 30000 }
            );
        } else {
            await fallbackToCapital();
        }
        map.on('click', async (e) => {
            const { lat, lng } = e.latlng;
            setPosition(lat, lng, `Match location`);

            let city = "";
            let country = "";
            let address = "";

            try {
                const res = await fetch(`https://nominatim.openstreetmap.org/reverse?lat=${lat}&lon=${lng}&format=json&addressdetails=1&accept-language=en`);
                const data = await res.json();
                city = data.address.city || data.address.town || data.address.village || "";
                country = data.address.country || "";
                address = data.display_name || "";
            } catch (err) {
                console.warn("Reverse geocoding failed:", err);
            }

            if (window._matchComponent) {
                window._matchComponent.invokeMethodAsync("UpdateLocationFromMap", lat, lng, city, country, address);
            }
        });
    }

    if (el.id === 'matchPageMap') {
        const lat = window._matchPageLat;
        const lng = window._matchPageLng;

        const matchLatLng = [lat, lng];
        const matchMarker = L.marker(matchLatLng)
            .addTo(map)
            .bindPopup("Match location");

        window._matchMarker = matchMarker;

        map.setView(matchLatLng, 15);

        if ("geolocation" in navigator) {
            navigator.geolocation.getCurrentPosition(
                (pos) => {
                    const userLat = pos.coords.latitude;
                    const userLng = pos.coords.longitude;

                    const userLatLng = [userLat, userLng];

                    const userMarker = L.marker(userLatLng, { 
                        icon: L.icon({
                            iconUrl: "https://cdn-icons-png.flaticon.com/512/684/684908.png",
                            iconSize: [35, 35],
                            iconAnchor: [17, 34]
                        })
                    })
                    .addTo(map);

                    const distKm = distanceInKm(userLat, userLng, lat, lng);
                    const distText =
                        distKm < 1
                            ? `${Math.round(distKm * 1000)} m`
                            : `${distKm.toFixed(2)} km`;

                    userMarker.bindPopup(`You are here<br>Distance to match: <b>${distText}</b>`);

                    matchMarker.bindPopup(`Match location<br>Distance from you: <b>${distText}</b>`);

                    L.polyline([userLatLng, matchLatLng], {
                        color: "#405d13",
                        weight: 5,
                        opacity: 0.75
                    }).addTo(map);

                },
                (err) => {
                    console.warn("User location unavailable:", err);
                },
                { enableHighAccuracy: true, timeout: 8000, maximumAge: 30000 }
            );
        }
    }

    async function fallbackToCapital() {
        try {
            const locRes = await fetch('https://ipapi.co/json/');
            const locData = await locRes.json();
            const country = locData.country_name;
            
            if (country) {
                const capRes = await fetch(`https://restcountries.com/v3.1/name/${country}`);
                const capData = await capRes.json();
                const capital = capData[0]?.capital?.[0];
                const coords = capData[0]?.capitalInfo?.latlng;

                if (capital && coords) {
                    setPosition(coords[0], coords[1], `Capital: ${capital}`);
                    return;
                }
            }
        } catch (e) {
            console.warn('Erro ao obter capital:', e);
        }
        setPosition(fallback[0], fallback[1], 'Fallback');
    }

    setTimeout(() => map.invalidateSize(), 0);
};

window.registerMatchComponent = (dotnetObj) => {
    window._matchComponent = dotnetObj;
};


window.geocodeAddress = async function (address) {
    if (!address) return;

    const url = `https://nominatim.openstreetmap.org/search?format=json&addressdetails=1&accept-language=en&q=${encodeURIComponent(address)}`;

    try {
        const res = await fetch(url);
        const results = await res.json();
        if (!results.length) return;

        const result = results[0];
        const lat = parseFloat(result.lat);
        const lng = parseFloat(result.lon);

        window._matchMap?.setView([lat, lng], 20);
        window._matchMarker?.setLatLng([lat, lng]);

        const city = result.address?.city || result.address?.town || result.address?.village || "";
        const country = result.address?.country || "";

        if (window._matchComponent) {
            window._matchComponent.invokeMethodAsync("UpdateLocationFromMap", lat, lng, city, country, address);
        }
    } catch (e) {
        console.warn("Geocoding failed:", e);
    }
};


(function () {
    const ensureInit = (node) => {
        if (!node || node.nodeType !== 1) return;

        if (node.id === 'matchMap' && !node.dataset.leafletInit) {
            node.dataset.leafletInit = '1';
            if (!node.style.height) node.style.height = '400px';
            window.initMapOn(node);
        }
    };

    ensureInit(document.getElementById('matchMap'));

    const first = document.getElementById('matchMap');
    if (first) ensureInit(first);

    const obs = new MutationObserver((mutations) => {
        for (const m of mutations)
            for (const added of m.addedNodes)
                ensureInit(added);
    });

    obs.observe(document.body, { childList: true, subtree: true });
})();
