namespace ShoppingCart.IntegrationTests;

internal static class Eventually
{
    public static async Task<T> GetAsync<T>(Func<Task<T>> probe, Func<T, bool> isDone, TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(15));
        while (true)
        {
            var value = await probe();
            if (isDone(value) || DateTime.UtcNow > deadline)
            {
                return value;
            }

            await Task.Delay(100);
        }
    }
}
