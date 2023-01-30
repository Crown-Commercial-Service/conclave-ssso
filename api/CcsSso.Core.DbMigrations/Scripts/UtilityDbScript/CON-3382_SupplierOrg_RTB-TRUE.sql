


-- reporting mode
SELECT * FROM "Organisation" WHERE "SupplierBuyerType"=0 and "RightToBuy"= true


-- Update mode

UPDATE "Organisation" SET "RightToBuy"=false
 WHERE "Id" in (
	SELECT "Id" FROM "Organisation" WHERE "SupplierBuyerType"=0 and "RightToBuy"= true 
 )

