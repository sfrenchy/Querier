using System.Collections.Generic;

public class PagedResult<T>
{
    public IEnumerable<T> Data { get; set; }
    public int TotalCount { get; set; }
} 