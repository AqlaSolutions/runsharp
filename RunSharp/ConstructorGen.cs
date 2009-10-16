/*
 * Copyright (c) 2009, Stefan Simek
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
using System.Reflection;
using System.Reflection.Emit;

namespace TriAxis.RunSharp
{
	public sealed class ConstructorGen : ICodeGenContext
	{
		TypeGen owner;
		MethodAttributes attributes;
		Type[] parameterTypes;
		ConstructorBuilder cb;
		CodeGen code;

		internal ConstructorBuilder ConstructorBuilder { get { return cb; } }

		internal ConstructorGen(TypeGen owner, MethodAttributes attributes, Type[] parameterTypes, MethodImplAttributes implFlags)
		{
			this.owner = owner;
			this.attributes = attributes;
			this.parameterTypes = parameterTypes;
			this.cb = owner.TypeBuilder.DefineConstructor(attributes | MethodAttributes.HideBySig, IsStatic ? CallingConventions.Standard : CallingConventions.HasThis, parameterTypes);
			if (implFlags != 0)
				cb.SetImplementationFlags(implFlags);

			if ((implFlags & MethodImplAttributes.Runtime) == 0)
			{
				this.code = new CodeGen(this);
				owner.AddCodeBlock(code);
			}
		}

		public string Name { get { return IsStatic ? ".cctor" : ".ctor"; } }
		public TypeGen Type { get { return owner; } }
		public CodeGen Code { get { return code; } }

		public bool IsStatic
		{
			get { return (attributes & MethodAttributes.Static) != 0; }
		}

		#region ICodeGenContext Members

		ILGenerator ICodeGenContext.GetILGenerator()
		{
			return cb.GetILGenerator();
		}

		Type ICodeGenContext.OwnerType
		{
			get { return owner.TypeBuilder; }
		}

		MemberInfo IMemberInfo.Member
		{
			get { return cb; }
		}

		public Type ReturnType
		{
			get { return typeof(void); }
		}

		public Type[] ParameterTypes
		{
			get { return parameterTypes; }
		}

		// TODO: params support
		bool IMemberInfo.IsParameterArray
		{
			get { return false; }
		}

		bool IMemberInfo.IsOverride
		{
			get { return false; }
		}

		void ICodeGenContext.DefineParameterName(int index, string name)
		{
			if (index < 0 || index >= parameterTypes.Length)
				throw new ArgumentOutOfRangeException("index");

			cb.DefineParameter(1 + index, ParameterAttributes.None, name);
		}
		#endregion

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2225:OperatorOverloadsHaveNamedAlternates", Justification = "CodeGen can be retrieved using the Code property")]
		public static implicit operator CodeGen(ConstructorGen cg)
		{
			if (cg == null)
				return null;

			return cg.code;
		}
	}
}
