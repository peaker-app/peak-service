namespace PeakService.Application.UnitTests.TestData;

internal static class AsyncSequence
{
    public static async IAsyncEnumerable<TItem> Of<TItem>(params TItem[] items)
    {
        await Task.CompletedTask;

        foreach (TItem item in items)
        {
            yield return item;
        }
    }
}
