window.copyToClipboard = (text) => {
    navigator.clipboard.writeText(text).then(function () {
    }).catch(function (err) {
        console.error('Error copying to clipboard: ', err);
    });
};
