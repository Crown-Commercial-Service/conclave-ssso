-- set clientId
INSERT INTO public."RelyingParty"(
	"Name", "ClientId", "BackChannelLogoutUrl", "CreatedByUserId", "LastUpdatedByUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
	VALUES ('Evidence Locker', '', null, 0, 0, now(), now(), false);
