function emailVerified(user, context, callback) {
    if (!user.email_verified) {
        return callback(
            new UnauthorizedError('PENDING_PASSWORD_CHANGE')
        );
    } else {
        return callback(null, user, context);
    }
}