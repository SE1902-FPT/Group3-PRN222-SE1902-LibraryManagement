(function () {
    const unreadNodes = Array.from(document.querySelectorAll("[data-unread-count]"));

    function getUnreadCount() {
        if (unreadNodes.length === 0) {
            return 0;
        }

        const currentValue = Number.parseInt(unreadNodes[0].textContent || "0", 10);
        return Number.isNaN(currentValue) ? 0 : currentValue;
    }

    function setUnreadCount(nextValue) {
        const safeValue = Math.max(0, nextValue);
        unreadNodes.forEach(function (node) {
            node.textContent = String(safeValue);
        });
    }

    window.parentNotifications = {
        getUnreadCount: getUnreadCount,
        setUnreadCount: setUnreadCount
    };
})();
