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
    static class Helpers
    {
        public static Type GetEnumEnderlyingType(Type type)
        {
#if FEAT_IKVM
            return type.GetEnumUnderlyingType();
#else
            return Enum.GetUnderlyingType(type);
#endif
        }

#if FEAT_IKVM
        public static IList<CustomAttributeData> GetCustomAttributes(MemberInfo m, Type attribute, bool inherit)
        {
            return m.__GetCustomAttributes(attribute, inherit);
        }
#else
        public static IList<object> GetCustomAttributes(MemberInfo m, Type attribute, bool inherit)
        {
            return m.GetCustomAttributes(attribute, inherit);
        }
#endif
#if FEAT_IKVM
        public static IList<CustomAttributeData> GetCustomAttributes(ParameterInfo m, Type attribute, bool inherit)
        {
            return m.__GetCustomAttributes(attribute, inherit);
        }
#else
        public static IList<object> GetCustomAttributes(ParameterInfo m, Type attribute, bool inherit)
        {
            return m.GetCustomAttributes(attribute, inherit);
        }
#endif
    }
}
