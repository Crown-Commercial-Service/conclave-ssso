
-- ========================== Salesforce =============================================
INSERT INTO public."ConclaveEntity"(
	"Id", "Name", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
	VALUES (9, 'Salesforce', '2021-07-02 10:30:10.852362', '2021-07-02 10:30:10.852362', false);

INSERT INTO public."ConclaveEntityAttribute"(
"Id", "AttributeName", "ConclaveEntityId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
VALUES (85, 'All', 9, '2021-07-02 10:30:10.852362', '2021-07-02 10:30:10.852362', false);

INSERT INTO public."ConclaveEntityAttribute"(
"Id", "AttributeName", "ConclaveEntityId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
VALUES (86, 'Urn', 9, '2021-07-02 10:30:10.852362', '2021-07-02 10:30:10.852362', false);

INSERT INTO public."ConclaveEntityAttribute"(
"Id", "AttributeName", "ConclaveEntityId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
VALUES (87, 'Id', 9, '2021-07-02 10:30:10.852362', '2021-07-02 10:30:10.852362', false);
