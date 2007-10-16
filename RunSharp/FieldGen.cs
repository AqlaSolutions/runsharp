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
	public sealed class FieldGen : Operand, IMemberInfo
	{
		TypeGen owner;
		FieldAttributes attrs;
		string name;
		Type type;
		FieldBuilder fb;

		internal FieldGen(TypeGen owner, string name, Type type, FieldAttributes attrs)
		{
			this.owner = owner;
			this.attrs = attrs;
			this.name = name;
			this.type = type;

			fb = owner.TypeBuilder.DefineField(name, type, attrs);
		}

		public override Type Type
		{
			get
			{
				return type;
			}
		}

		public string Name { get { return name; } }
		public bool IsStatic { get { return (attrs & FieldAttributes.Static) != 0; } }

		internal override void EmitGet(CodeGen g)
		{
			if (!IsStatic)
			{
				if (g.Context.IsStatic || g.Context.OwnerType != owner.TypeBuilder)
					throw new InvalidOperationException(Properties.Messages.ErrInvalidFieldContext);

				g.IL.Emit(OpCodes.Ldarg_0);
				g.IL.Emit(OpCodes.Ldfld, fb);
			}
			else
				g.IL.Emit(OpCodes.Ldsfld, fb);
		}

		internal override void EmitSet(CodeGen g, Operand value, bool allowExplicitConversion)
		{
			if (!IsStatic)
			{
				if (g.Context.IsStatic || g.Context.OwnerType != owner.TypeBuilder)
					throw new InvalidOperationException(Properties.Messages.ErrInvalidFieldContext);

				g.IL.Emit(OpCodes.Ldarg_0);
			}

			g.EmitGetHelper(value, type, allowExplicitConversion);
			g.IL.Emit(IsStatic ? OpCodes.Stsfld : OpCodes.Stfld, fb);
		}

		internal override void EmitAddressOf(CodeGen g)
		{
			if (!IsStatic)
			{
				if (g.Context.IsStatic || g.Context.OwnerType != owner.TypeBuilder)
					throw new InvalidOperationException(Properties.Messages.ErrInvalidFieldContext);

				g.IL.Emit(OpCodes.Ldarg_0);
				g.IL.Emit(OpCodes.Ldflda, fb);
			}
			else
				g.IL.Emit(OpCodes.Ldsflda, fb);
		}

		internal override bool TrivialAccess
		{
			get
			{
				return true;
			}
		}

		#region IMemberInfo Members

		MemberInfo IMemberInfo.Member
		{
			get { return fb; }
		}

		Type IMemberInfo.ReturnType
		{
			get { return type; }
		}

		Type[] IMemberInfo.ParameterTypes
		{
			get { return Type.EmptyTypes; }
		}

		bool IMemberInfo.IsParameterArray
		{
			get { return false; }
		}

		bool IMemberInfo.IsOverride
		{
			get { return false; }
		}

		#endregion
	}
}
