namespace MilitaryRoster.ViewModels;

public class PagedList<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalPages => TotalCount == 0 ? 1 : (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;

    public int StartItemIndex => TotalCount == 0 ? 0 : ((PageNumber - 1) * PageSize) + 1;
    public int EndItemIndex => Math.Min(PageNumber * PageSize, TotalCount);
}
