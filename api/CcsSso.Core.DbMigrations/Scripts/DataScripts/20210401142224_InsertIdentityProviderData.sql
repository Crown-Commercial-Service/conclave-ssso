-- Create Initial IdentityProviderData
INSERT INTO public."IdentityProvider"(
			"IdpConnectionName", "IdpUri", "IdpName", "ExternalIdpFlag", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
			VALUES ('Username-Password-Authentication', 'auth0', 'User ID and password', false, 0, 0, now(), now(), false);

INSERT INTO public."IdentityProvider"(
			"IdpConnectionName", "IdpUri", "IdpName", "ExternalIdpFlag", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
			VALUES ('facebook', 'https://www.facebook.com/', 'Facebook', true, 0, 0, now(), now(), false);

INSERT INTO public."IdentityProvider"(
			"IdpConnectionName", "IdpUri", "IdpName", "ExternalIdpFlag", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
			VALUES ('google-oauth2', 'https://accounts.google.com', 'Google', true, 0, 0, now(), now(), false);

INSERT INTO public."IdentityProvider"(
			"IdpConnectionName", "IdpUri", "IdpName", "ExternalIdpFlag", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
			VALUES ('windowslive', 'https://account.microsoft.com', 'Microsoft 365', true, 0, 0, now(), now(), false);

INSERT INTO public."IdentityProvider"(
			"IdpConnectionName", "IdpUri", "IdpName", "ExternalIdpFlag", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
			VALUES ('linkedin', 'https://lk.linkedin.com', 'LinkedIn', true, 0, 0, now(), now(), false);

INSERT INTO public."IdentityProvider"(
			"IdpConnectionName", "IdpUri", "IdpName", "ExternalIdpFlag", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
			VALUES ('none', 'none', 'None', false, 0, 0, now(), now(), false);
