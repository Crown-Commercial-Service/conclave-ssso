export const environment = {
  production: false,
  idam_client_id:'',
  uri: {
    api: {
      security: 'https://sand-api-security.london.cloudapps.digital',
      postgres: 'https://sand-api-core.london.cloudapps.digital',
      cii: 'https://conclave-cii-testing-talkative-oryx-hh.london.cloudapps.digital',
      wrapper: 'https://sand-api-wrapper.london.cloudapps.digital'
    },
    web: {
      dashboard: 'https://sand-ccs-sso.london.cloudapps.digital'
    }
  },
  wrapperApiKey: "",
  securityApiKey:'',
  usedPasswordThreshold: 5, //This value should be changed when Auth0 password history policy changed,
  userNamePasswordIdentityProviderConnectionName: "Username-Password-Authentication",
};
