using System.Diagnostics;
using TriAxis.RunSharp.Operands;
using TriAxis.RunSharp;

namespace TriAxis.RunSharp
{
    // had to remove ExtensionAttribute because otherwise .NET 4.0 projects can't reference this dll
    static class OperandExtensions
    {
        [DebuggerStepThrough]
        internal static void SetLeakedState(Operand[] operands, bool state)
        {
            if (operands != null)
            {
                for (int i = 0; i < operands.Length; i++)
                {
                    OperandExtensions.SetLeakedState(operands[i], state);
                }
            }
        }
        [DebuggerStepThrough]
        internal static void SetLeakedState(ContextualOperand[] operands, bool state)
        {
            if (operands != null)
            {
                for (int i = 0; i < operands.Length; i++)
                {
                    OperandExtensions.SetLeakedState(operands[i], state);
                }
            }
        }
        [DebuggerStepThrough]
        internal static void SetLeakedState(Assignment[] operands, bool state)
        {
            if (operands != null)
            {
                for (int i = 0; i < operands.Length; i++)
                {
                    OperandExtensions.SetLeakedState(operands[i], state);
                }
            }
        }
        [DebuggerStepThrough]
        internal static void SetLeakedState(ContextualAssignment[] operands, bool state)
        {
            if (operands != null)
            {
                for (int i = 0; i < operands.Length; i++)
                {
                    OperandExtensions.SetLeakedState(operands[i], state);
                }
            }
        }
        [DebuggerStepThrough]
        internal static Operand SetLeakedState(Operand operand, bool state)
        {
            if ((object)operand != null)
                operand.LeakedState = state;
            return operand;
        }

        [DebuggerStepThrough]
        internal static ContextualOperand SetLeakedState(ContextualOperand operand, bool state)
        {
            if ((object)operand != null)
                operand.LeakedState = state;
            return operand;
        }
        [DebuggerStepThrough]
        internal static Assignment SetLeakedState(Assignment operand, bool state)
        {
            if ((object)operand != null)
                operand.LeakedState = state;
            return operand;
        }
        [DebuggerStepThrough]
        internal static ContextualAssignment SetLeakedState(ContextualAssignment operand, bool state)
        {
            if ((object)operand != null)
                operand.LeakedState = state;
            return operand;
        }
    }
}