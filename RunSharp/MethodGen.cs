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
	public sealed class MethodGen : ICodeGenContext
	{
		TypeGen owner;
		string name;
		MethodAttributes attributes;
		Type returnType;
		Type[] parameterTypes;
		MethodBuilder mb;
		CodeGen code;

		internal MethodBuilder MethodBuilder { get { return mb; } }

		internal MethodGen(TypeGen owner, string name, MethodAttributes attributes, Type returnType, Type[] parameterTypes, MethodImplAttributes implFlags)
		{
			this.owner = owner;
			this.name = name;
			this.attributes = attributes;
			this.returnType = returnType;
			this.parameterTypes = parameterTypes;

			this.attributes = owner.PreprocessAttributes(this, attributes);

			this.mb = owner.TypeBuilder.DefineMethod(name, this.attributes | MethodAttributes.HideBySig, IsStatic ? CallingConventions.Standard : CallingConventions.HasThis, returnType, parameterTypes);
			if (implFlags != 0)
				mb.SetImplementationFlags(implFlags);

			if (!IsAbstract && (implFlags & MethodImplAttributes.Runtime) == 0)
			{
				this.code = new CodeGen(this);
				owner.AddCodeBlock(code);
			}
		}

		public CodeGen Code { get { return code; } }

		public bool IsPublic
		{
			get { return (attributes & MethodAttributes.Public) != 0; }
		}

		public bool IsAbstract
		{
			get { return (attributes & MethodAttributes.Abstract) != 0; }
		}

		public bool IsStatic
		{
			get { return (attributes & MethodAttributes.Static) != 0; }
		}

		public bool IsOverride
		{
			get
			{
				return Utils.IsOverride(attributes);
			}
		}

		public string Name
		{
			get { return name; }
		}

		#region ICodeGenContext Members

		ILGenerator ICodeGenContext.GetILGenerator()
		{
			return mb.GetILGenerator();
		}

		MemberInfo IMemberInfo.Member
		{
			get { return mb; }
		}

		Type ICodeGenContext.OwnerType
		{
			get { return owner.TypeBuilder; }
		}

		public Type ReturnType
		{
			get { return returnType; }
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

		void ICodeGenContext.DefineParameterName(int index, string name)
		{
			if (index < 0 || index >= parameterTypes.Length)
				throw new ArgumentOutOfRangeException("index");

			mb.DefineParameter(1 + index, ParameterAttributes.None, name);
		}
		#endregion

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2225:OperatorOverloadsHaveNamedAlternates", Justification = "CodeGen can be retrieved using the Code property")]
		public static implicit operator CodeGen(MethodGen mg)
		{
			if (mg == null)
				return null;

			return mg.code;
		}

		public override string ToString()
		{
			return owner.ToString() + "." + name;
		}
	}
}
