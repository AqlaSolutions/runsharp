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
	public sealed class PropertyGen : Operand, IMemberInfo, IDelayedCompletion
	{
		TypeGen owner;
		MethodAttributes attrs;
		Type type;
		string name;
		ParameterGenCollection indexParameters = new ParameterGenCollection();
		PropertyBuilder pb;
		Type interfaceType;
		List<AttributeGen> customAttributes;

		MethodGen getter, setter;

		internal PropertyGen(TypeGen owner, MethodAttributes attrs, Type type, string name)
		{
			this.owner = owner;
			this.attrs = attrs;
			this.type = type;
			this.name = name;
		}

		void LockSignature()
		{
			if (pb == null)
			{
				indexParameters.Lock();

				pb = owner.TypeBuilder.DefineProperty(interfaceType == null ? name : interfaceType.FullName + "." + name, PropertyAttributes.None, type, indexParameters.TypeArray);
				owner.RegisterForCompletion(this);
			}
		}

		internal Type ImplementedInterface
		{
			get { return interfaceType; }
			set { interfaceType = value; }
		}

		public MethodGen Getter()
		{
			if (getter == null)
			{
				LockSignature();
				getter = new MethodGen(owner, "get_" + name, attrs | MethodAttributes.SpecialName, type, 0);
				getter.ImplementedInterface = interfaceType;
				getter.CopyParameters(indexParameters);
				pb.SetGetMethod(getter.GetMethodBuilder());
			}

			return getter;
		}

		public MethodGen Setter()
		{
			if (setter == null)
			{
				LockSignature();
				setter = new MethodGen(owner, "set_" + name, attrs | MethodAttributes.SpecialName, typeof(void), 0);
				setter.ImplementedInterface = interfaceType;
				setter.CopyParameters(indexParameters);
				setter.UncheckedParameter(type, "value");
				pb.SetSetMethod(setter.GetMethodBuilder());
			}

			return setter;
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
			return AttributeGen<PropertyGen>.CreateAndAdd(this, ref customAttributes, AttributeTargets.Property, type, args);
		}

		#endregion

		#region Index parameter definition
		public ParameterGen BeginIndex(Type type, string name)
		{
			ParameterGen pgen = new ParameterGen(indexParameters, indexParameters.Count + 1, type, 0, name, false);
			indexParameters.Add(pgen);
			return pgen;
		}

		public PropertyGen Index(Type type, string name)
		{
			BeginIndex(type, name);
			return this;
		}
		#endregion

		public bool IsAbstract { get { return (attrs & MethodAttributes.Abstract) != 0; } }
		public bool IsOverride { get { return Utils.IsOverride(attrs); } }
		public bool IsStatic { get { return (attrs & MethodAttributes.Static) != 0; } }

		public string Name { get { return name; } }

		internal override void EmitGet(CodeGen g)
		{
			if (getter == null)
				base.EmitGet(g);

			if (indexParameters.Count != 0)
				throw new InvalidOperationException(Properties.Messages.ErrMissingPropertyIndex);

			if (!IsStatic && (g.Context.IsStatic || g.Context.OwnerType != owner.TypeBuilder))
				throw new InvalidOperationException(Properties.Messages.ErrInvalidPropertyContext);

			g.IL.Emit(OpCodes.Ldarg_0);
			g.EmitCallHelper(getter.GetMethodBuilder(), null);
		}

		internal override void EmitSet(CodeGen g, Operand value, bool allowExplicitConversion)
		{
			if (setter == null)
				base.EmitSet(g, value, allowExplicitConversion);

			if (indexParameters.Count != 0)
				throw new InvalidOperationException(Properties.Messages.ErrMissingPropertyIndex);

			if (!IsStatic && (g.Context.IsStatic || g.Context.OwnerType != owner.TypeBuilder))
				throw new InvalidOperationException(Properties.Messages.ErrInvalidPropertyContext);

			g.IL.Emit(OpCodes.Ldarg_0);
			g.EmitGetHelper(value, Type, allowExplicitConversion);
			g.EmitCallHelper(setter.GetMethodBuilder(), null);
		}

		public override Type Type { get { return type; } }

		#region IMethodInfo Members

		MemberInfo IMemberInfo.Member
		{
			get { return pb; }
		}

		Type IMemberInfo.ReturnType
		{
			get { return type; }
		}

		Type[] IMemberInfo.ParameterTypes
		{
			get { return indexParameters.TypeArray; }
		}

		bool IMemberInfo.IsParameterArray
		{
			get { return indexParameters.Count > 0 && indexParameters[indexParameters.Count - 1].IsParameterArray; }
		}

		#endregion

		#region IDelayedCompletion Members

		void IDelayedCompletion.Complete()
		{
			AttributeGen.ApplyList(ref customAttributes, pb.SetCustomAttribute);
		}

		#endregion
	}
}
