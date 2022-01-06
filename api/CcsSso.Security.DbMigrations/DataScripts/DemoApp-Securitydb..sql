-- set clientId and backChannelLogoutUrl
INSERT INTO public."RelyingParty"(
	"Name", "ClientId", "BackChannelLogoutUrl", "CreatedByUserId", "LastUpdatedByUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
	VALUES ('Demo App', '', '', 0, 0, now(), now(), false);
