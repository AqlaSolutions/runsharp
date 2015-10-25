/*
Copyright(c) 2015, Vladyslav Taranov

MIT License

Permission is hereby granted, free of charge, to any person obtaining
a copy of this software and associated documentation files (the
"Software"), to deal in the Software without restriction, including
without limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to
permit persons to whom the Software is furnished to do so, subject to
the following conditions:

The above copyright notice and this permission notice shall be
included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

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
        public static Type TypeOf<T>(ITypeMapper typeMapper)
        {
            return typeMapper.MapType(typeof(T));
        }

        public static bool AreTypesEqual(Type a, Type b)
        {
            return a == b;
        }

        public static bool AreTypesEqual(Type a, Type b, ITypeMapper typeMapper)
        {
            return AreTypesEqual(a, b);
        }

#if FEAT_IKVM
        public static bool AreTypesEqual(Type a, System.Type b)
        {
            return a.FullName == b.FullName;
        }
        
        public static bool AreTypesEqual(Type a, System.Type b, ITypeMapper typeMapper)
        {
            return a == typeMapper.MapType(b);
        }

        public static bool AreTypesEqual(System.Type b, Type a, ITypeMapper typeMapper)
        {
            return AreTypesEqual(a, b, typeMapper);
        }
#endif
        public static bool IsAttribute(Type t)
        {
#if FEAT_IKVM
            return IsAssignableDirectlyFrom("System.Attribute", t);
#else
            return IsAssignableFrom(typeof(System.Attribute), t);
#endif
        }

        public static bool IsAssignableFrom(Type t, Type @from, ITypeMapper typeMapper)
        {
            return IsAssignableFrom(t, @from);
        }

        public static bool IsAssignableFrom(Type t, Type @from)
        {
            if (t == null) throw new ArgumentNullException(nameof(t));
#if FEAT_IKVM
            return IsAssignableFrom(t.FullName, @from);
#else
            return t.IsAssignableFrom(@from);
#endif
        }
#if FEAT_IKVM
        public static bool IsAssignableFrom(System.Type t, Type @from, ITypeMapper typeMapper)
        {
            if (t == null) throw new ArgumentNullException(nameof(t));
            return typeMapper.MapType(t).IsAssignableFrom(@from);
        }

        public static bool IsAssignableFrom(System.Type t, Type @from)
        {
            if (t == null) throw new ArgumentNullException(nameof(t));
            return IsAssignableFrom(t.FullName, @from);
        }
#endif
#if FEAT_IKVM
        public static bool IsAssignableFrom(string typeFullName, Type @from)
        {
            if (@from == null) throw new ArgumentNullException(nameof(@from));
            if (IsNullOrEmpty(typeFullName)) return false;
            if (typeFullName == @from.FullName) return true;
            foreach (var @interface in from.GetInterfaces())
            {
                if (typeFullName == @interface.FullName) return true;
            }

            from = from.BaseType;

            while (@from != null && @from.FullName != "System.Object" && @from.FullName != "System.ValueType")
            {
                if (typeFullName == from.FullName) return true;
                @from = @from.BaseType;
            }
            return false;
        }
#endif
        public static bool IsAssignableDirectlyFrom(string typeFullName, Type @from)
        {
            if (@from == null) throw new ArgumentNullException(nameof(@from));
            if (IsNullOrEmpty(typeFullName)) return false;
            if (typeFullName == @from.FullName) return true;
            
            from = from.BaseType;

            while (@from != null && @from.FullName != "System.Object" && @from.FullName != "System.ValueType")
            {
                if (typeFullName == from.FullName) return true;
                @from = @from.BaseType;
            }
            return false;
        }

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
        public static IList<CustomAttributeData> GetCustomAttributes(MemberInfo m, System.Type type, bool inherit, ITypeMapper mapper = null)
        {
            if (mapper != null) return m.__GetCustomAttributes(mapper.MapType(type), inherit);
            return GetCustomAttributes(m, type.FullName, inherit);
        }

        public static IList<CustomAttributeData> GetCustomAttributes(ParameterInfo m, System.Type type, bool inherit, ITypeMapper mapper = null)
        {
            if (mapper != null) return m.__GetCustomAttributes(mapper.MapType(type), inherit);
            return GetCustomAttributes(m, type.FullName, inherit);
        }
        
        public static IList<CustomAttributeData> GetCustomAttributes(MemberInfo m, string attribute, bool inherit)
        {
            var t = m.DeclaringType;
            while (t != null && t.FullName != "System.Object") t = t.BaseType;
            if (t == null) return new List<CustomAttributeData>();
            var list = GetCustomAttributes(m, t, inherit);
            var list2 = new List<CustomAttributeData>(list);
            list2.RemoveAll(el => !IsAssignableFrom(attribute, el.AttributeType));
            return list2;
        }

        public static IList<CustomAttributeData> GetCustomAttributes(ParameterInfo m, string attribute, bool inherit)
        {
            var t = m.ParameterType;
            while (t != null && t.FullName != "System.Object") t = t.BaseType;
            if (t == null) return new List<CustomAttributeData>();

            var list = GetCustomAttributes(m, t, inherit);
            var list2 = new List<CustomAttributeData>(list);
            list2.RemoveAll(el => !IsAssignableFrom(attribute, el.AttributeType));
            return list2;
        }

#else
        public static IList<object> GetCustomAttributes(MemberInfo m, string attribute, bool inherit)
        {
            var list = GetCustomAttributes(m, typeof(object), inherit);
            var list2 = new List<object>(list);
            list2.RemoveAll(el => IsAssignableDirectlyFrom(attribute, el.GetType()));
            return list2;
        }
#endif

#if FEAT_IKVM
        public static IList<CustomAttributeData> GetCustomAttributes(MemberInfo m, Type attribute, bool inherit)
        {
            return m.__GetCustomAttributes(attribute, inherit);
        }
#else
        public static IList<object> GetCustomAttributes(MemberInfo m, Type attribute, bool inherit, ITypeMapper mapper = null)
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
        public static IList<object> GetCustomAttributes(ParameterInfo m, Type attribute, bool inherit, ITypeMapper mapper = null)
        {
            return m.GetCustomAttributes(attribute, inherit);
        }
#endif
    }
}
