window.getCurrentPosition = function () {
    return new Promise((resolve, reject) => {
        if (!('geolocation' in navigator)) {
            reject({ message: 'Geolocation not supported' });
            return;
        }

        navigator.geolocation.getCurrentPosition(
            (position) => {
                resolve({
                    latitude: position.coords.latitude,
                    longitude: position.coords.longitude
                });
            },
            (error) => {
                reject({ message: error.message });
            },
            {
                enableHighAccuracy: true,
                timeout: 8000,
                maximumAge: 30000
            }
        );
    });
};