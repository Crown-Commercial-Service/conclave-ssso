function (user, context, callback) {
    // TODO: implement your rule
    // 
    // 
    //if (context.protocol === 'oauth2-refresh-token') {
    // console.log("oauth2-refresh-token : context.sessionID");  
    //console.log(context.request.query.state);
    //  console.log(context.sessionID);
    // console.log(context.request);
    // console.log(context.refresh_token);
    //}
    if (context.protocol !== 'oidc-basic-profile') {
        return callback(null, user, context);
    }
    const namespace = 'https://identify.crowncommercial.gov.uk/';
    context.idToken[namespace + 'analytics-sessionID'] = context.sessionID;
    context.idToken[namespace + 'analytics-state'] = context.request.query.state;
    //console.log(context.clientName); 
    //console.log(context.sessionID);
    //console.log(context.state);
    //console.log(user.state);
    console.log(context.idToken);
    console.log(context.request.query.state);
    //console.log(context.refresh_token);
    //console.log(context.request);
    return callback(null, user, context);
}