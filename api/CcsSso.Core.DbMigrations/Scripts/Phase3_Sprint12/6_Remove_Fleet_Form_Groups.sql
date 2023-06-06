CREATE OR REPLACE FUNCTION Remove_Fleet_Form_Groups(
	) RETURNS integer AS $$
BEGIN

    RAISE NOTICE 'Removing fleet role from all groups - start';

    UPDATE "OrganisationGroupEligibleRole"
        SET "IsDeleted"=true 
    WHERE "OrganisationEligibleRoleId" IN (
        SELECT "Id" FROM public."OrganisationEligibleRole" 
        WHERE "CcsAccessRoleId" IN (SELECT "Id" from public."CcsAccessRole" WHERE "CcsAccessRoleNameKey" = 'FP_USER')
        AND "IsDeleted" = false)
    AND "IsDeleted" = false;

    RAISE NOTICE 'Removing fleet role from all groups - stop';

RETURN 1;
END;
$$ LANGUAGE plpgsql;

SELECT Remove_Fleet_Form_Groups();

DROP FUNCTION Remove_Fleet_Form_Groups;