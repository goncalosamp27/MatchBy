window.initMap = async () => {
    const fallback = [0, 0];
    const map = L.map('map').setView(fallback, 6);

    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: '&copy; OpenStreetMap contributors'
    }).addTo(map);

    let marker;
    let centeredOnce = false;

    function setPosition(lat, lng, label = 'You are here!') {
        const latlng = [lat, lng];
        if (!marker) {
            marker = L.marker(latlng).addTo(map).bindPopup(label);
        } else {
            marker.setLatLng(latlng);
            marker.setPopupContent(label);
        }
        if (!centeredOnce) {
            centeredOnce = true;
            map.setView(latlng, 10);
        }
    }

    if ('geolocation' in navigator) {
        navigator.geolocation.getCurrentPosition(
            (pos) => {
                const { latitude, longitude } = pos.coords;
                setPosition(latitude, longitude);
            },
            async (err) => {
                console.warn('Geolocalização falhou:', err.message);
                await fallbackToCapital();
            },
            { enableHighAccuracy: true, timeout: 8000, maximumAge: 30000 }
        );
    } 
    else {
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
                    console.log(`Fallback para ${capital}, ${country}`);
                    setPosition(coords[0], coords[1], `Capital: ${capital}`);
                    return;
                }
            }
        } catch (e) {
            console.warn('Erro ao obter capital:', e);
        }

        setPosition(fallback[0], fallback[1], 'fallback)');
    }
};

document.addEventListener('DOMContentLoaded', () => {
    if (document.getElementById('map')) {
        window.initMap();
    }
});
