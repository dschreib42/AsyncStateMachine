namespace BugTrackerExample
{
    internal class Program
    {
        private static async Task Main()
        {
            var bug = new Bug("Incorrect stock count");

            await bug.InitializeAsync();
            await bug.AssignAsync("Joe");
            await bug.DeferAsync();
            await bug.AssignAsync("Harry");
            await bug.AssignAsync("Fred");
            await bug.CloseAsync();

            Console.WriteLine();
            Console.WriteLine("State machine:");
            Console.WriteLine(bug.ToDotGraph());

            // wait for a key press, unless the input is redirected (e.g. when run from a script or CI)
            if (!Console.IsInputRedirected)
                Console.ReadKey(false);
        }
    }
}