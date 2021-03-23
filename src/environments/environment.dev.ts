export const environment = {
  production: false,
  idam_client_id:'',
  uri: {
    api: {
      security: 'https://dev-api-security.london.cloudapps.digital',
      postgres: 'https://dev-api-core.london.cloudapps.digital',
      cii: 'https://conclave-cii-integration-brash-shark-mk.london.cloudapps.digital',
      wrapper: 'https://dev-api-wrapper.london.cloudapps.digital'
    },
    web: {
      dashboard: 'https://dev-ccs-sso.london.cloudapps.digital'
    }
  },
  wrapperApiKey: "",
  securityApiKey: '',
  usedPasswordThreshold: 5, //This value should be changed when Auth0 password history policy changed,
  userNamePasswordIdentityProviderConnectionName: "Username-Password-Authentication",
};
