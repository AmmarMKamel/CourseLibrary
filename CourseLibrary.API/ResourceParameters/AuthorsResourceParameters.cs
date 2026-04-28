namespace CourseLibrary.API.ResourceParameters;

public class AuthorsResourceParameters
{
    const int maxPageSize = 10;

    public string? SearchQuery { get; set; }
    public string? MainCategory { get; set; }
    public int PageNumber { get; set; } = 1;
    private int _pageSize = 10;
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = Math.Min(value, maxPageSize);
    }
    public string OrderBy { get; set; } = "Name";
}
