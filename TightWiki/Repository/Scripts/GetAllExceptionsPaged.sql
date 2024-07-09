SELECT
	Id,
	[Text],
	[ExceptionText],
	[StackTrace],
	[CreatedDate],

	@PageSize as PaginationPageSize,
	(
		SELECT
			CAST((Count(0) + (@PageSize - 1.0)) / @PageSize AS INTEGER)
		FROM
			[Exception] as P
	) as PaginationPageCount
FROM
	[Exception]
LIMIT @PageSize
OFFSET (@PageNumber - 1) * @PageSize
