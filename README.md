# Aqla.RunSharp

RunSharp is a runtime IL generator based on Reflection.Emit and IKVM which allows you to emit IL in a way similar to writing normal C# code.

It's a layer above the standard .NET Reflection.Emit API, allowing to generate/compile dynamic code at runtime very quickly and efficiently (unlike using CodeDOM and invoking the C# compiler).

The IKVM version has also an ability to emit NET 2.0 and .NET 4.0 assemblies (while running on, for example, .NET 3.0).

It is a fork of RunSharp from Google Code: https://code.google.com/p/runsharp/

- added IKVM support
- examples converted to tests
- added peverify checks
- fixed multiple bugs

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

	AssemblyGen ag = new AssemblyGen("Hello", new CompilerOptions() { OutputPath = "Hello.exe" });
	TypeGen Test = ag.Public.Class("Test");
	{
	   CodeGen g = Test.Public.Static.Method(typeof(void), "Main", typeof(string[]));
	   {
	      ContextualOperand args = g.Param(0, "args");
	      g.Invoke(typeof(Console), "WriteLine", "Hello " + args[0] + "!");
	   }
	}
	ag.Save();

The above code should generate roughly the same assembly as if the first example was compiled using csc. 

For more examples please see tests.
