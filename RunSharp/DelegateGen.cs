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
using System.Reflection;
using System.Reflection.Emit;

namespace TriAxis.RunSharp
{
	public class DelegateGen : SignatureGen<DelegateGen>
	{
		AssemblyGen owner;
		string name;
		TypeAttributes attrs;
		TypeGen delegateType;
		List<AttributeGen> customAttributes;

		public DelegateGen(AssemblyGen owner, string name, Type returnType, TypeAttributes attrs)
			: base(returnType)
		{
			this.owner = owner;
			this.name = name;
			this.attrs = attrs;
		}

		protected override System.Reflection.Emit.ParameterBuilder DefineParameter(int position, System.Reflection.ParameterAttributes attributes, string parameterName)
		{
			return null;
		}

		TypeGen GetDelegateType()
		{
			if (delegateType == null)
			{
				LockSignature();
				delegateType = ImplementDelegate();
			}

			return delegateType;
		}

		#region Custom Attributes

		public DelegateGen Attribute(AttributeType type)
		{
			BeginAttribute(type);
			return this;
		}

		public DelegateGen Attribute(AttributeType type, params object[] args)
		{
			BeginAttribute(type, args);
			return this;
		}

		public AttributeGen<DelegateGen> BeginAttribute(AttributeType type)
		{
			return BeginAttribute(type, EmptyArray<object>.Instance);
		}

		public AttributeGen<DelegateGen> BeginAttribute(AttributeType type, params object[] args)
		{
			return AttributeGen<DelegateGen>.CreateAndAdd(this, ref customAttributes, AttributeTargets.Delegate, type, args);
		}

		#endregion

		TypeGen ImplementDelegate()
		{
			TypeGen tg = new TypeGen(owner, name, attrs, typeof(MulticastDelegate), Type.EmptyTypes);

			ConstructorBuilder cb = tg.Public.RuntimeImpl.Constructor()
				.Parameter(typeof(object), "object")
				.Parameter(typeof(IntPtr), "method")
				.GetConstructorBuilder();
			cb.SetImplementationFlags(MethodImplAttributes.Runtime | MethodImplAttributes.Managed);

			MethodBuilder mb = tg.Public.Virtual.RuntimeImpl.Method(typeof(IAsyncResult), "BeginInvoke")
				.CopyParameters(Parameters)
				.UncheckedParameter(typeof(AsyncCallback), "callback")
				.UncheckedParameter(typeof(object), "object")
				.GetMethodBuilder();
			mb.SetImplementationFlags(MethodImplAttributes.Runtime | MethodImplAttributes.Managed);

			mb = tg.Public.Virtual.RuntimeImpl.Method(ReturnType, "EndInvoke")
				.Parameter(typeof(IAsyncResult), "result")
				.GetMethodBuilder();
			mb.SetImplementationFlags(MethodImplAttributes.Runtime | MethodImplAttributes.Managed);

			mb = tg.Public.Virtual.RuntimeImpl.Method(ReturnType, "Invoke")
				.CopyParameters(Parameters)
				.GetMethodBuilder();
			mb.SetImplementationFlags(MethodImplAttributes.Runtime | MethodImplAttributes.Managed);

			AttributeGen.ApplyList(ref customAttributes, tg.TypeBuilder.SetCustomAttribute);

			return tg;
		}

		public static implicit operator TypeGen(DelegateGen delegateGen)
		{
			return delegateGen.GetDelegateType();
		}
	}
}
