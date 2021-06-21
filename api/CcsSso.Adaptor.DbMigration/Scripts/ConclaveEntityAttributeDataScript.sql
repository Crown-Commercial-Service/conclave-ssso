-- ================ Entities =======================================
INSERT INTO public."ConclaveEntity"(
	"Id", "Name", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
	VALUES (1, 'OrgProfile', '2021-05-06 17:10:10.852362', '2021-05-06 17:10:10.852362', false);

INSERT INTO public."ConclaveEntity"(
	"Id", "Name", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
	VALUES (2, 'UserProfile', '2021-05-06 17:10:10.852362', '2021-05-06 17:10:10.852362', false);
	
INSERT INTO public."ConclaveEntity"(
	"Id", "Name", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
	VALUES (3, 'SiteProfile', '2021-05-06 17:10:10.852362', '2021-05-06 17:10:10.852362', false);
	
INSERT INTO public."ConclaveEntity"(
	"Id", "Name", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
	VALUES (4, 'OrgContact', '2021-05-06 17:10:10.852362', '2021-05-06 17:10:10.852362', false);
	
INSERT INTO public."ConclaveEntity"(
	"Id", "Name", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
	VALUES (5, 'UserContact', '2021-05-06 17:10:10.852362', '2021-05-06 17:10:10.852362', false);
	
INSERT INTO public."ConclaveEntity"(
	"Id", "Name", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
	VALUES (6, 'SiteContact', '2021-05-06 17:10:10.852362', '2021-05-06 17:10:10.852362', false);

INSERT INTO public."ConclaveEntity"(
	"Id", "Name", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
	VALUES (7, 'Contact', '2021-05-06 17:10:10.852362', '2021-05-06 17:10:10.852362', false);

INSERT INTO public."ConclaveEntity"(
	"Id", "Name", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
	VALUES (8, 'OrgUser', '2021-05-06 17:10:10.852362', '2021-05-06 17:10:10.852362', false);

-- ===================== Organisation =============================
INSERT INTO public."ConclaveEntityAttribute"(
"Id", "AttributeName", "ConclaveEntityId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
VALUES (1, 'Identifier', 1, '2021-05-06 17:10:10.852362', '2021-05-06 17:10:10.852362', false);

INSERT INTO public."ConclaveEntityAttribute"(
"Id", "AttributeName", "ConclaveEntityId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
VALUES (2, 'Identifier.Id', 1, '2021-05-06 17:10:10.852362', '2021-05-06 17:10:10.852362', false);

INSERT INTO public."ConclaveEntityAttribute"(
"Id", "AttributeName", "ConclaveEntityId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
VALUES (3, 'Identifier.LegalName', 1, '2021-05-06 17:10:10.852362', '2021-05-06 17:10:10.852362', false);

INSERT INTO public."ConclaveEntityAttribute"(
"Id", "AttributeName", "ConclaveEntityId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
VALUES (4, 'Identifier.Uri', 1, '2021-05-06 17:10:10.852362', '2021-05-06 17:10:10.852362', false);

INSERT INTO public."ConclaveEntityAttribute"(
"Id", "AttributeName", "ConclaveEntityId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
VALUES (5, 'Identifier.Scheme', 1, '2021-05-06 17:10:10.852362', '2021-05-06 17:10:10.852362', false);

INSERT INTO public."ConclaveEntityAttribute"(
"Id", "AttributeName", "ConclaveEntityId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
VALUES (6, 'AdditionalIdentifiers', 1, '2021-05-06 17:10:10.852362', '2021-05-06 17:10:10.852362', false);

INSERT INTO public."ConclaveEntityAttribute"(
"Id", "AttributeName", "ConclaveEntityId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
VALUES (7, 'Address', 1, '2021-05-06 17:10:10.852362', '2021-05-06 17:10:10.852362', false);

INSERT INTO public."ConclaveEntityAttribute"(
"Id", "AttributeName", "ConclaveEntityId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
VALUES (8, 'Address.StreetAddress', 1, '2021-05-06 17:10:10.852362', '2021-05-06 17:10:10.852362', false);
	
INSERT INTO public."ConclaveEntityAttribute"(
"Id", "AttributeName", "ConclaveEntityId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
VALUES (9, 'Address.Locality', 1, '2021-05-06 17:10:10.852362', '2021-05-06 17:10:10.852362', false);

INSERT INTO public."ConclaveEntityAttribute"(
"Id", "AttributeName", "ConclaveEntityId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
VALUES (10, 'Address.Region', 1, '2021-05-06 17:10:10.852362', '2021-05-06 17:10:10.852362', false);

INSERT INTO public."ConclaveEntityAttribute"(
"Id", "AttributeName", "ConclaveEntityId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
VALUES (11, 'Address.PostalCode', 1, '2021-05-06 17:10:10.852362', '2021-05-06 17:10:10.852362', false);

INSERT INTO public."ConclaveEntityAttribute"(
"Id", "AttributeName", "ConclaveEntityId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
VALUES (12, 'Address.CountryCode', 1, '2021-05-06 17:10:10.852362', '2021-05-06 17:10:10.852362', false);

INSERT INTO public."ConclaveEntityAttribute"(
"Id", "AttributeName", "ConclaveEntityId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
VALUES (13, 'Detail', 1, '2021-05-06 17:10:10.852362', '2021-05-06 17:10:10.852362', false);

INSERT INTO public."ConclaveEntityAttribute"(
"Id", "AttributeName", "ConclaveEntityId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
VALUES (14, 'Detail.OrganisationId', 1, '2021-05-06 17:10:10.852362', '2021-05-06 17:10:10.852362', false);

INSERT INTO public."ConclaveEntityAttribute"(
"Id", "AttributeName", "ConclaveEntityId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
VALUES (15, 'Detail.CreationDate', 1, '2021-05-06 17:10:10.852362', '2021-05-06 17:10:10.852362', false);

INSERT INTO public."ConclaveEntityAttribute"(
"Id", "AttributeName", "ConclaveEntityId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
VALUES (16, 'Detail.CompanyType', 1, '2021-05-06 17:10:10.852362', '2021-05-06 17:10:10.852362', false);

INSERT INTO public."ConclaveEntityAttribute"(
"Id", "AttributeName", "ConclaveEntityId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
VALUES (17, 'Detail.SupplierBuyerType', 1, '2021-05-06 17:10:10.852362', '2021-05-06 17:10:10.852362', false);

INSERT INTO public."ConclaveEntityAttribute"(
"Id", "AttributeName", "ConclaveEntityId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
VALUES (18, 'Detail.IsSme', 1, '2021-05-06 17:10:10.852362', '2021-05-06 17:10:10.852362', false);

INSERT INTO public."ConclaveEntityAttribute"(
"Id", "AttributeName", "ConclaveEntityId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
VALUES (19, 'Detail.IsVcse', 1, '2021-05-06 17:10:10.852362', '2021-05-06 17:10:10.852362', false);

INSERT INTO public."ConclaveEntityAttribute"(
"Id", "AttributeName", "ConclaveEntityId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
VALUES (20, 'Detail.RightToBuy', 1, '2021-05-06 17:10:10.852362', '2021-05-06 17:10:10.852362', false);

INSERT INTO public."ConclaveEntityAttribute"(
"Id", "AttributeName", "ConclaveEntityId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
VALUES (21, 'Detail.IsActive', 1, '2021-05-06 17:10:10.852362', '2021-05-06 17:10:10.852362', false);

-- ===============================User==============================================

INSERT INTO public."ConclaveEntityAttribute"(
"Id", "AttributeName", "ConclaveEntityId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
VALUES (22, 'UserName', 2, '2021-05-06 17:10:10.852362', '2021-05-06 17:10:10.852362', false);

INSERT INTO public."ConclaveEntityAttribute"(
"Id", "AttributeName", "ConclaveEntityId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
VALUES (23, 'OrganisationId', 2, '2021-05-06 17:10:10.852362', '2021-05-06 17:10:10.852362', false);

INSERT INTO public."ConclaveEntityAttribute"(
"Id", "AttributeName", "ConclaveEntityId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
VALUES (24, 'FirstName', 2, '2021-05-06 17:10:10.852362', '2021-05-06 17:10:10.852362', false);

INSERT INTO public."ConclaveEntityAttribute"(
"Id", "AttributeName", "ConclaveEntityId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
VALUES (25, 'LastName', 2, '2021-05-06 17:10:10.852362', '2021-05-06 17:10:10.852362', false);

INSERT INTO public."ConclaveEntityAttribute"(
"Id", "AttributeName", "ConclaveEntityId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
VALUES (26, 'Title', 2, '2021-05-06 17:10:10.852362', '2021-05-06 17:10:10.852362', false);

INSERT INTO public."ConclaveEntityAttribute"(
"Id", "AttributeName", "ConclaveEntityId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
VALUES (27, 'Detail', 2, '2021-05-06 17:10:10.852362', '2021-05-06 17:10:10.852362', false);

INSERT INTO public."ConclaveEntityAttribute"(
"Id", "AttributeName", "ConclaveEntityId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
VALUES (28, 'Detail.Id', 2, '2021-05-06 17:10:10.852362', '2021-05-06 17:10:10.852362', false);

INSERT INTO public."ConclaveEntityAttribute"(
"Id", "AttributeName", "ConclaveEntityId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
VALUES (29, 'Detail.IdentityProviderId', 2, '2021-05-06 17:10:10.852362', '2021-05-06 17:10:10.852362', false);

INSERT INTO public."ConclaveEntityAttribute"(
"Id", "AttributeName", "ConclaveEntityId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
VALUES (30, 'Detail.IdentityProviderDisplayName', 2, '2021-05-06 17:10:10.852362', '2021-05-06 17:10:10.852362', false);

INSERT INTO public."ConclaveEntityAttribute"(
"Id", "AttributeName", "ConclaveEntityId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
VALUES (31, 'Detail.CanChangePassword', 2, '2021-05-06 17:10:10.852362', '2021-05-06 17:10:10.852362', false);

INSERT INTO public."ConclaveEntityAttribute"(
"Id", "AttributeName", "ConclaveEntityId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
VALUES (32, 'Detail.RolePermissionInfo', 2, '2021-05-06 17:10:10.852362', '2021-05-06 17:10:10.852362', false);

INSERT INTO public."ConclaveEntityAttribute"(
"Id", "AttributeName", "ConclaveEntityId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
VALUES (33, 'Detail.RolePermissionInfo.RoleId', 2, '2021-05-06 17:10:10.852362', '2021-05-06 17:10:10.852362', false);

INSERT INTO public."ConclaveEntityAttribute"(
"Id", "AttributeName", "ConclaveEntityId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
VALUES (34, 'Detail.RolePermissionInfo.RoleName', 2, '2021-05-06 17:10:10.852362', '2021-05-06 17:10:10.852362', false);

INSERT INTO public."ConclaveEntityAttribute"(
"Id", "AttributeName", "ConclaveEntityId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
VALUES (35, 'Detail.RolePermissionInfo.RoleKey', 2, '2021-05-06 17:10:10.852362', '2021-05-06 17:10:10.852362', false);

INSERT INTO public."ConclaveEntityAttribute"(
"Id", "AttributeName", "ConclaveEntityId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
VALUES (36, 'Detail.UserGroups', 2, '2021-05-06 17:10:10.852362', '2021-05-06 17:10:10.852362', false);

INSERT INTO public."ConclaveEntityAttribute"(
"Id", "AttributeName", "ConclaveEntityId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
VALUES (37, 'Detail.UserGroups.GroupId', 2, '2021-05-06 17:10:10.852362', '2021-05-06 17:10:10.852362', false);

INSERT INTO public."ConclaveEntityAttribute"(
"Id", "AttributeName", "ConclaveEntityId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
VALUES (38, 'Detail.UserGroups.Group', 2, '2021-05-06 17:10:10.852362', '2021-05-06 17:10:10.852362', false);

INSERT INTO public."ConclaveEntityAttribute"(
"Id", "AttributeName", "ConclaveEntityId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
VALUES (39, 'Detail.UserGroups.AccessRole', 2, '2021-05-06 17:10:10.852362', '2021-05-06 17:10:10.852362', false);

INSERT INTO public."ConclaveEntityAttribute"(
"Id", "AttributeName", "ConclaveEntityId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
VALUES (40, 'Detail.UserGroups.AccessRoleName', 2, '2021-05-06 17:10:10.852362', '2021-05-06 17:10:10.852362', false);

-- ============================== Site =============================================
INSERT INTO public."ConclaveEntityAttribute"(
"Id", "AttributeName", "ConclaveEntityId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
VALUES (41, 'Sites', 3, '2021-05-06 17:10:10.852362', '2021-05-06 17:10:10.852362', false);

INSERT INTO public."ConclaveEntityAttribute"(
"Id", "AttributeName", "ConclaveEntityId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
VALUES (42, 'Sites.Details', 3, '2021-05-06 17:10:10.852362', '2021-05-06 17:10:10.852362', false);

INSERT INTO public."ConclaveEntityAttribute"(
"Id", "AttributeName", "ConclaveEntityId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
VALUES (43, 'Sites.Details.SiteId', 3, '2021-05-06 17:10:10.852362', '2021-05-06 17:10:10.852362', false);

INSERT INTO public."ConclaveEntityAttribute"(
"Id", "AttributeName", "ConclaveEntityId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
VALUES (44, 'Sites.SiteName', 3, '2021-05-06 17:10:10.852362', '2021-05-06 17:10:10.852362', false);

INSERT INTO public."ConclaveEntityAttribute"(
"Id", "AttributeName", "ConclaveEntityId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
VALUES (45, 'Sites.SiteAddress', 3, '2021-05-06 17:10:10.852362', '2021-05-06 17:10:10.852362', false);

INSERT INTO public."ConclaveEntityAttribute"(
"Id", "AttributeName", "ConclaveEntityId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
VALUES (46, 'Sites.SiteAddress.StreetAddress', 3, '2021-05-06 17:10:10.852362', '2021-05-06 17:10:10.852362', false);

INSERT INTO public."ConclaveEntityAttribute"(
"Id", "AttributeName", "ConclaveEntityId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
VALUES (47, 'Sites.SiteAddress.Locality', 3, '2021-05-06 17:10:10.852362', '2021-05-06 17:10:10.852362', false);

INSERT INTO public."ConclaveEntityAttribute"(
"Id", "AttributeName", "ConclaveEntityId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
VALUES (48, 'Sites.SiteAddress.Region', 3, '2021-05-06 17:10:10.852362', '2021-05-06 17:10:10.852362', false);

INSERT INTO public."ConclaveEntityAttribute"(
"Id", "AttributeName", "ConclaveEntityId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
VALUES (49, 'Sites.SiteAddress.PostalCode', 3, '2021-05-06 17:10:10.852362', '2021-05-06 17:10:10.852362', false);

INSERT INTO public."ConclaveEntityAttribute"(
"Id", "AttributeName", "ConclaveEntityId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
VALUES (50, 'Sites.SiteAddress.CountryCode', 3, '2021-05-06 17:10:10.852362', '2021-05-06 17:10:10.852362', false);

-- ============================== Org Contacts =============================================
INSERT INTO public."ConclaveEntityAttribute"(
"Id", "AttributeName", "ConclaveEntityId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
VALUES (51, 'ContactPoints', 4, '2021-05-06 17:10:10.852362', '2021-05-06 17:10:10.852362', false);

INSERT INTO public."ConclaveEntityAttribute"(
"Id", "AttributeName", "ConclaveEntityId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
VALUES (52, 'ContactPoints.ContactPointId', 4, '2021-05-06 17:10:10.852362', '2021-05-06 17:10:10.852362', false);

INSERT INTO public."ConclaveEntityAttribute"(
"Id", "AttributeName", "ConclaveEntityId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
VALUES (53, 'ContactPoints.ContactPointReason', 4, '2021-05-06 17:10:10.852362', '2021-05-06 17:10:10.852362', false);

INSERT INTO public."ConclaveEntityAttribute"(
"Id", "AttributeName", "ConclaveEntityId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
VALUES (54, 'ContactPoints.ContactPointName', 4, '2021-05-06 17:10:10.852362', '2021-05-06 17:10:10.852362', false);

INSERT INTO public."ConclaveEntityAttribute"(
"Id", "AttributeName", "ConclaveEntityId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
VALUES (55, 'Contacts', 4, '2021-05-06 17:10:10.852362', '2021-05-06 17:10:10.852362', false);

INSERT INTO public."ConclaveEntityAttribute"(
"Id", "AttributeName", "ConclaveEntityId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
VALUES (56, 'PHONE', 4, '2021-05-06 17:10:10.852362', '2021-05-06 17:10:10.852362', false);

INSERT INTO public."ConclaveEntityAttribute"(
"Id", "AttributeName", "ConclaveEntityId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
VALUES (57, 'EMAIL', 4, '2021-05-06 17:10:10.852362', '2021-05-06 17:10:10.852362', false);

INSERT INTO public."ConclaveEntityAttribute"(
"Id", "AttributeName", "ConclaveEntityId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
VALUES (58, 'FAX', 4, '2021-05-06 17:10:10.852362', '2021-05-06 17:10:10.852362', false);

INSERT INTO public."ConclaveEntityAttribute"(
"Id", "AttributeName", "ConclaveEntityId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
VALUES (59, 'WEB_ADDRESS', 4, '2021-05-06 17:10:10.852362', '2021-05-06 17:10:10.852362', false);

-- ============================== User Contacts =============================================
INSERT INTO public."ConclaveEntityAttribute"(
"Id", "AttributeName", "ConclaveEntityId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
VALUES (60, 'ContactPoints', 5, '2021-05-06 17:10:10.852362', '2021-05-06 17:10:10.852362', false);

INSERT INTO public."ConclaveEntityAttribute"(
"Id", "AttributeName", "ConclaveEntityId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
VALUES (61, 'ContactPoints.ContactPointId', 5, '2021-05-06 17:10:10.852362', '2021-05-06 17:10:10.852362', false);

INSERT INTO public."ConclaveEntityAttribute"(
"Id", "AttributeName", "ConclaveEntityId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
VALUES (62, 'ContactPoints.ContactPointReason', 5, '2021-05-06 17:10:10.852362', '2021-05-06 17:10:10.852362', false);

INSERT INTO public."ConclaveEntityAttribute"(
"Id", "AttributeName", "ConclaveEntityId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
VALUES (63, 'ContactPoints.ContactPointName', 5, '2021-05-06 17:10:10.852362', '2021-05-06 17:10:10.852362', false);

INSERT INTO public."ConclaveEntityAttribute"(
"Id", "AttributeName", "ConclaveEntityId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
VALUES (64, 'Contacts', 5, '2021-05-06 17:10:10.852362', '2021-05-06 17:10:10.852362', false);

INSERT INTO public."ConclaveEntityAttribute"(
"Id", "AttributeName", "ConclaveEntityId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
VALUES (65, 'PHONE', 5, '2021-05-06 17:10:10.852362', '2021-05-06 17:10:10.852362', false);

INSERT INTO public."ConclaveEntityAttribute"(
"Id", "AttributeName", "ConclaveEntityId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
VALUES (66, 'EMAIL', 5, '2021-05-06 17:10:10.852362', '2021-05-06 17:10:10.852362', false);

INSERT INTO public."ConclaveEntityAttribute"(
"Id", "AttributeName", "ConclaveEntityId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
VALUES (67, 'FAX', 5, '2021-05-06 17:10:10.852362', '2021-05-06 17:10:10.852362', false);

INSERT INTO public."ConclaveEntityAttribute"(
"Id", "AttributeName", "ConclaveEntityId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
VALUES (68, 'WEB_ADDRESS', 5, '2021-05-06 17:10:10.852362', '2021-05-06 17:10:10.852362', false);

-- ============================== Site Contacts =============================================
INSERT INTO public."ConclaveEntityAttribute"(
"Id", "AttributeName", "ConclaveEntityId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
VALUES (69, 'ContactPoints', 6, '2021-05-06 17:10:10.852362', '2021-05-06 17:10:10.852362', false);

INSERT INTO public."ConclaveEntityAttribute"(
"Id", "AttributeName", "ConclaveEntityId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
VALUES (70, 'ContactPoints.ContactPointId', 6, '2021-05-06 17:10:10.852362', '2021-05-06 17:10:10.852362', false);

INSERT INTO public."ConclaveEntityAttribute"(
"Id", "AttributeName", "ConclaveEntityId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
VALUES (71, 'ContactPoints.ContactPointReason', 6, '2021-05-06 17:10:10.852362', '2021-05-06 17:10:10.852362', false);

INSERT INTO public."ConclaveEntityAttribute"(
"Id", "AttributeName", "ConclaveEntityId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
VALUES (72, 'ContactPoints.ContactPointName', 6, '2021-05-06 17:10:10.852362', '2021-05-06 17:10:10.852362', false);

INSERT INTO public."ConclaveEntityAttribute"(
"Id", "AttributeName", "ConclaveEntityId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
VALUES (73, 'Contacts', 6, '2021-05-06 17:10:10.852362', '2021-05-06 17:10:10.852362', false);

INSERT INTO public."ConclaveEntityAttribute"(
"Id", "AttributeName", "ConclaveEntityId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
VALUES (74, 'PHONE', 6, '2021-05-06 17:10:10.852362', '2021-05-06 17:10:10.852362', false);

INSERT INTO public."ConclaveEntityAttribute"(
"Id", "AttributeName", "ConclaveEntityId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
VALUES (75, 'EMAIL', 6, '2021-05-06 17:10:10.852362', '2021-05-06 17:10:10.852362', false);

INSERT INTO public."ConclaveEntityAttribute"(
"Id", "AttributeName", "ConclaveEntityId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
VALUES (76, 'FAX', 6, '2021-05-06 17:10:10.852362', '2021-05-06 17:10:10.852362', false);

INSERT INTO public."ConclaveEntityAttribute"(
"Id", "AttributeName", "ConclaveEntityId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
VALUES (77, 'WEB_ADDRESS', 6, '2021-05-06 17:10:10.852362', '2021-05-06 17:10:10.852362', false);

-- ========================== Contact =============================================

INSERT INTO public."ConclaveEntityAttribute"(
"Id", "AttributeName", "ConclaveEntityId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
VALUES (78, 'ContactId', 7, '2021-05-06 17:10:10.852362', '2021-05-06 17:10:10.852362', false);

INSERT INTO public."ConclaveEntityAttribute"(
"Id", "AttributeName", "ConclaveEntityId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
VALUES (79, 'ContactType', 7, '2021-05-06 17:10:10.852362', '2021-05-06 17:10:10.852362', false);

INSERT INTO public."ConclaveEntityAttribute"(
"Id", "AttributeName", "ConclaveEntityId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
VALUES (80, 'ContactValue', 7, '2021-05-06 17:10:10.852362', '2021-05-06 17:10:10.852362', false);

INSERT INTO public."ConclaveEntityAttribute"(
"Id", "AttributeName", "ConclaveEntityId", "CreatedOnUtc", "LastUpdatedOnUtc", "IsDeleted")
VALUES (81, 'Contact', 7, '2021-05-06 17:10:10.852362', '2021-05-06 17:10:10.852362', false);

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
