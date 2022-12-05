function mfaresetVerified(user, context, callback) {
    if (user.user_metadata && user.user_metadata.mfa_reset_verified === false) {
        return callback(
            new UnauthorizedError('PENDING_MFA_VERIFICATION')
        );
    } else {
        return callback(null, user, context);
    }
}