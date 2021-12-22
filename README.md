# AsyncStateMachine

**A framework for creating *state machines* and lightweight *state machine-based workflows* for Microsoft .NET**

```csharp
// Instantiate a new state machine in the Open state
var stateMachine = new StateMachine<Trigger, State>(State.Open);

// Configure the Open state
stateMachine.Configure(State.Open)
    .Permit(Trigger.assign, State.Assigned);

// Configure the Assigned state
stateMachine.Configure(State.Assigned)
    .Permit(Trigger.assign, State.Assigned)
    .Permit(Trigger.close, State.Closed)
    .Permit(Trigger.defer, State.Deferred)
    .OnEntry<string>(assignee => OnAssignedAsync(assignee)) // asynchronous with parameter
    .OnExit(OnDeassignedAsync); // asynchronous without parameter

// Configure the Deferred state
stateMachine.Configure(State.Deferred)
    .OnEntry(() => _assignee = null) // synchronous
    .Permit(Trigger.assign, State.Assigned);

// Configure the Closed state
stateMachine.Configure(State.Closed)
    .OnEntry(() => Console.WriteLine("Bug is closed"));

// ...

await stateMachine.FireAsync(Trigger.assign); // asynchronous
Assert.AreEqual(State.Assigned, stateMachine.CurrentState);
```

This project, as well as the example above, was inspired by [Stateless](https://github.com/dotnet-state-machine/stateless/).
The aim of this project is not to provide a one-to-one replacement of stateless. The goal was to provide a state machine providing
an asynchronous interface, lightweight implementation, easy to maintain and easy to extend. 

## Features

Most standard state machine constructs are supported:

 * Generic support for states and triggers of type struct (numbers, chars, enums, etc.)
 * Entry/exit actions for states (synchronous and asynchronous)
 * Guard clauses to support conditional transitions

Some useful extensions are also provided:

 * Parameterized triggers
 * Reentrant states
 * Export state machine as graph (Graphviz/DOT, Mermaid Flow-Chart, State-Diagram)

### Entry/Exit actions

...

### Guard Clauses

The state machine will choose between multiple transitions based on guard clauses, e.g.:

```csharp
stateMachine.Configure(State.SomeState)
    .PermitIf(Trigger.ab, State.A, () => Condition)
    .PermitIf(Trigger.ab, State.B, () => !Condition);
```

Depending on the guard clause, the trigger either transitions to state A or state B.
Guard clauses within a state must be mutually exclusive (multiple guard clauses cannot be valid at the same time).
The guard clauses will be evaluated whenever a trigger is fired. Guards should therefor be made side effect free.

### Parameterised Triggers

A strongly-typed parameter can be assigned to trigger:

```csharp
stateMachine.Configure(State.Assigned)
    .OnEntry<string>(email => OnAssigned(email));

await stateMachine.FireAsync(assignTrigger, "john.doe@example.com");
```

If no callback can be found handling the given type of argument, an ArgumentException is thrown.
If multiple parameters have to be passed into the attached callback, then wrap the parameters into a new class and pass a strongly-typed instance of that class into the FireAsync() call.
It doesn't matter if the callback is synchronous or asynchronous, both versions are supported out of the box. 

### Ignored Transitions and Reentrant States

Firing a trigger that does not have an allowed transition associated with it will cause an exception (InvalidOperationException) to be thrown.

To ignore triggers within certain states, use the `Ignore(...)` directive:

```csharp
phoneCall.Configure(State.Connected)
    .Ignore(Trigger.CallDialled);
```

Alternatively, a state can be marked reentrant so its entry and exit actions will fire even when transitioning from/to itself:

```csharp
stateMachine.Configure(State.Assigned)
    .Permit(Trigger.Assigned, State.Assign)
    .OnEntry(() => SendEmailToAssignee());
```

By default, triggers must be ignored explicitly. Otherwise an exception (InvalidOperationException) is thrown.

#### State transition

AsyncStateMachine provides an observable propagate state changes:

```csharp
IDisposable subscription = stateMachine.Observable.Subscribe(
    (source, trigger, destination) => Console.WriteLine($"{source} --{trigger}--> {destination}"));

// ...

subscription.Dispose();
```

The observable is invoked, whenever the state machine state has changed.

### Export as graph

It can be useful to visualize state machines. With this approach the code is the authoritative source and state diagrams are always up-to-date.
Therefore AsyncStateMachine supports different graph engines like:
* Graphviz
* Mermaid

#### Graphviz

```csharp
string graph = DotGraph.Format(stateMachine.Transitions);
```

The `DotGraph.Format()` method returns a string representation of the state machine in the [DOT graph language](https://en.wikipedia.org/wiki/DOT_(graph_description_language)).

This can then be rendered by tools that support the DOT graph language, such as the [dot command line tool](http://www.graphviz.org/doc/info/command.html) from [graphviz.org](http://www.graphviz.org) or [viz.js](https://github.com/mdaines/viz.js). See http://www.webgraphviz.com for instant gratification.

#### Mermaid

```csharp
string graph = MermaidStateGraph.Format(stateMachine.Transitions);
```

or 

```csharp
string graph = MermaidFlowChartGraph.Format(stateMachine.Transitions);
```

The `Mermaid*Graph.Format()` methods return a string representation of the state machine in the [Mermaid graph language](https://mermaid-js.github.io)).

This string representation can then be rendered by tools that support the mermaid graph language, such as Azure DevOPS or https://mermaid.live for instant viewing.

## Building

AsyncStateMachine runs on .NET and .NetCore platforms targeting .NET Standard 2.0 and .NET Standard2.1. Visual Studio 2022 or later is required to build the solution, because .NET 6.0 is the default.

## Project Goals

This page is an almost-complete description of AsyncStateMachine, and its explicit aim is to remain minimal.

Please use the issue tracker or the if you'd like to report problems or discuss features.
