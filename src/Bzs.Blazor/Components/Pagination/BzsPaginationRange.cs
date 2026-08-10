namespace Bzs.Blazor;

internal static class BzsPaginationRange
{
    internal static IReadOnlyList<int?> Create(
        int page,
        int pageCount,
        int siblingCount,
        int boundaryCount)
    {
        Validate(page, pageCount, siblingCount, boundaryCount);

        if (pageCount == 0)
        {
            return Array.Empty<int?>();
        }

        var selectedPages = new SortedSet<int> { page };
        AddRange(selectedPages, 1, Math.Min(boundaryCount, pageCount));
        AddRange(
            selectedPages,
            Math.Max(1, pageCount - Math.Min(boundaryCount, pageCount) + 1),
            pageCount);

        var siblingStart = (int)Math.Max(1L, (long)page - siblingCount);
        var siblingEnd = (int)Math.Min(pageCount, (long)page + siblingCount);
        AddRange(selectedPages, siblingStart, siblingEnd);

        var items = new List<int?>(selectedPages.Count + 2);
        int? previousPage = null;
        foreach (var selectedPage in selectedPages)
        {
            if (previousPage is int previous)
            {
                var gap = (long)selectedPage - previous;
                if (gap == 2)
                {
                    items.Add(previous + 1);
                }
                else if (gap > 2)
                {
                    items.Add(null);
                }
            }

            items.Add(selectedPage);
            previousPage = selectedPage;
        }

        return items;
    }

    private static void Validate(int page, int pageCount, int siblingCount, int boundaryCount)
    {
        if (pageCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pageCount), pageCount, "PageCount cannot be negative.");
        }

        if (page < 1 || (pageCount == 0 ? page != 1 : page > pageCount))
        {
            throw new ArgumentOutOfRangeException(
                nameof(page),
                page,
                pageCount == 0
                    ? "Page must be 1 when PageCount is 0."
                    : "Page must be between 1 and PageCount, inclusive.");
        }

        if (siblingCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(siblingCount), siblingCount, "SiblingCount cannot be negative.");
        }

        if (boundaryCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(boundaryCount), boundaryCount, "BoundaryCount cannot be negative.");
        }
    }

    private static void AddRange(ISet<int> pages, int start, int end)
    {
        if (start > end)
        {
            return;
        }

        var page = start;
        while (true)
        {
            pages.Add(page);
            if (page == end)
            {
                return;
            }

            page++;
        }
    }
}
