-- Insert Initial party type and virtual address type and contact point reason data

INSERT INTO public."PartyType"("PartyTypeName", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
	VALUES ('INTERNAL_ORGANISATION', 0, 0, now(), now(), false);

INSERT INTO public."PartyType"("PartyTypeName", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
	VALUES ('EXTERNAL_ORGANISATION', 0, 0, now(), now(), false);

INSERT INTO public."PartyType"("PartyTypeName", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
	VALUES ('USER', 0, 0, now(), now(), false);

INSERT INTO public."PartyType"("PartyTypeName", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
	VALUES ('NON_USER', 0, 0, now(), now(), false);


INSERT INTO public."VirtualAddressType"("Name", "Description", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
	VALUES ('EMAIL','Email', 0, 0, now(), now(), false);

INSERT INTO public."VirtualAddressType"("Name", "Description", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
	VALUES ('WEB_ADDRESS','Web Address', 0, 0, now(), now(), false);

INSERT INTO public."VirtualAddressType"("Name", "Description", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
	VALUES ('PHONE','Phone', 0, 0, now(), now(), false);

INSERT INTO public."VirtualAddressType"("Name", "Description", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
	VALUES ('FAX','Fax', 0, 0, now(), now(), false);

INSERT INTO public."VirtualAddressType"("Name", "Description", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
	VALUES ('MOBILE','Mobile', 0, 0, now(), now(), false);



INSERT INTO public."ContactPointReason"("Name", "Description", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
  VALUES ('GENERAL','General', 0, 0, now(), now(), false);

INSERT INTO public."ContactPointReason"("Name", "Description", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
  VALUES ('PERSONAL','Personal', 0, 0, now(), now(), false);

INSERT INTO public."ContactPointReason"("Name", "Description", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
	VALUES ('OTHER','Other reason', 0, 0, now(), now(), false);

INSERT INTO public."ContactPointReason"("Name", "Description", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
	VALUES ('SHIPPING','Shipping', 0, 0, now(), now(), false);

INSERT INTO public."ContactPointReason"("Name", "Description", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
  VALUES ('BILLING','Billing', 0, 0, now(), now(), false);

INSERT INTO public."ContactPointReason"("Name", "Description", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
  VALUES ('MAIN_OFFICE','Main Office', 0, 0, now(), now(), false);

INSERT INTO public."ContactPointReason"("Name", "Description", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
  VALUES ('HEAD_QUARTERS','Head quarters', 0, 0, now(), now(), false);

INSERT INTO public."ContactPointReason"("Name", "Description", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
  VALUES ('MANUFACTURING','Manufacturing', 0, 0, now(), now(), false);

INSERT INTO public."ContactPointReason"("Name", "Description", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
  VALUES ('BRANCH','Branch', 0, 0, now(), now(), false);

INSERT INTO public."ContactPointReason"("Name", "Description", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
  VALUES ('SITE','Organisation Site', 0, 0, now(), now(), false);

INSERT INTO public."ContactPointReason"("Name", "Description", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
  VALUES ('UNSPECIFIED','Unspecified', 0, 0, now(), now(), false);


