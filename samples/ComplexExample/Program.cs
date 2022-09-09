using AsyncStateMachine;
using AsyncStateMachine.Graphs;

namespace ComplexExample
{
    internal static class Program
    {
        private enum Trigger
        {
            ab,
            stop,
        }

        private enum State
        {
            Start,
            A,
            B,
            Stop,
        }

        public static async Task Main()
        {
            var triggerSelector = true;

            var config = new StateMachineConfiguration<Trigger, State>(State.Start);

            config.Configure(State.Start)
                .PermitIf(Trigger.ab, State.A, () => triggerSelector)
                .PermitIf(Trigger.ab, State.B, () => !triggerSelector)
                .OnEntry(() => Console.WriteLine("\n\nEntered Start"))
                .OnExit(() => Console.WriteLine("Exited Start"));

            config.Configure(State.A)
                .Permit(Trigger.stop, State.Stop)
                .OnEntry(() => Console.WriteLine("Entered A"))
                .OnExit(() => Console.WriteLine("Exited A"));

            config.Configure(State.B)
                .Permit(Trigger.stop, State.Stop)
                .OnEntry(() => Console.WriteLine("Entered B"))
                .OnExit(() => Console.WriteLine("Exited B"));

            config.Configure(State.Stop)
                .Ignore(Trigger.stop)
                .OnEntry<string>(text => Console.WriteLine($"Entered Stop ({text})"));

            using var sm = new StateMachine<Trigger, State>(config);

            var rand = new Random();

            using var _ = sm.Observable.Subscribe(OnTransitioned);
            {
                await sm.InitializeAsync();

                Console.WriteLine(DotGraph.Format(config));
                Console.WriteLine(MermaidStateGraph.Format(config));
                Console.WriteLine(MermaidFlowChartGraph.Format(config));

                while (true)
                {
                    triggerSelector = (rand.Next(0, 10) % 2) == 0;

                    // slow down
                    await Task.Delay(TimeSpan.FromSeconds(1));

                    // trigger depending on trigger selector
                    await sm.FireAsync(Trigger.ab);

                    // slow down
                    await Task.Delay(TimeSpan.FromSeconds(1));

                    // trigger with strongly-typed parameter
                    await sm.FireAsync(Trigger.stop, $"Hello from {sm.CurrentState}");

                    // slow down
                    await Task.Delay(TimeSpan.FromSeconds(1));

                    // restart from initial state
                    await sm.ResetAsync();
                }
            }
        }

        private static void OnTransitioned(Transition<Trigger, State> transition)
        {
            if (transition.IsStartTransition)
                Console.WriteLine($"(Initial transition to {transition.Destination})");
            else
                Console.WriteLine($"(Transitioned from {transition.Source} --{transition.Trigger}--> {transition.Destination})");
        }
    }
}