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
    public class TypeMapper : ITypeMapper
    {
#if FEAT_IKVM
        Universe universe;

        public TypeMapper(Universe universe)
        {
            if (universe == null) throw new ArgumentNullException(nameof(universe));
            this.universe = universe;
            TypeInfo = new TypeInfo(this);
        }
#else
        
        public TypeMapper()
        {
            TypeInfo = new TypeInfo(this);
        }
#endif

        /// <summary>
        /// Translate a System.Type into the universe's type representation
        /// </summary>
        public Type MapType(System.Type type, bool demand)
        {
#if FEAT_IKVM
            if (type == null) return null;

            if (type.Assembly == typeof(IKVM.Reflection.Type).Assembly)
            {
                throw new InvalidOperationException(
                    string.Format(
                        "Somebody is passing me IKVM types! {0} should be fully-qualified at the call-site",
                        type.Name));
            }

            Type result = universe.GetType(type.AssemblyQualifiedName);

            if (result == null)
            {
                // things also tend to move around... *a lot* - especially in WinRT; search all as a fallback strategy
                foreach (Assembly a in universe.GetAssemblies())
                {
                    result = a.GetType(type.FullName);
                    if (result != null) break;
                }
                if (result == null && demand)
                {
                    throw new InvalidOperationException("Unable to map type: " + type.AssemblyQualifiedName);
                }
            }
            return result;
#else
            return type;
#endif
        }

        public Type GetType(string fullName, Assembly context = null)
        {
#if FEAT_IKVM
            if (context != null)
            {
                Type found = universe.GetType(context, fullName, false);
                if (found != null) return found;
            }
            return universe.GetType(fullName, false);
#else
            if (context != null)
            {
                Type found = context.GetType(fullName, false);
                if (found != null) return found;
            }
            return Type.GetType(fullName, false);
#endif
        }
        
        public virtual ITypeInfo TypeInfo { get; }
    }
}