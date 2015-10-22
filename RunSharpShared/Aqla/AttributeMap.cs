/*
protobuf-net is Copyright 2008 Marc Gravell
   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
   

AqlaSerializer is Copyright 2015 Vladyslav Taranov

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
*/

// Modified by Vladyslav Taranov for AqlaSerializer, 2015

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
    public abstract class AttributeMap
    {
#if DEBUG
        [Obsolete("Please use AttributeType instead")]
        new public Type GetType() { return AttributeType; }
#endif
        public abstract bool TryGet(string key, bool publicOnly, out object value);
        public bool TryGet(string key, out object value)
        {
            return TryGet(key, true, out value);
        }
        public abstract Type AttributeType { get; }
        public static AttributeMap[] Create(ITypeMapper model, Type type, bool inherit)
        {
#if FEAT_IKVM
            Type attribType = model.MapType(typeof(System.Attribute), true);
            System.Collections.Generic.IList<CustomAttributeData> all = type.__GetCustomAttributes(attribType, inherit);
            AttributeMap[] result = new AttributeMap[all.Count];
            int index = 0;
            foreach (CustomAttributeData attrib in all)
            {
                result[index++] = new AttributeDataMap(attrib);
            }
            return result;
#else
#if WINRT
            Attribute[] all = System.Linq.Enumerable.ToArray(type.GetTypeInfo().GetCustomAttributes(inherit));
#else
            object[] all = type.GetCustomAttributes(inherit);
#endif
            AttributeMap[] result = new AttributeMap[all.Length];
            for(int i = 0 ; i < all.Length ; i++)
            {
                result[i] = new ReflectionAttributeMap((Attribute)all[i]);
            }
            return result;
#endif
        }

        public static AttributeMap GetAttribute(AttributeMap[] attribs, string fullName)
        {
            for (int i = 0; i < attribs.Length; i++)
            {
                AttributeMap attrib = attribs[i];
                if (attrib != null && attrib.AttributeType.FullName == fullName) return attrib;
            }
            return null;
        }

        public static AttributeMap[] Create(ITypeMapper model, MemberInfo member, bool inherit)
        {
#if FEAT_IKVM
            System.Collections.Generic.IList<CustomAttributeData> all = member.__GetCustomAttributes(model.MapType(typeof(Attribute), true), inherit);
            AttributeMap[] result = new AttributeMap[all.Count];
            int index = 0;
            foreach (CustomAttributeData attrib in all)
            {
                result[index++] = new AttributeDataMap(attrib);
            }
            return result;
#else
#if WINRT
            Attribute[] all = System.Linq.Enumerable.ToArray(member.GetCustomAttributes(inherit));
#else
            object[] all = member.GetCustomAttributes(inherit);
#endif
            AttributeMap[] result = new AttributeMap[all.Length];
            for(int i = 0 ; i < all.Length ; i++)
            {
                result[i] = new ReflectionAttributeMap((Attribute)all[i]);
            }
            return result;
#endif
        }
        public static AttributeMap[] Create(ITypeMapper model, Assembly assembly)
        {

#if FEAT_IKVM
            const bool inherit = false;
            System.Collections.Generic.IList<CustomAttributeData> all = assembly.__GetCustomAttributes(model.MapType(typeof(System.Attribute), true), inherit);
            AttributeMap[] result = new AttributeMap[all.Count];
            int index = 0;
            foreach (CustomAttributeData attrib in all)
            {
                result[index++] = new AttributeDataMap(attrib);
            }
            return result;
#else
#if WINRT
            Attribute[] all = System.Linq.Enumerable.ToArray(assembly.GetCustomAttributes());
#else
            const bool inherit = false;
            object[] all = assembly.GetCustomAttributes(inherit);
#endif
            AttributeMap[] result = new AttributeMap[all.Length];
            for(int i = 0 ; i < all.Length ; i++)
            {
                result[i] = new ReflectionAttributeMap((Attribute)all[i]);
            }
            return result;
#endif
        }
#if FEAT_IKVM
        private sealed class AttributeDataMap : AttributeMap
        {
            public override Type AttributeType
            {
                get { return attribute.Constructor.DeclaringType; }
            }
            private readonly CustomAttributeData attribute;
            public AttributeDataMap(CustomAttributeData attribute)
            {
                this.attribute = attribute;
            }
            public override bool TryGet(string key, bool publicOnly, out object value)
            {
                foreach (CustomAttributeNamedArgument arg in attribute.NamedArguments)
                {
                    if (string.Equals(arg.MemberInfo.Name, key, StringComparison.OrdinalIgnoreCase))
                    {
                        value = arg.TypedValue.Value;
                        return true;
                    }
                }


                int index = 0;
                ParameterInfo[] parameters = attribute.Constructor.GetParameters();
                foreach (CustomAttributeTypedArgument arg in attribute.ConstructorArguments)
                {
                    if (string.Equals(parameters[index++].Name, key, StringComparison.OrdinalIgnoreCase))
                    {
                        value = arg.Value;
                        return true;
                    }
                }
                value = null;
                return false;
            }
        }
#else
        public abstract object Target { get; }
        private sealed class ReflectionAttributeMap : AttributeMap
        {
            public override object Target => _attribute;

            public override Type AttributeType => _attribute.GetType();

            public override bool TryGet(string key, bool publicOnly, out object value)
            {
                MemberInfo[] members = Helpers.GetInstanceFieldsAndProperties(_attribute.GetType(), publicOnly);
                foreach (MemberInfo member in members)
                {
#if FX11
                    if (member.Name.ToUpper() == key.ToUpper())
#else
                    if (string.Equals(member.Name, key, StringComparison.OrdinalIgnoreCase))
#endif
                    {
                        PropertyInfo prop = member as PropertyInfo;
                        if (prop != null) {
                            value = Helpers.GetPropertyValue(prop, _attribute);
                            return true;
                        }
                        FieldInfo field = member as FieldInfo;
                        if (field != null) {
                            value = field.GetValue(_attribute);
                            return true;
                        }

                        throw new NotSupportedException(member.GetType().Name);
                    }
                }
                value = null;
                return false;
            }
            private readonly Attribute _attribute;
            public ReflectionAttributeMap(Attribute attribute)
            {
                _attribute = attribute;
            }
        }
#endif
    }
}