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

    if (el.id === 'matchMap') {
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

    const obs = new MutationObserver((muts) => {
        for (const m of muts)
            for (const n of m.addedNodes)
                ensureInit(n);
    });

    obs.observe(document.body, { childList: true, subtree: true });
})();
