# Aqla.RunSharp
It is a fork of RunSharp from Google Code: https://code.google.com/p/runsharp/

- added IKVM support
- examples converted to tests
- added peverify checks
- fixed multiple bugs

RunSharp is a layer above the standard .NET Reflection.Emit API, allowing to generate/compile dynamic code at runtime very quickly and efficiently (unlike using CodeDOM and invoking the C# compiler). To the best of my knowledge, there is no such library available at the moment.

The IKVM version has also an ability to emit .NET 2.0 and .NET 4.0 assemblies (while running on, for example, .NET 2.0).

# Example

A simple hello world example in C#

	public class Test
	{
	   public static void Main(string[] args)
	   {
	      Console.WriteLine("Hello " + args[0]);
	   }
	}


can be dynamically generated using RunSharp as follows:

	AssemblyGen ag = new AssemblyGen("hello.exe");
	TypeGen Test = ag.Public.Class("Test");
	{
	   CodeGen g = Test.Public.Static.Method(typeof(void), "Main", typeof(string[]));
	   {
	      Operand args = g.Param(0, "args");
	      g.Invoke(typeof(Console), "WriteLine", "Hello " + args[0] + "!");
	   }
	}
	ag.Save();

The above code should generate roughly the same assembly as if the first example was compiled using csc. 

For more examples please see tests.
