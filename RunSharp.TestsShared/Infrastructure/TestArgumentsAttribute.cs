using System;

namespace TriAxis.RunSharp
{
    [AttributeUsage(AttributeTargets.Method)]
    class TestArgumentsAttribute : Attribute
    {
        string[] _args;

        public TestArgumentsAttribute(params string[] args)
        {
            this._args = args;
        }

        public string[] Arguments { get { return _args; } }
    }
}