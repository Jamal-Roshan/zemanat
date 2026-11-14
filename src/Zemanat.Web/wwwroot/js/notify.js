// Desktop notifications for the signed-in user. Browsers allow them only on https or localhost.
window.zemanatNotify = {
    status: function () {
        return ('Notification' in window) ? Notification.permission : 'unsupported';
    },
    ask: async function () {
        if (!('Notification' in window)) return 'unsupported';
        return await Notification.requestPermission();
    },
    show: function (title, body, url) {
        if (!('Notification' in window) || Notification.permission !== 'granted') return;
        var n = new Notification(title, { body: body, dir: 'rtl', lang: 'fa' });
        n.onclick = function () {
            window.focus();
            if (url) { window.location.href = url; }
            n.close();
        };
    }
};
