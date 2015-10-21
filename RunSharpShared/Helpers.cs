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
        public static object GetPropertyValue(System.Reflection.PropertyInfo prop, object instance)
        {
            return GetPropertyValue(prop, instance, null);
        }

        public static object GetPropertyValue(System.Reflection.PropertyInfo prop, object instance, object[] index)
        {
#if !UNITY && (PORTABLE || WINRT || CF2 || CF35)
            return prop.GetValue(instance, index);
#else
            return prop.GetValue(instance, index);
#endif
        }

        public static MemberInfo[] GetInstanceFieldsAndProperties(Type type, bool publicOnly)
        {
#if WINRT
            System.Collections.Generic.List<MemberInfo> members = new System.Collections.Generic.List<MemberInfo>();
            foreach(FieldInfo field in type.GetRuntimeFields())
            {
                if(field.IsStatic) continue;
                if(field.IsPublic || !publicOnly) members.Add(field);
            }
            foreach(PropertyInfo prop in type.GetRuntimeProperties())
            {
                MethodInfo getter = Helpers.GetGetMethod(prop, true, true);
                if(getter == null || getter.IsStatic) continue;
                if(getter.IsPublic || !publicOnly) members.Add(prop);
            }
            return members.ToArray();
#else
            BindingFlags flags = publicOnly ? BindingFlags.Public | BindingFlags.Instance : BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic;
            PropertyInfo[] props = type.GetProperties(flags);
            FieldInfo[] fields = type.GetFields(flags);
            MemberInfo[] members = new MemberInfo[fields.Length + props.Length];
            props.CopyTo(members, 0);
            fields.CopyTo(members, props.Length);
            return members;
#endif
        }

        public static bool IsNullOrEmpty(string s)
        {
            return string.IsNullOrEmpty(s);
        }

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
