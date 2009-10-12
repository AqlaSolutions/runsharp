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
	public sealed class MethodGen : RoutineGen<MethodGen>
	{
		string name;
		MethodAttributes attributes;
		MethodBuilder mb;
		MethodImplAttributes implFlags;
		Type interfaceType;

		internal MethodBuilder GetMethodBuilder()
		{
			LockSignature(); 
			return mb;
		}

		internal MethodGen(TypeGen owner, string name, MethodAttributes attributes, Type returnType, MethodImplAttributes implFlags)
			: base(owner, returnType)
		{
			this.name = name;
			this.attributes = owner.PreprocessAttributes(this, attributes);
			this.implFlags = implFlags;
		}

		protected override void CreateMember()
		{
			string methodName = name;

			if (interfaceType != null)
				methodName = interfaceType + "." + name;

			this.mb = Owner.TypeBuilder.DefineMethod(methodName, this.attributes | MethodAttributes.HideBySig, IsStatic ? CallingConventions.Standard : CallingConventions.HasThis, ReturnType, ParameterTypes);
			if (implFlags != 0)
				mb.SetImplementationFlags(implFlags);
		}

		protected override void RegisterMember()
		{
			Owner.Register(this);
		}

		public bool IsPublic
		{
			get { return (attributes & MethodAttributes.Public) != 0; }
		}

		public bool IsAbstract
		{
			get { return (attributes & MethodAttributes.Abstract) != 0; }
		}

		internal Type ImplementedInterface
		{
			get { return interfaceType; }
			set { interfaceType = value; }
		}

		#region RoutineGen concrete implementation

		protected internal override bool IsStatic
		{
			get { return (attributes & MethodAttributes.Static) != 0; }
		}

		protected internal override bool IsOverride
		{
			get
			{
				return Utils.IsOverride(attributes);
			}
		}

		public override string Name
		{
			get { return name; }
		}

		protected override bool HasCode
		{
			get { return !IsAbstract && (implFlags & MethodImplAttributes.Runtime) == 0; }
		}

		protected override ILGenerator GetILGenerator()
		{
			return mb.GetILGenerator();
		}

		protected override ParameterBuilder DefineParameter(int position, ParameterAttributes attributes, string parameterName)
		{
			return mb.DefineParameter(position, attributes, parameterName);
		}

		protected override MemberInfo Member
		{
			get { return mb; }
		}

		protected override AttributeTargets AttributeTarget
		{
			get { return AttributeTargets.Constructor; }
		}

		protected override void SetCustomAttribute(CustomAttributeBuilder cab)
		{
			mb.SetCustomAttribute(cab);
		}

		#endregion
		
		public override string ToString()
		{
			return OwnerType.ToString() + "." + name;
		}
	}
}
