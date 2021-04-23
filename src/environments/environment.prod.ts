export const environment = {
  production: true,
  idam_client_id:'',
  uri: {
    api: {
      security: 'https://ccs-sso-api-agile-ratel-ix.london.cloudapps.digital',
      postgres: 'https://api-org-22jan-proud-crane-wu.london.cloudapps.digital',
      cii: 'https://conclave-cii-testing-talkative-oryx-hh.london.cloudapps.digital',
      wrapper: ''
    },
    web: {
      dashboard: 'http://localhost:4200'
    }
  },
  securityApiKey:'',
  usedPasswordThreshold: 5, //This value should be changed when Auth0 password history policy changed,
  listPageSize: 10
};
