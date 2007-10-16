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

namespace TriAxis.RunSharp.Operands
{
	class NewDelegate : Operand
	{
		Type delegateType;
		Operand target;
		MethodInfo method;
		ConstructorInfo delegateConstructor;

		public NewDelegate(Type delegateType, Type targetType, string methodName)
		{
			this.delegateType = delegateType;
			Initialize(targetType, methodName);
		}

		public NewDelegate(Type delegateType, Operand target, string methodName)
		{
			this.delegateType = delegateType;
			this.target = target;
			Initialize(target.Type, methodName);
		}

		void Initialize(Type targetType, string methodName)
		{
			if (!delegateType.IsSubclassOf(typeof(Delegate)))
				throw new ArgumentException(Properties.Messages.ErrInvalidDelegateType, "delegateType");

			IMemberInfo delegateInvocationMethod = null;

			foreach (IMemberInfo mi in TypeInfo.GetMethods(delegateType))
			{
				if (mi.Name == "Invoke")
				{
					if (delegateInvocationMethod != null)
						throw new ArgumentException(Properties.Messages.ErrInvalidDelegateType, "delegateType");

					delegateInvocationMethod = mi;
				}
			}

			if (delegateInvocationMethod == null)
				throw new ArgumentException(Properties.Messages.ErrInvalidDelegateType, "delegateType");

			foreach (IMemberInfo mi in TypeInfo.GetConstructors(delegateType))
			{
				if (mi.IsStatic)
					continue;

				Type[] ctorParamTypes = mi.ParameterTypes;

				if (ctorParamTypes.Length == 2 && ctorParamTypes[0] == typeof(object) && ctorParamTypes[1] == typeof(IntPtr))
				{
					if (delegateConstructor != null)
						throw new ArgumentException(Properties.Messages.ErrInvalidDelegateType, "delegateType");

					delegateConstructor = (ConstructorInfo)mi.Member;
				}
			}

			if (delegateConstructor == null)
				throw new ArgumentException(Properties.Messages.ErrInvalidDelegateType, "delegateType");

			Type retType = delegateInvocationMethod.ReturnType;
			Type[] parameterTypes = delegateInvocationMethod.ParameterTypes;

			for ( ; targetType != null; targetType = targetType.BaseType)
			{
				foreach (IMemberInfo mi in TypeInfo.Filter(TypeInfo.GetMethods(targetType), methodName, false, (object)target == null, false))
				{
					if (mi.ReturnType == retType && ArrayUtils.Equals(mi.ParameterTypes, parameterTypes))
					{
						if (method == null)
							method = (MethodInfo)mi.Member;
						else
							throw new AmbiguousMatchException(Properties.Messages.ErrAmbiguousBinding);
					}
				}

				if (method != null)
					break;
			}

			if (method == null)
				throw new MissingMethodException(Properties.Messages.ErrMissingMethod);
		}

		internal override void EmitGet(CodeGen g)
		{
			g.EmitGetHelper(target, typeof(object), false);
			g.IL.Emit(OpCodes.Ldftn, method);
			g.IL.Emit(OpCodes.Newobj, delegateConstructor);
		}

		public override Type Type
		{
			get
			{
				return delegateType;
			}
		}
	}
}
