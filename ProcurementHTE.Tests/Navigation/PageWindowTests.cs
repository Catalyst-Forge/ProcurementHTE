using ProcurementHTE.Web.Navigation;

namespace ProcurementHTE.Tests.Navigation;

public class PageWindowTests
{
    [Fact]
    public void ShortListShowsEveryPage()
    {
        Assert.Equal([1, 2, 3, 4, 5, 6, 7], PageWindow.For(3, 7));
        Assert.Equal([1, 2, 3, 4, 5], PageWindow.For(3, 5, PageWindow.Narrow));
    }

    [Theory]
    [InlineData(PageWindow.Wide)]
    [InlineData(PageWindow.Narrow)]
    public void LongListNeverShowsMoreThanItsSlots(int slots)
    {
        // Arsip LDP had 90 pages and drew all of them, pushing the page 2,700px wide.
        for (var page = 1; page <= 90; page++)
            Assert.Equal(slots, PageWindow.For(page, 90, slots).Count);
    }

    [Theory]
    [InlineData(PageWindow.Wide)]
    [InlineData(PageWindow.Narrow)]
    public void CurrentFirstAndLastPageAreAlwaysShown(int slots)
    {
        for (var page = 1; page <= 90; page++)
        {
            var window = PageWindow.For(page, 90, slots);
            Assert.Contains(page, window);
            Assert.Equal(1, window[0]);
            Assert.Equal(90, window[^1]);
        }
    }

    [Theory]
    [InlineData(PageWindow.Wide)]
    [InlineData(PageWindow.Narrow)]
    public void GapsAlwaysHideAtLeastTwoPages(int slots)
    {
        // A gap that hides one page takes the same room as that page's number.
        for (var total = slots + 1; total <= 30; total++)
        for (var page = 1; page <= total; page++)
        {
            var window = PageWindow.For(page, total, slots);
            for (var i = 1; i < window.Count - 1; i++)
            {
                if (window[i] is null)
                    Assert.True(window[i + 1] - window[i - 1] >= 3, $"page {page} of {total}");
            }
        }
    }

    [Fact]
    public void WideWindow()
    {
        Assert.Equal([1, 2, 3, 4, 5, null, 90], PageWindow.For(1, 90));
        Assert.Equal([1, 2, 3, 4, 5, null, 90], PageWindow.For(4, 90));
        Assert.Equal([1, null, 44, 45, 46, null, 90], PageWindow.For(45, 90));
        Assert.Equal([1, null, 86, 87, 88, 89, 90], PageWindow.For(87, 90));
        Assert.Equal([1, null, 86, 87, 88, 89, 90], PageWindow.For(90, 90));
    }

    [Fact]
    public void NarrowWindow()
    {
        Assert.Equal([1, 2, 3, null, 90], PageWindow.For(1, 90, PageWindow.Narrow));
        Assert.Equal([1, 2, 3, null, 90], PageWindow.For(3, 90, PageWindow.Narrow));
        Assert.Equal([1, null, 45, null, 90], PageWindow.For(45, 90, PageWindow.Narrow));
        Assert.Equal([1, null, 88, 89, 90], PageWindow.For(88, 90, PageWindow.Narrow));
    }

    [Fact]
    public void OutOfRangePageIsClamped()
    {
        Assert.Equal([1, null, 86, 87, 88, 89, 90], PageWindow.For(500, 90));
        Assert.Equal([1, 2, 3, 4, 5, null, 90], PageWindow.For(0, 90));
        Assert.Empty(PageWindow.For(1, 0));
    }
}
