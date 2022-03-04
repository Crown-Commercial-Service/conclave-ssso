START TRANSACTION;

UPDATE public."CountryDetails" 
SET "Name"='Mauritania',
"LastUpdatedOnUtc"=Now()
where "Code"='MR';

UPDATE public."CountryDetails" 
SET "Name"='France',
"LastUpdatedOnUtc"=Now()
where "Code"='FR';

COMMIT;

