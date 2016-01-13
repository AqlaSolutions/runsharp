/*
Copyright(c) 2009, Stefan Simek
Copyright(c) 2015, Vladyslav Taranov

MIT License

Permission is hereby granted, free of charge, to any person obtaining
a copy of this software and associated documentation files (the
"Software"), to deal in the Software without restriction, including
without limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to
permit persons to whom the Software is furnished to do so, subject to
the following conditions:

The above copyright notice and this permission notice shall be
included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/


#if !PHONE8

using System;
using System.Collections.Generic;
using System.Diagnostics;
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
	public sealed class FieldGen : Operand, IMemberInfo, IDelayedCompletion
	{
	    readonly TypeGen _owner;
	    readonly FieldAttributes _attrs;
	    readonly Type _type;
	    readonly FieldBuilder _fb;
		List<AttributeGen> _customAttributes = new List<AttributeGen>();

	    public ITypeMapper TypeMapper => _owner.TypeMapper;

		internal FieldGen(TypeGen owner, string name, Type type, FieldAttributes attrs)
		{
			_owner = owner;
			_attrs = attrs;
			Name = name;
			_type = type;

			_fb = owner.TypeBuilder.DefineField(name, type, attrs);
			owner.RegisterForCompletion(this);
		}

	    public override Type GetReturnType(ITypeMapper typeMapper) => _type;

	    public string Name { get; }
	    public bool IsStatic => (_attrs & FieldAttributes.Static) != 0;

        #region Custom Attributes

#if FEAT_IKVM

        public FieldGen Attribute(System.Type attributeType)
	    {
	        return Attribute(TypeMapper.MapType(attributeType));
	    }
        
#endif

	    public FieldGen Attribute(AttributeType type)
		{
			BeginAttribute(type);
			return this;
		}

#if FEAT_IKVM
        public FieldGen Attribute(System.Type attributeType, params object[] args)
	    {
	        return Attribute(TypeMapper.MapType(attributeType), args);
	    }
        
#endif

	    public FieldGen Attribute(AttributeType type, params object[] args)
		{
			BeginAttribute(type, args);
			return this;
		}

#if FEAT_IKVM

        public AttributeGen<FieldGen> BeginAttribute(System.Type attributeType)
	    {
	        return BeginAttribute(TypeMapper.MapType(attributeType));
	    }
        
#endif

	    public AttributeGen<FieldGen> BeginAttribute(AttributeType type)
		{
			return BeginAttribute(type, EmptyArray<object>.Instance);
		}

#if FEAT_IKVM

        public AttributeGen<FieldGen> BeginAttribute(System.Type attributeType, params object[] args)
	    {
	        return BeginAttribute(TypeMapper.MapType(attributeType), args);
	    }
        
#endif

	    public AttributeGen<FieldGen> BeginAttribute(AttributeType type, params object[] args)
		{
			return AttributeGen<FieldGen>.CreateAndAdd(this, ref _customAttributes, AttributeTargets.Field, type, args, _owner.TypeMapper);
		}

		#endregion

		internal override void EmitGet(CodeGen g)
		{
			if (!IsStatic)
			{
				if (g.Context.IsStatic || g.Context.OwnerType != _owner.TypeBuilder)
					throw new InvalidOperationException(Properties.Messages.ErrInvalidFieldContext);

				g.IL.Emit(OpCodes.Ldarg_0);
				g.IL.Emit(OpCodes.Ldfld, _fb);
			}
			else
				g.IL.Emit(OpCodes.Ldsfld, _fb);
		}

		internal override void EmitSet(CodeGen g, Operand value, bool allowExplicitConversion)
		{
			if (!IsStatic)
			{
				if (g.Context.IsStatic || g.Context.OwnerType != _owner.TypeBuilder)
					throw new InvalidOperationException(Properties.Messages.ErrInvalidFieldContext);

				g.IL.Emit(OpCodes.Ldarg_0);
			}

			g.EmitGetHelper(value, _type, allowExplicitConversion);
			g.IL.Emit(IsStatic ? OpCodes.Stsfld : OpCodes.Stfld, _fb);
		}

		internal override void EmitAddressOf(CodeGen g)
		{
			if (!IsStatic)
			{
				if (g.Context.IsStatic || g.Context.OwnerType != _owner.TypeBuilder)
					throw new InvalidOperationException(Properties.Messages.ErrInvalidFieldContext);

				g.IL.Emit(OpCodes.Ldarg_0);
				g.IL.Emit(OpCodes.Ldflda, _fb);
			}
			else
				g.IL.Emit(OpCodes.Ldsflda, _fb);
		}

		internal override bool TrivialAccess => true;

	    #region IMemberInfo Members

		MemberInfo IMemberInfo.Member => _fb;

	    Type IMemberInfo.ReturnType => _type;

	    Type[] IMemberInfo.ParameterTypes => Type.EmptyTypes;

	    bool IMemberInfo.IsParameterArray => false;

	    bool IMemberInfo.IsOverride => false;

	    #endregion

		#region IDelayedCompletion Members

		void IDelayedCompletion.Complete()
		{
			AttributeGen.ApplyList(ref _customAttributes, _fb.SetCustomAttribute);
		}

		#endregion
	}
}

#endif
