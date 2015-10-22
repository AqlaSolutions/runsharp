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

namespace TriAxis.RunSharp
{
	public sealed class PropertyGen : Operand, IMemberInfo, IDelayedCompletion
	{
	    public ITypeMapper TypeMapper => _owner.TypeMapper;
	    readonly TypeGen _owner;
	    readonly MethodAttributes _attrs;
	    readonly Type _type;
	    readonly ParameterGenCollection _indexParameters;
		PropertyBuilder _pb;
	    List<AttributeGen> _customAttributes;

		MethodGen _getter, _setter;

		internal PropertyGen(TypeGen owner, MethodAttributes attrs, Type type, string name)
		{
			_owner = owner;
			_attrs = attrs;
			_type = type;
			Name = name;
		    _indexParameters = new ParameterGenCollection(owner.TypeMapper);
		}

		void LockSignature()
		{
			if (_pb == null)
			{
				_indexParameters.Lock();

				_pb = _owner.TypeBuilder.DefineProperty(ImplementedInterface == null ? Name : ImplementedInterface.FullName + "." + Name, PropertyAttributes.None, _type, _indexParameters.TypeArray);
				_owner.RegisterForCompletion(this);
			}
		}

		internal Type ImplementedInterface { get; set; }

	    public MethodGen Getter()
		{
			if (_getter == null)
			{
				LockSignature();
				_getter = new MethodGen(_owner, "get_" + Name, _attrs | MethodAttributes.SpecialName, _type, 0);
				_getter.ImplementedInterface = ImplementedInterface;
				_getter.CopyParameters(_indexParameters);
				_pb.SetGetMethod(_getter.GetMethodBuilder());
			}

			return _getter;
		}

		public MethodGen Setter()
		{
			if (_setter == null)
			{
				LockSignature();
				_setter = new MethodGen(_owner, "set_" + Name, _attrs | MethodAttributes.SpecialName, TypeMapper.MapType(typeof(void)), 0);
				_setter.ImplementedInterface = ImplementedInterface;
				_setter.CopyParameters(_indexParameters);
				_setter.UncheckedParameter(_type, "value");
				_pb.SetSetMethod(_setter.GetMethodBuilder());
			}

			return _setter;
		}

		#region Custom Attributes

		public PropertyGen Attribute(AttributeType type)
		{
			BeginAttribute(type);
			return this;
		}

		public PropertyGen Attribute(AttributeType type, params object[] args)
		{
			BeginAttribute(type, args);
			return this;
		}

		public AttributeGen<PropertyGen> BeginAttribute(AttributeType type)
		{
			return BeginAttribute(type, EmptyArray<object>.Instance);
		}

		public AttributeGen<PropertyGen> BeginAttribute(AttributeType type, params object[] args)
		{
			return AttributeGen<PropertyGen>.CreateAndAdd(this, ref _customAttributes, AttributeTargets.Property, type, args, TypeMapper);
		}

		#endregion

		#region Index parameter definition
		public ParameterGen BeginIndex(Type type, string name)
		{
			ParameterGen pgen = new ParameterGen(_indexParameters, _indexParameters.Count + 1, type, 0, name, false);
			_indexParameters.Add(pgen);
			return pgen;
		}

		public PropertyGen Index(Type type, string name)
		{
			BeginIndex(type, name);
			return this;
		}
		#endregion

		public bool IsAbstract => (_attrs & MethodAttributes.Abstract) != 0;
	    public bool IsOverride => Utils.IsOverride(_attrs);
	    public bool IsStatic => (_attrs & MethodAttributes.Static) != 0;

	    public string Name { get; }

	    internal override void EmitGet(CodeGen g)
		{
			if (_getter == null)
				base.EmitGet(g);

			if (_indexParameters.Count != 0)
				throw new InvalidOperationException(Properties.Messages.ErrMissingPropertyIndex);

			if (!IsStatic && (g.Context.IsStatic || g.Context.OwnerType != _owner.TypeBuilder))
				throw new InvalidOperationException(Properties.Messages.ErrInvalidPropertyContext);

			g.IL.Emit(OpCodes.Ldarg_0);
			g.EmitCallHelper(_getter.GetMethodBuilder(), null);
		}

		internal override void EmitSet(CodeGen g, Operand value, bool allowExplicitConversion)
		{
			if (_setter == null)
				base.EmitSet(g, value, allowExplicitConversion);

			if (_indexParameters.Count != 0)
				throw new InvalidOperationException(Properties.Messages.ErrMissingPropertyIndex);

			if (!IsStatic && (g.Context.IsStatic || g.Context.OwnerType != _owner.TypeBuilder))
				throw new InvalidOperationException(Properties.Messages.ErrInvalidPropertyContext);

			g.IL.Emit(OpCodes.Ldarg_0);
			g.EmitGetHelper(value, GetReturnType(TypeMapper), allowExplicitConversion);
			g.EmitCallHelper(_setter.GetMethodBuilder(), null);
		}

	    public override Type GetReturnType(ITypeMapper typeMapper) => _type;

	    #region IMethodInfo Members

		MemberInfo IMemberInfo.Member => _pb;

	    Type IMemberInfo.ReturnType => _type;

	    Type[] IMemberInfo.ParameterTypes => _indexParameters.TypeArray;

	    bool IMemberInfo.IsParameterArray => _indexParameters.Count > 0 && _indexParameters[_indexParameters.Count - 1].IsParameterArray;

	    #endregion

		#region IDelayedCompletion Members

		void IDelayedCompletion.Complete()
		{
			AttributeGen.ApplyList(ref _customAttributes, _pb.SetCustomAttribute);
		}

		#endregion
	}
}
