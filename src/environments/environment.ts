export const environment = {
  production: false,
  idam_client_id:'',
  uri: {
    api: {
      //security: 'https://localhost:44352',
      security: 'https://dev-api-security.london.cloudapps.digital',
      // postgres: 'https://localhost:44330',
      postgres: 'https://dev-api-core.london.cloudapps.digital',
      // wrapper: 'https://localhost:44309'
      wrapper: 'https://dev-api-wrapper.london.cloudapps.digital'
    },
    web: {
      dashboard: 'http://localhost:4200'
    }
  },
  wrapperApiKey: '',
  securityApiKey: '',
  usedPasswordThreshold: 5, //This value should be changed when Auth0 password history policy changed
  userNamePasswordIdentityProviderConnectionName: "Username-Password-Authentication",
};
