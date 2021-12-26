using AsyncStateMachine;
using AsyncStateMachine.Graphs;

namespace BugTrackerExample
{
    internal class Bug
    {
        private readonly StateMachine<Trigger, State> _machine;
        private readonly string _title;
        private string _assignee;

        private enum State
        {
            Open,
            Assigned,
            Deferred,
            Closed
        }

        private enum Trigger
        {
            assign,
            defer,
            close
        }

        /// <summary>
        /// Constructor for the <see cref="Bug"/> class.
        /// </summary>
        /// <param name="title">The title of the bug report</param>
        public Bug(string title)
        {
            _title = title;

            // Instantiate a new state machine in the Open state
            _machine = new StateMachine<Trigger, State>(State.Open);

            // Configure the Open state
            _machine.Configure(State.Open)
                .Permit(Trigger.assign, State.Assigned);

            // Configure the Assigned state
            _machine.Configure(State.Assigned)
                .PermitReentry(Trigger.assign)
                .Permit(Trigger.close, State.Closed)
                .Permit(Trigger.defer, State.Deferred)
                .OnEntry<string>(OnAssignedAsync)
                .OnExit(OnDeAssignedAsync);

            // Configure the Deferred state
            _machine.Configure(State.Deferred)
                .OnEntry(() => _assignee = null)
                .Permit(Trigger.assign, State.Assigned);

            // Configure the Closed state
            _machine.Configure(State.Closed)
                .OnEntry(() => Console.WriteLine("Bug is closed"));
        }

        public Task CloseAsync()
            => _machine.FireAsync(Trigger.close);

        public Task AssignAsync(string assignee)
            => _machine.FireAsync(Trigger.assign, assignee);

        public Task DeferAsync()
            => _machine.FireAsync(Trigger.defer);

        /// <summary>
        /// This method is called automatically when the Assigned state is entered, but only when the trigger is _assignTrigger.
        /// </summary>
        /// <param name="assignee">The person assigned to the bug.</param>
        private async Task OnAssignedAsync(string assignee)
        {
            if (_assignee != null && assignee != _assignee)
                await SendEmailToAssigneeAsync("Don't forget to help the new employee!");

            _assignee = assignee;
            await SendEmailToAssigneeAsync("You own it.");
        }

        /// <summary>
        /// This method is called when the state machine exits the Assigned state
        /// </summary>
        private Task OnDeAssignedAsync()
            => SendEmailToAssigneeAsync("You're off the hook.");

        /// <summary>
        /// This method simulates sending an email to an assignee.
        /// </summary>
        private Task SendEmailToAssigneeAsync(string message)
        {
            Console.WriteLine("{0}, RE {1}: {2}", _assignee, _title, message);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Export state machine as DOT graph.
        /// </summary>
        public string ToDotGraph()
            => DotGraph.Format(_machine.Transitions);
    }
}