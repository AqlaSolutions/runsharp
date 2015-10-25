using System;
using System.Collections.Generic;

namespace TriAxis.RunSharp.Tests
{
    public static class EnumerableActionExtensions
    {
        public static void RunAll(this IEnumerable<Action> actionList)
        {
            foreach (var el in actionList)
            {
                el?.Invoke();
            }
        }
    }
}