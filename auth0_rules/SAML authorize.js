function samlAuthorization(user, context, callback) {
    const axios = require('axios@0.22.0');

    if (context.protocol !== 'samlp') {
        console.log("NOT_SAML");
        return callback(null, user, context);
    }

    console.log("SAML");
    console.log(user.email);
    console.log(configuration.TRAINLINE_DIGITS_CLIENT_ID);
    // Set Trainline Custom Attribue p2sprint9
    if (context.clientID === configuration.TRAINLINE_DIGITS_CLIENT_ID) {
        user.corpref = "10009655";
        console.log(user.corpref);
    }

    const options = {
        method: 'GET',
        url: `${configuration.SEC_API_URL}/security/users/saml?email=${user.email}&client-id=${context.clientID}`,
        headers: { 'content-type': 'application/json', 'x-api-key': `${configuration.SEC_API_KEY}` }
    };

    axios(options)
        .then(res => {
            const result = res.data;
            if (result.isAccessible === true) {
                console.log("TRUE");
                return callback(null, user, context);
            }
            else {
                console.log("FALSE");
                return callback(
                    new UnauthorizedError('NOT ALLOWED#01')
                );
            }
        })
        .catch(err => {
            console.log("ERROR");
            console.log(err);
            return callback(
                new UnauthorizedError('NOT ALLOWED02')
            );
        });
}