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

namespace TriAxis.RunSharp.Operands
{
	class NewDelegate : Operand
	{
	    readonly Type _delegateType;
	    readonly Operand _target;
		MethodInfo _method;
		ConstructorInfo _delegateConstructor;
	    ITypeInfo _typeInfo;

		public NewDelegate(Type delegateType, Type targetType, string methodName, ITypeInfo typeInfo)
		{
			this._delegateType = delegateType;
		    this._typeInfo = typeInfo;
		    Initialize(targetType, methodName);
		}

		public NewDelegate(Type delegateType, Operand target, string methodName, ITypeInfo typeInfo)
		{
			this._delegateType = delegateType;
			this._target = target;
		    this._typeInfo = typeInfo;
		    Initialize(target.Type, methodName);
		}

		void Initialize(Type targetType, string methodName)
		{
			if (!_delegateType.IsSubclassOf(typeof(Delegate)))
				throw new ArgumentException(Properties.Messages.ErrInvalidDelegateType, "delegateType");

			IMemberInfo delegateInvocationMethod = null;

			foreach (IMemberInfo mi in TypeInfo.GetMethods(_delegateType))
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

			foreach (IMemberInfo mi in TypeInfo.GetConstructors(_delegateType))
			{
				if (mi.IsStatic)
					continue;

				Type[] ctorParamTypes = mi.ParameterTypes;

				if (ctorParamTypes.Length == 2 && ctorParamTypes[0] == typeof(object) && ctorParamTypes[1] == typeof(IntPtr))
				{
					if (_delegateConstructor != null)
						throw new ArgumentException(Properties.Messages.ErrInvalidDelegateType, "delegateType");

					_delegateConstructor = (ConstructorInfo)mi.Member;
				}
			}

			if (_delegateConstructor == null)
				throw new ArgumentException(Properties.Messages.ErrInvalidDelegateType, "delegateType");

			Type retType = delegateInvocationMethod.ReturnType;
			Type[] parameterTypes = delegateInvocationMethod.ParameterTypes;

			for ( ; targetType != null; targetType = targetType.BaseType)
			{
				foreach (IMemberInfo mi in TypeInfo.Filter(TypeInfo.GetMethods(targetType), methodName, false, (object)_target == null, false))
				{
					if (mi.ReturnType == retType && ArrayUtils.Equals(mi.ParameterTypes, parameterTypes))
					{
						if (_method == null)
							_method = (MethodInfo)mi.Member;
						else
							throw new AmbiguousMatchException(Properties.Messages.ErrAmbiguousBinding);
					}
				}

				if (_method != null)
					break;
			}

			if (_method == null)
				throw new MissingMethodException(Properties.Messages.ErrMissingMethod);
		}

		internal override void EmitGet(CodeGen g)
		{
			g.EmitGetHelper(_target, typeof(object), false);
			g.IL.Emit(OpCodes.Ldftn, _method);
			g.IL.Emit(OpCodes.Newobj, _delegateConstructor);
		}

		public override Type Type => _delegateType;
	}
}
