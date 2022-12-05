function guardianMultifactor(user, context, callback) {
    //const CLIENTS_WITH_MFA = ['REPLACE_WITH_YOUR_CLIENT_ID'];

    //console.log("MFA log 1");
    //console.log(user);

    if (context.protocol === 'oauth2-refresh-token')
        return callback(null, user, context);

    //Avoid prompting a user for multifactor authentication if they have successfully completed MFA in their current session
    let authMethods = [];
    if (context.authentication && Array.isArray(context.authentication.methods)) {
        authMethods = context.authentication.methods;
        console.log("MFA log2  context.authentication.methods");
        console.log(context.authentication.methods);
    }

    let findMFA = authMethods.find((method) => method.name === 'mfa');
    console.log("Finding authMethods for mfa", findMFA);
    console.log("Finding authMethods for !!mfa", !!findMFA);


    const completedMfa = !!authMethods.find((method) => method.name === 'mfa');
    console.log("MFA log3  ccompletedMfa");
    console.log(completedMfa);
    if (completedMfa) {
        return callback(null, user, context);
    }

    let socialLogin = !!user.identities.find((identity) => identity.provider === "google-oauth2");
    console.log('user.user_metadata-', user.user_metadata);

    // run only for the specified clients
    //if (CLIENTS_WITH_MFA.indexOf(context.clientID) !== -1) {

    // uncomment the following if clause in case you want to request a second factor only from user's that have user_metadata.use_mfa === true
    if (user.user_metadata && user.user_metadata.use_mfa === true) {
        console.log('Inside mfa true condition');
        context.multifactor = {
            // required
            provider: 'any',

            // optional, defaults to true. Set to false to force Guardian authentication every time.
            // See https://auth0.com/docs/multifactor-authentication/custom#change-the-frequency-of-authentication-requests for details
            allowRememberBrowser: false
        };
        //}
    }

    callback(null, user, context);
}