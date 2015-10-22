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
	public abstract class RoutineGen<T> : SignatureGen<T>, ICodeGenContext where T : RoutineGen<T>
	{
	    readonly ICodeGenBasicContext _context;
	    CodeGen _code;
		List<AttributeGen> _customAttributes;

		protected RoutineGen(TypeGen owner, Type returnType, ICodeGenBasicContext context)
			: base(returnType, owner.TypeMapper)
		{
		    _context = context;
		    OwnerType = Owner = owner;
		    
			if (owner != null)
				owner.RegisterForCompletion(this);
		}

		protected RoutineGen(Type ownerType, Type returnType, ICodeGenBasicContext context)
			: base(returnType, context.TypeMapper)
		{
		    _context = context;
		    OwnerType = ownerType;
		}

	    public Type OwnerType { get; }
	    public TypeGen Owner { get; }

	    public CodeGen GetCode()
		{
			if (_code == null)
			{
				if (!HasCode)
					throw new InvalidOperationException(Properties.Messages.ErrNoCodeAllowed);

				LockSignature();
				_code = new CodeGen(this);
			}

			return _code;
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

		public override T Attribute(AttributeType type)
		{
			BeginAttribute(type);
			return TypedThis;
		}

	    public override T Attribute(AttributeType type, params object[] args)
		{
			BeginAttribute(type, args);
			return TypedThis;
		}
        

	    public override AttributeGen<T> BeginAttribute(AttributeType type)
		{
			return BeginAttribute(type, EmptyArray<object>.Instance);
		}
        
	    public override AttributeGen<T> BeginAttribute(AttributeType type, params object[] args)
		{
			return AttributeGen<T>.CreateAndAdd(TypedThis, ref _customAttributes, AttributeTarget, type, args, TypeMapper);
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

			AttributeGen.ApplyList(ref _customAttributes, SetCustomAttribute);
		}

		#endregion

		#region IMemberInfo Members

		MemberInfo IMemberInfo.Member => Member;

	    string IMemberInfo.Name => Name;

	    Type IMemberInfo.ReturnType => ReturnType;

	    Type[] IMemberInfo.ParameterTypes => ParameterTypes;

	    bool IMemberInfo.IsParameterArray => ParameterCount > 0 && Parameters[ParameterCount - 1].IsParameterArray;

	    bool IMemberInfo.IsStatic => IsStatic;

	    bool IMemberInfo.IsOverride => IsOverride;

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

		#region ICodeGenContext Members

		bool ICodeGenContext.SupportsScopes => true;
	    public StaticFactory StaticFactory => _context.StaticFactory;
	    public ExpressionFactory ExpressionFactory => _context.ExpressionFactory;

	    #endregion
	}
}
