function addAttributes(user, context, callback) {
    context.idToken['https://ccs-sso/connection'] = context.connection;
    if (user.user_metadata && user.user_metadata.use_mfa === true) {
        context.idToken['https://ccs-sso/use_mfa'] = true;
    }
    callback(null, user, context);
}