using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using TryAxis.RunSharp;
#if FEAT_IKVM
using IKVM.Reflection;
using IKVM.Reflection.Emit;
using Type = IKVM.Reflection.Type;
using MissingMethodException = System.MissingMethodException;
using MissingMemberException = System.MissingMemberException;
using DefaultMemberAttribute = System.Reflection.DefaultMemberAttribute;
using Attribute = IKVM.Reflection.CustomAttributeData;
using BindingFlags = IKVM.Reflection.BindingFlags;
#else
using System.Reflection;
using System.Reflection.Emit;

#endif

namespace TriAxis.RunSharp.Operands
{
    public class ContextualAssignment : ContextualOperand, IAssignmentInternal
    {
        readonly Assignment _assignment;

        public ContextualAssignment(Assignment assignment, ITypeMapper typeMapper)
            : base(assignment, typeMapper)
        {
            _assignment = assignment;
        }

        public virtual void Emit(CodeGen g)
        {
            _assignment.Emit(g);
        }
    }
}