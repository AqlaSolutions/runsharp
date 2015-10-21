/*
 * Copyright (c) 2015, Stefan Simek, Vladyslav Taranov
 *
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to
 * permit persons to whom the Software is furnished to do so, subject to
 * the following conditions:
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
 * LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
 * OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
 * WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 *
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
    public interface ITypeInfo
    {
        ITypeMapper TypeMapper { get; }
        void RegisterProvider(Type t, ITypeInfoProvider prov);
        void UnregisterProvider(Type t);
        IEnumerable<IMemberInfo> GetConstructors(Type t);
        IEnumerable<IMemberInfo> GetFields(Type t);
        IEnumerable<IMemberInfo> GetProperties(Type t);
        IEnumerable<IMemberInfo> GetEvents(Type t);
        IEnumerable<IMemberInfo> GetMethods(Type t);
        string GetDefaultMember(Type t);
        IEnumerable<IMemberInfo> Filter(IEnumerable<IMemberInfo> source, string name, bool ignoreCase, bool isStatic, bool allowOverrides);
        ApplicableFunction FindConstructor(Type t, Operand[] args);
        IMemberInfo FindField(Type t, string name, bool @static);
        ApplicableFunction FindProperty(Type t, string name, Operand[] indexes, bool @static);
        IMemberInfo FindEvent(Type t, string name, bool @static);
        IEnumerable<Type> SearchInterfaces(Type t);
        ApplicableFunction FindMethod(Type t, string name, Operand[] args, bool @static);
    }
}