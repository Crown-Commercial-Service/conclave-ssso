function checkOrSetMFAForSocial(user, context, callback) {
    const axios = require('axios@0.22.0');

    let authMethods = [];
    if (context.authentication && Array.isArray(context.authentication.methods)) {
        authMethods = context.authentication.methods;
        console.log(context.authentication.methods);
    }

    const isFederatedLogin = !!authMethods.find((method) => method.name === 'federated');

    console.log('1. Social MFA > isFederatedLogin -', isFederatedLogin);

    if (!isFederatedLogin) {
        return callback(null, user, context);
    }

    console.log('2. Social MFA > isFederatedLogin - Inside mfa true condition');

    let loginAttempt = context.stats.loginsCount;
    console.log('3. Social MFA > context.stats.loginsCount -', loginAttempt);

    if (loginAttempt > 1) {
        console.log('4. Social MFA > login attempt more than 2 -');
        return callback(null, user, context);
    }

    user.user_metadata = user.user_metadata || {};

    if (loginAttempt === 1 || !user.user_metadata.use_mfa) {
        console.log('4. Social MFA > first login or user mfa undefined');

        console.log('5. Social MFA > No meta data-', user.user_metadata);
        const options = {
            method: 'GET',
            url: `${configuration.EXT_API_URL}/user-profiles?user-id=${user.email}`,
            headers: { 'content-type': 'application/json', 'x-api-key': `${configuration.EXT_API_KEY}` }
        };

        axios(options)
            .then(res => {
                console.log('6. Social MFA > API call return data -', res.data);

                const result = res.data;
                user.user_metadata.use_mfa = result.mfaEnabled ? result.mfaEnabled : false;

                auth0.users.updateUserMetadata(user.user_id, user.user_metadata)
                    .then(() => {
                        console.log('6. Social MFA > meta data update success -', user.user_metadata);

                        if (user.user_metadata.use_mfa) {
                            context.multifactor = {
                                provider: 'any',
                                allowRememberBrowser: false
                            };
                            console.log('7. Social MFA > Inside use_mfa true');
                            return callback(null, user, context);
                        }


                    })
                    .catch(err => {
                        console.log('7. Social MFA > meta data update has failed -', err);

                        callback(err);
                    });

            })
            .catch(err => {
                console.log('8. Social MFA > API call failed  -', err);

                console.log("ERROR");
                console.log(err);
                callback(err);
                // return callback(new UnauthorizedError('NOT ALLOWED02'));
            });
    }

}