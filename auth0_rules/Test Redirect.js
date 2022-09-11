function multifactorAuthentication(user, context, callback) {
    if (user.user_metadata && user.user_metadata.use_mfa === true) {
        context.multifactor = {
            // required
            provider: 'any',

            // optional, defaults to true. Set to false to force Guardian authentication every time.
            // See https://auth0.com/docs/multifactor-authentication/custom#change-the-frequency-of-authentication-requests for details
            allowRememberBrowser: true
        };
        context.redirect = {
            url: "http://localhost:4200/authsuccess"
        };
        //}
    }
    return callback(null, user, context);
}