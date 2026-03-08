namespace CaseGuard.Backend.Assignment.Contracts.Common;

public record PagedResponse<T>(IEnumerable<T> Items, int TotalCount, int Page, int PageSize);
