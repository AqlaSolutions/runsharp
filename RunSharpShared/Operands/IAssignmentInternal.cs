namespace TriAxis.RunSharp.Operands
{
    interface IAssignmentInternal : IAssignment
    {
        void Emit(CodeGen g);
    }
}