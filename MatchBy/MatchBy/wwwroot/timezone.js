window.timeZone = () => {
    var timezone = Intl.DateTimeFormat().resolvedOptions().timeZone;
    return timezone;
};