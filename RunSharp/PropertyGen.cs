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
	public sealed class PropertyGen : Operand, IMemberInfo
	{
		TypeGen owner;
		MethodAttributes attrs;
		Type type;
		string name;
		Type[] indexTypes;
		PropertyBuilder pb;

		MethodGen getter, setter;

		internal PropertyGen(TypeGen owner, MethodAttributes attrs, Type type, string name, Type[] indexTypes)
		{
			this.owner = owner;
			this.attrs = attrs;
			this.type = type;
			this.name = name;
			this.indexTypes = indexTypes;
			pb = owner.TypeBuilder.DefineProperty(name, PropertyAttributes.None, type, indexTypes);
		}

		public MethodGen Getter()
		{
			if (getter == null)
			{
				getter = new MethodGen(owner, "get_" + name, attrs | MethodAttributes.SpecialName, type, indexTypes, 0);
				pb.SetGetMethod(getter.MethodBuilder);
			}

			return getter;
		}

		public MethodGen Setter()
		{
			if (setter == null)
			{
				setter = new MethodGen(owner, "set_" + name, attrs | MethodAttributes.SpecialName, typeof(void), ArrayUtils.Combine(indexTypes, type), 0);
				pb.SetSetMethod(setter.MethodBuilder);
			}

			return setter;
		}

		public bool IsAbstract { get { return (attrs & MethodAttributes.Abstract) != 0; } }
		public bool IsOverride { get { return Utils.IsOverride(attrs); } }
		public bool IsStatic { get { return (attrs & MethodAttributes.Static) != 0; } }

		public string Name { get { return name; } }

		internal override void EmitGet(CodeGen g)
		{
			if (getter == null)
				base.EmitGet(g);

			if (indexTypes.Length != 0)
				throw new InvalidOperationException(Properties.Messages.ErrMissingPropertyIndex);

			if (!IsStatic && (g.Context.IsStatic || g.Context.OwnerType != owner.TypeBuilder))
				throw new InvalidOperationException(Properties.Messages.ErrInvalidPropertyContext);

			g.IL.Emit(OpCodes.Ldarg_0);
			g.EmitCallHelper(getter.MethodBuilder, null);
		}

		internal override void EmitSet(CodeGen g, Operand value, bool allowExplicitConversion)
		{
			if (setter == null)
				base.EmitSet(g, value, allowExplicitConversion);

			if (indexTypes.Length != 0)
				throw new InvalidOperationException(Properties.Messages.ErrMissingPropertyIndex);

			if (!IsStatic && (g.Context.IsStatic || g.Context.OwnerType != owner.TypeBuilder))
				throw new InvalidOperationException(Properties.Messages.ErrInvalidPropertyContext);

			g.IL.Emit(OpCodes.Ldarg_0);
			g.EmitGetHelper(value, Type, allowExplicitConversion);
			g.EmitCallHelper(setter.MethodBuilder, null);
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
			get { return indexTypes; }
		}

		bool IMemberInfo.IsParameterArray
		{
			// TODO
			get { return false; }
		}

		#endregion
	}
}
