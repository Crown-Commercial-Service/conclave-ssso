-- Insert virtual address type (Mobile) and contact point reason (Person)
INSERT INTO public."VirtualAddressType"("Name", "Description", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
	VALUES ('MOBILE','Mobile', 0, 0, now(), now(), false);

INSERT INTO public."ContactPointReason"("Name", "Description", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
  VALUES ('GENERAL','General', 0, 0, now(), now(), false);

INSERT INTO public."ContactPointReason"("Name", "Description", "CreatedUserId", "LastUpdatedUserId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
  VALUES ('PERSONAL','Personal', 0, 0, now(), now(), false);
