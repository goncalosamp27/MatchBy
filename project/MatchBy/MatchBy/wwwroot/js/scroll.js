window.scrollToBottom = (element) => {
    if (element) {
        element.scrollTop = element.scrollHeight;
    }
};

window.scrollToTop = (element) => {
    if (element) {
        element.scrollTop = 0;
    }
};

window.getScrollPosition = (element) => {
    if (element) {
        return {
            scrollTop: element.scrollTop,
            scrollHeight: element.scrollHeight,
            clientHeight: element.clientHeight
        };
    }
    return null;
};