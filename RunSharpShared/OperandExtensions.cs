using TriAxis.RunSharp.Operands;
using TryAxis.RunSharp;

namespace TriAxis.RunSharp
{
    static class OperandExtensions
    {
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

        internal static Operand SetLeakedState(this Operand operand, bool state)
        {
            if ((object)operand != null)
                operand.LeakedState = state;
            return operand;
        }

        internal static ContextualOperand SetLeakedState(this ContextualOperand operand, bool state)
        {
            if ((object)operand != null)
                operand.LeakedState = state;
            return operand;
        }

        internal static Assignment SetLeakedState(this Assignment operand, bool state)
        {
            if ((object)operand != null)
                operand.LeakedState = state;
            return operand;
        }

        internal static ContextualAssignment SetLeakedState(this ContextualAssignment operand, bool state)
        {
            if ((object)operand != null)
                operand.LeakedState = state;
            return operand;
        }
    }
}