UPDATE public."User"
	SET "AccountVerified"=true
	WHERE "IsDeleted" = false;
