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
	public abstract class RoutineGen<T> : SignatureGen<T>, ICodeGenContext where T : RoutineGen<T>
	{
		TypeGen owner;
		Type ownerType;
		CodeGen code;
		List<AttributeGen> customAttributes;

		protected RoutineGen(TypeGen owner, Type returnType)
			: base(returnType)
		{
			this.ownerType = this.owner = owner;

			if (owner != null)
				owner.RegisterForCompletion(this);
		}

		protected RoutineGen(Type ownerType, Type returnType)
			: base(returnType)
		{
			this.ownerType = ownerType;
		}

		public Type OwnerType { get { return ownerType; } }
		public TypeGen Owner { get { return owner; } }

		public CodeGen GetCode()
		{
			if (code == null)
			{
				if (!HasCode)
					throw new InvalidOperationException(Properties.Messages.ErrNoCodeAllowed);

				LockSignature();
				code = new CodeGen(this);
			}

			return code;
		}

		protected override void OnParametersLocked()
		{
			base.OnParametersLocked();
			CreateMember();
		}

		protected override void OnParametersCompleted()
		{
			base.OnParametersCompleted();
			RegisterMember();
		}

		#region Abstract interface
		public abstract string Name { get; }

		protected internal abstract bool IsStatic { get; }
		protected internal abstract bool IsOverride { get; }
		
		protected abstract ILGenerator GetILGenerator();

		protected abstract void CreateMember();
		protected abstract void RegisterMember();
		
		protected abstract bool HasCode { get; }
		protected abstract MemberInfo Member { get; }

		protected abstract AttributeTargets AttributeTarget { get; }
		protected abstract void SetCustomAttribute(CustomAttributeBuilder cab);
		#endregion

		#region Custom Attributes

		public T Attribute(AttributeType type)
		{
			BeginAttribute(type);
			return typedThis;
		}

		public T Attribute(AttributeType type, params object[] args)
		{
			BeginAttribute(type, args);
			return typedThis;
		}

		public AttributeGen<T> BeginAttribute(AttributeType type)
		{
			return BeginAttribute(type, EmptyArray<object>.Instance);
		}

		public AttributeGen<T> BeginAttribute(AttributeType type, params object[] args)
		{
			return AttributeGen<T>.CreateAndAdd(typedThis, ref customAttributes, AttributeTarget, type, args);
		}

		#endregion

		#region IRequiresCompletion Members

		void IDelayedDefinition.EndDefinition()
		{
			LockSignature();
		}

		void IDelayedCompletion.Complete()
		{
			LockSignature();

			if (HasCode)
			{
				GetCode().Complete();
			}

			AttributeGen.ApplyList(ref customAttributes, SetCustomAttribute);
		}

		#endregion

		#region IMemberInfo Members

		System.Reflection.MemberInfo IMemberInfo.Member
		{
			get { return Member; }
		}

		string IMemberInfo.Name
		{
			get { return Name; }
		}

		Type IMemberInfo.ReturnType
		{
			get { return ReturnType; }
		}

		Type[] IMemberInfo.ParameterTypes
		{
			get { return ParameterTypes; }
		}

		bool IMemberInfo.IsParameterArray
		{
			get { return ParameterCount > 0 && Parameters[ParameterCount - 1].IsParameterArray; }
		}

		bool IMemberInfo.IsStatic
		{
			get { return IsStatic; }
		}

		bool IMemberInfo.IsOverride
		{
			get { return IsOverride; }
		}

		#endregion

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2225:OperatorOverloadsHaveNamedAlternates", Justification = "CodeGen can be retrieved using the Code property")]
		public static implicit operator CodeGen(RoutineGen<T> rg)
		{
			if (rg == null)
				return null;

			return rg.GetCode();
		}

		#region IRoutineGen Members

		ILGenerator ICodeGenContext.GetILGenerator()
		{
			if (!HasCode)
				throw new InvalidOperationException(Properties.Messages.ErrNoCodeAllowed);

			return GetILGenerator();
		}

		#endregion
	}
}
