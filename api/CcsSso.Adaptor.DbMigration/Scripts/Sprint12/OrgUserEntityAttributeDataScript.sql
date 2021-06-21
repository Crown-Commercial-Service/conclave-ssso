-- This is also included in ConclaveEntityAttributeDataScript.sql file

-- ================ Entities =======================================

INSERT INTO public."ConclaveEntity"(
	"Id", "Name", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
	VALUES (8, 'OrgUser', '2021-05-06 17:10:10.852362', '2021-05-06 17:10:10.852362', false);



-- ========================== Organisation User =============================================

INSERT INTO public."ConclaveEntityAttribute"(
"Id", "AttributeName", "ConclaveEntityId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
VALUES (82, 'Users', 8, '2021-05-06 17:10:10.852362', '2021-05-06 17:10:10.852362', false);

INSERT INTO public."ConclaveEntityAttribute"(
"Id", "AttributeName", "ConclaveEntityId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
VALUES (83, 'Name', 8, '2021-05-06 17:10:10.852362', '2021-05-06 17:10:10.852362', false);

INSERT INTO public."ConclaveEntityAttribute"(
"Id", "AttributeName", "ConclaveEntityId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
VALUES (84, 'UserName', 8, '2021-05-06 17:10:10.852362', '2021-05-06 17:10:10.852362', false);
