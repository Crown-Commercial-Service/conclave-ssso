-- Insert newly added contact point reasons. These two are already added to "20210225035731_InsertInitialStaticData.sql" file
-- This is only for progressive script execution. Will be removed once new script generated.

INSERT INTO public."ContactPointReason"("Name", "Description", "CreatedPartyId", "LastUpdatedPartyId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
  VALUES ('SITE','Organisation Site', 0, 0, now(), now(), false);

INSERT INTO public."ContactPointReason"("Name", "Description", "CreatedPartyId", "LastUpdatedPartyId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
  VALUES ('UNSPECIFIED','Unspecified', 0, 0, now(), now(), false);


