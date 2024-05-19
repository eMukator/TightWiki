SELECT
	P.Id,
	P.[Name],
	P.[Description],
	P.Navigation,
	P.CreatedByUserId,
	P.CreatedDate,
	P.ModifiedByUserId,
	P.ModifiedDate
FROM
	[Page] as P
WHERE
	P.Navigation = @Navigation
