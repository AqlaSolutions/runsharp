using System.Diagnostics;
using TriAxis.RunSharp.Operands;
using TriAxis.RunSharp;

namespace TriAxis.RunSharp
{
    static class OperandExtensions
    {
        [DebuggerStepThrough]
        internal static void SetLeakedState(this Operand[] operands, bool state)
        {
            if (operands != null)
            {
                for (int i = 0; i < operands.Length; i++)
                {
                    operands[i].SetLeakedState(state);
                }
            }
        }
        [DebuggerStepThrough]
        internal static void SetLeakedState(this ContextualOperand[] operands, bool state)
        {
            if (operands != null)
            {
                for (int i = 0; i < operands.Length; i++)
                {
                    operands[i].SetLeakedState(state);
                }
            }
        }
        [DebuggerStepThrough]
        internal static void SetLeakedState(this Assignment[] operands, bool state)
        {
            if (operands != null)
            {
                for (int i = 0; i < operands.Length; i++)
                {
                    operands[i].SetLeakedState(state);
                }
            }
        }
        [DebuggerStepThrough]
        internal static void SetLeakedState(this ContextualAssignment[] operands, bool state)
        {
            if (operands != null)
            {
                for (int i = 0; i < operands.Length; i++)
                {
                    operands[i].SetLeakedState(state);
                }
            }
        }
        [DebuggerStepThrough]
        internal static Operand SetLeakedState(this Operand operand, bool state)
        {
            if ((object)operand != null)
                operand.LeakedState = state;
            return operand;
        }

        [DebuggerStepThrough]
        internal static ContextualOperand SetLeakedState(this ContextualOperand operand, bool state)
        {
            if ((object)operand != null)
                operand.LeakedState = state;
            return operand;
        }
        [DebuggerStepThrough]
        internal static Assignment SetLeakedState(this Assignment operand, bool state)
        {
            if ((object)operand != null)
                operand.LeakedState = state;
            return operand;
        }
        [DebuggerStepThrough]
        internal static ContextualAssignment SetLeakedState(this ContextualAssignment operand, bool state)
        {
            if ((object)operand != null)
                operand.LeakedState = state;
            return operand;
        }
    }
}