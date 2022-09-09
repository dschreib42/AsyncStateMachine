# AsyncStateMachine [![Continuous](https://github.com/dschreib42/AsyncStateMachine/workflows/ci/badge.svg)](https://github.com/dschreib42/AsyncStateMachine/actions/workflows/ci.yml) [![NuGet Status](https://img.shields.io/nuget/v/AsyncStateMachine.svg?style=flat)](https://www.nuget.org/packages/AsyncStateMachine)

**A framework for creating *state machines* and lightweight *state machine-based workflows* for Microsoft .NET**

```csharp
// Create a state machine configuration with the initial Open state.
var config = new StateMachineConfiguration<Trigger, State>(State.Open);

// Configure the Open state
config.Configure(State.Open)
    .Permit(Trigger.assign, State.Assigned);

// Configure the Assigned state
config.Configure(State.Assigned)
    .PermitReentry(Trigger.assign)
    .Permit(Trigger.close, State.Closed)
    .Permit(Trigger.defer, State.Deferred)
    .OnEntry<string>(assignee => OnAssignedAsync(assignee)) // asynchronous with parameter
    .OnExit(OnDeassignedAsync); // asynchronous without parameter

// Configure the Deferred state
config.Configure(State.Deferred)
    .OnEntry(() => _assignee = null) // synchronous
    .Permit(Trigger.assign, State.Assigned);

// Configure the Closed state
config.Configure(State.Closed)
    .OnEntry(() => Console.WriteLine("Bug is closed"));

// ...

// Instantiate a new state machine using the previously created configuration.
var stateMachine = new StateMachine<Trigger, State>(config);

// initialize the state machine to invoke the OnEntry callback for the initial state.
await stateMachine.IntializeAsync();

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
 * Hierarchical states
 * Unit tested (code coverage >80%)

Some useful extensions are also provided:

 * Parameterized triggers
 * Reentrant states
 * Export state machine as graph (Graphviz/DOT, Mermaid Flow-Chart or Mermaid State-Diagram)

### Entry/Exit actions

```csharp
// Configure the Assigned state
config.Configure(State.Assigned)
    .Permit(Trigger.assign, State.Assigned)
    .Permit(Trigger.close, State.Closed)
    .Permit(Trigger.defer, State.Deferred)
    .OnEntry<string>(assignee => OnAssignedAsync(assignee)) // asynchronous with parameter
    .OnExit(OnDeassignedAsync); // asynchronous without parameter
```

Entry/Exit actions can be used to apply certain functionality when a certain state is reached. The OnEntry() calls are all invoked when the state is reached and all OnExit() calls are invoked, when the state is changed. OnEntry() calls support synchronous and asynchronous functions as well as parameterized versions. Currently,
only a single parameter is supported. If several parameters are needed, then these must be encapsulated in a class/struct/pair. When multiple OnEntry/OnExit callbacks are registered, then all of them are executed in the order of registration.

### Guard Clauses

The state machine will choose between multiple transitions based on guard clauses, e.g.:

```csharp
config.Configure(State.SomeState)
    .PermitIf(Trigger.ab, State.A, () => Condition)
    .PermitIf(Trigger.ab, State.B, () => !Condition);
```

Depending on the guard clause, the trigger either transitions to state A or state B.
Guard clauses within a state must be mutually exclusive (multiple guard clauses cannot be valid at the same time).
The guard clauses will be evaluated whenever a trigger is fired. Guards should therefor be made side effect free.

### Parameterised Triggers

A strongly-typed parameter can be assigned to trigger:

```csharp
config.Configure(State.Assigned)
    .OnEntry<string>(email => OnAssigned(email));

// ...

await stateMachine.FireAsync(assignTrigger, "john.doe@example.com");
```

If no callback can be found handling the given type of argument, an ArgumentException is thrown.
If multiple parameters have to be passed into the attached callback, then wrap the parameters into a new class and pass a strongly-typed instance of that class into the FireAsync() call.
It doesn't matter if the callback is synchronous or asynchronous, both versions are supported out of the box. 

### Ignored Transitions and Reentrant States

Firing a trigger that does not have an allowed transition associated with it will cause an exception (InvalidOperationException) to be thrown.

To ignore triggers within certain states, use the `Ignore(...)` directive:

```csharp
config.Configure(State.Closed)
    .Ignore(Trigger.close);
```

Alternatively, a state can be marked reentrant so its entry and exit actions will fire even when transitioning from/to itself:

```csharp
config.Configure(State.Assigned)
    .PermitReentry(Trigger.assign)
    .OnEntry(() => SendEmailToAssignee());
```

By default, triggers must be ignored explicitly. Otherwise an exception (InvalidOperationException) is thrown.

### State Hierarchy

```csharp
config.Configure(State.A);
config.Configure(State.B)
    .SubstateOf(A);
config.Configure(State.C)
    .SubstateOf(B);
```

Defines a multi-level hierarchy where C and B are sub-states of A. The `CurrentState` property always returns the current state. But with `InStateAsync(...)` can be checked,
if the machine is in a sub-state and in the super-/parent-state.

The max. supported depth of hierarchies is limited to InStateAsync(...) by `ushort.MaxValue = 65535`.

### Observation

AsyncStateMachine provides an observable to propagate state changes:

```csharp
IDisposable subscription = stateMachine.Observable.Subscribe(
    (source, trigger, destination) => Console.WriteLine($"{source} --{trigger}--> {destination}"));

// ...

subscription.Dispose();
```

The observable is invoked, whenever the state machine state has changed.

### Export as graph

It can be useful to visualize state machines. With this approach the code is the authoritative source and state diagrams are always up-to-date.
Therefore AsyncStateMachine supports different graph engines like: Graphviz and Mermaid

#### Graphviz

```csharp
string graph = DotGraph.Format(config);
```

The `DotGraph.Format()` method returns a string representation of the state machine in the [DOT graph language](https://en.wikipedia.org/wiki/DOT_(graph_description_language)).

This can then be rendered by tools that support the DOT graph language, such as the [dot command line tool](http://www.graphviz.org/doc/info/command.html) from [graphviz.org](http://www.graphviz.org) or use http://www.webgraphviz.com for instant viewing.

#### Mermaid

```csharp
string graph = MermaidStateGraph.Format(config);
```

or 

```csharp
string graph = MermaidFlowChartGraph.Format(config);
```

The `Mermaid*Graph.Format()` methods return a string representation of the state machine in the [Mermaid graph language](https://mermaid-js.github.io)).

This string representation can then be rendered by tools that support the mermaid graph language, such as Azure DevOPS or https://mermaid.live for instant viewing.

## Building

AsyncStateMachine runs on .NET and .NetCore platforms targeting .NET Standard 2.0 and .NET Standard2.1. Visual Studio 2022 or later is required to build the solution, because .NET 6.0 is the default.

## Project Goals

This page is an almost-complete description of AsyncStateMachine, and its explicit aim is to remain minimal.

Please use the issue tracker if you'd like to report problems or discuss features.
