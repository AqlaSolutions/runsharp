using System;
using System.Collections.Generic;
using System.Text;
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


namespace TriAxis.RunSharp
{
    public interface ITypeMapper
    {
        /// <summary>
        /// Translate a System.Type into the universe's type representation
        /// </summary>
        Type MapType(System.Type type, bool demand = true);

        Type GetType(string fullName, Assembly context);

        ITypeInfo TypeInfo { get; }
    }
}