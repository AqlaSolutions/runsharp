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
	    readonly TypeGen _owner;
	    readonly MethodAttributes _attrs;
	    readonly Type _type;
	    readonly string _name;
	    readonly ParameterGenCollection _indexParameters = new ParameterGenCollection();
		PropertyBuilder _pb;
		Type _interfaceType;
		List<AttributeGen> _customAttributes;

		MethodGen _getter, _setter;

		internal PropertyGen(TypeGen owner, MethodAttributes attrs, Type type, string name)
		{
			this._owner = owner;
			this._attrs = attrs;
			this._type = type;
			this._name = name;
		}

		void LockSignature()
		{
			if (_pb == null)
			{
				_indexParameters.Lock();

				_pb = _owner.TypeBuilder.DefineProperty(_interfaceType == null ? _name : _interfaceType.FullName + "." + _name, PropertyAttributes.None, _type, _indexParameters.TypeArray);
				_owner.RegisterForCompletion(this);
			}
		}

		internal Type ImplementedInterface
		{
			get { return _interfaceType; }
			set { _interfaceType = value; }
		}

		public MethodGen Getter()
		{
			if (_getter == null)
			{
				LockSignature();
				_getter = new MethodGen(_owner, "get_" + _name, _attrs | MethodAttributes.SpecialName, _type, 0);
				_getter.ImplementedInterface = _interfaceType;
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
				_setter = new MethodGen(_owner, "set_" + _name, _attrs | MethodAttributes.SpecialName, typeof(void), 0);
				_setter.ImplementedInterface = _interfaceType;
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
			return AttributeGen<PropertyGen>.CreateAndAdd(this, ref _customAttributes, AttributeTargets.Property, type, args);
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

		public bool IsAbstract { get { return (_attrs & MethodAttributes.Abstract) != 0; } }
		public bool IsOverride { get { return Utils.IsOverride(_attrs); } }
		public bool IsStatic { get { return (_attrs & MethodAttributes.Static) != 0; } }

		public string Name { get { return _name; } }

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
			g.EmitGetHelper(value, Type, allowExplicitConversion);
			g.EmitCallHelper(_setter.GetMethodBuilder(), null);
		}

		public override Type Type { get { return _type; } }

		#region IMethodInfo Members

		MemberInfo IMemberInfo.Member
		{
			get { return _pb; }
		}

		Type IMemberInfo.ReturnType
		{
			get { return _type; }
		}

		Type[] IMemberInfo.ParameterTypes
		{
			get { return _indexParameters.TypeArray; }
		}

		bool IMemberInfo.IsParameterArray
		{
			get { return _indexParameters.Count > 0 && _indexParameters[_indexParameters.Count - 1].IsParameterArray; }
		}

		#endregion

		#region IDelayedCompletion Members

		void IDelayedCompletion.Complete()
		{
			AttributeGen.ApplyList(ref _customAttributes, _pb.SetCustomAttribute);
		}

		#endregion
	}
}
