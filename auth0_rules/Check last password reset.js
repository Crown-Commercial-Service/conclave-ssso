function checkLastPasswordReset(user, context, callback) {
    function daydiff(first, second) {
        return (second - first) / (1000 * 60 * 60 * 24);
    }

    function daydiffInMinutes(lastChangedDate, today) {
        var diffMs = (today - lastChangedDate);
        var diffMins = Math.round(((diffMs % 86400000) % 3600000) / 60000);
        return diffMins;
    }

    const last_password_change = user.last_password_reset || user.created_at;
    let dateDiff = daydiffInMinutes(new Date(last_password_change), new Date());
    console.log(dateDiff);
    if (dateDiff > 1) {
        return callback(new UnauthorizedError('CHANGE_PASSWORD'));
    }
    callback(null, user, context);
}