# Graphode
The aim of Graphode is to build a set of call graphs for each .NET application that it analyzes and store those graphs in your technology of choice such as Neo4j, Gephi etc. There are many call graph tools out there already, what makes Graphode interesting is that it:
- optionally includes accesses to external resources such as databases, web services, queues etc.
- can be integrated into a build system
- it has no visualization technology itself, it simply generates graphs for powerful graph technologies that allow you to query the data in the way you want to.

## Three Graph Types
There will be three graph types in order of greatest detail to lowest:
- Full call graph with certain calls omitted such as properties
- Public method call graph. Omit all calls to private methods.
- Cross-assembly method call graph. Omit calls to all private methods and to public methods in the same assembly. This is useful when understanding large applications and how the different assemblies depend on each other.

Each graph type will optionally include accesses to external resources such as databases, web services, queues etc. This means that using a graph query language you can do things such as:
- Detect all external accesses that are made when invoking a specific web service method.
- List all web service methods that end up reaching a given resource.

Currently only the cross-assembly method call graph is available.

## The Assignment Graph
The most complex piece of Graphode is finding the name of the resource once a resource access has been detected. The current version has in-built functionality to find connection strings in app.configs as this is pretty common. It uses simply rules such as: if there is only one connection string for the application then we assume that it is the connection string of the detected database access. 

When there are two or more connection strings then it uses a backtracking search (the complex bit). Starting at the detected resource access, for example ExecuteScalar then it walks IL instructions:
- find the ctor of the SqlCommand
- find the assignment of the SqlConnection via the ctor or a property setter
- find the SqlConnection ctor
- find the assignment of the connection string. This could come from a hard-coded string, usage of the connection manager, a property on some custom configuration class etc.

This IL instruction walking is made possible by an indexing process which creates an index of the left and right sides of all assignments. Each of these is called a Triple:
1. parent method
2. from (right side of assignment)
3. to (left side of assignment)

For example the following code has many assignments:
```charp
public int DoSomething()
{
  int x = 10;
  int y = 2;
  int result = Multiple(x, y);
  return result;
}

private int Multiply(int a, int b)
{
  return a*b;
}
```
This will generate the following triples:
- parent, from -> to
- DoSomething, 10 -> x
- DoSomething, 2 -> y
- DoSomething, return Multiply -> result
- DoSomething, x -> arg 1 Multiply
- DoSomething, y -> arg 2 Multiply
- Multiply, arg 1, stack obj
- Multiply, arg 2, stack obj
- Multiply, stack obj, return

Using this index of Triples, we can walk backwards through the assignment graph in IL very fast.

## This is experimental
There are no guarantees of precision at this point. This is quite old code that I am reviving and is currently only capable of analyzing up to .NET 4.0 and only can detect accesses to a database via ADO.NET or Entity Framework.

But I intend to bring it up to date, including .NET Core. 

## Optional custom code
What I cannot do is know how your configuration system works. Which means that people who want to use this software and include resource accesses in the graph will always need to write some custom code to integrate it into their own system. Creating the call graphs is the easy bit and there are plenty of tools for doing that already. Where Graphode gets interesting is that it will include database accesses (and other resources in the future) into this graph. 

But in order to find out the name of a database, it needs to reach into configuration. May be you use local app.configs, or may be you use Consul, or perhaps you have an in-house centralized configuration system, or you use environment variables. Graphode provides rudimentary hooks to plug in your own custom code that knows how to retrieve this data.

There are two hooks:
- Know when a resource access is being made. Perhaps you use Cassandra, you can tell Graphode how to detect that.
- Once detected, how to find the Cassandra db name being accessed. Graphode provides an IL backtraching search function that will walk the IL starting at the resource access looking for the interaction with the configuration system. So it can tell you for example that it found a call to the ConsulClient method you have registered and returns the string parameter. You can then use that string key to lookup the value in Consul and provide the database name to Graphode to include in the graph.

Of course, you don't provide any custom code then Graphode will still work, it will just tag all resources accesses it already can detect as "Unknown".
