
-- ========================== Change Salesforce info to CII identifier info=============================================

UPDATE public."ConclaveEntity"
	SET  "Name"='CiiOrgIdentifiers', "CreatedOnUtc"=now(), "LastUpdatedOnUtc"=now(), "IsDeleted"=false
	WHERE "Id"=9;

UPDATE public."ConclaveEntityAttribute"
	SET "AttributeName"='Identifier', "ConclaveEntityId"=9, "CreatedOnUtc"=now(), "LastUpdatedOnUtc"=now(), "IsDeleted"=false
	WHERE "Id"=85;

UPDATE public."ConclaveEntityAttribute"
	SET "AttributeName"='AdditionalIdentifiers', "ConclaveEntityId"=9, "CreatedOnUtc"=now(), "LastUpdatedOnUtc"=now(), "IsDeleted"=false
	WHERE "Id"=86;

DELETE FROM public."ConclaveEntityAttribute"
	WHERE "Id"=87;
