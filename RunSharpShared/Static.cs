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

namespace TriAxis.RunSharp
{
	using Operands;

	[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords", Justification = "There is no better name for this.")]
	public static class Static
	{
		public static ContextualOperand Field(Type type, string name, ITypeMapper typeMapper)
		{
			return new ContextualOperand(new Field((FieldInfo)typeMapper.TypeInfo.FindField(type, name, true).Member, null), typeMapper);
		}

		public static ContextualOperand Property(Type type, string name, ITypeMapper typeMapper)
		{
			return new ContextualOperand(Property(type, name, typeMapper, Operand.EmptyArray), typeMapper);
		}

		public static ContextualOperand Property(Type type, string name, ITypeMapper typeMapper, params Operand[] indexes)
		{
			return new ContextualOperand(new Property(typeMapper.TypeInfo.FindProperty(type, name, indexes, true), null, indexes), typeMapper);
		}

		public static ContextualOperand Invoke(Type type, string name, ITypeMapper typeMapper)
		{
			return new ContextualOperand(Invoke(type, name, typeMapper, Operand.EmptyArray), typeMapper);
		}

		public static ContextualOperand Invoke(Type type, string name, ITypeMapper typeMapper, params Operand[] args)
		{
			return new ContextualOperand(new Invocation(typeMapper.TypeInfo.FindMethod(type, name, args, true), null, args), typeMapper);
		}
	}
}
