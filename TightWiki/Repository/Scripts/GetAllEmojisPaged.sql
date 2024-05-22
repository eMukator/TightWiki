SELECT
	E.Id,
	E.[Name],
	E.MimeType,
	'%%' + lower(E.[Name]) + '%%' as Shortcut,
	@PageSize as PaginationSize,
	(
		SELECT
			(Round(Count(0) / (@PageSize + 0.0)  + 0.999))
		FROM
			Emoji as iE
	) as PaginationCount
FROM
	Emoji as E
ORDER BY
	E.[Name]
LIMIT @PageSize
OFFSET (@PageNumber - 1) * @PageSize
