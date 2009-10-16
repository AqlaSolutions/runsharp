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
	class Property : Operand
	{
		ApplicableFunction property;
		Operand target;
		Operand[] indexes;

		public Property(ApplicableFunction property, Operand target, Operand[] indexes)
		{
			this.property = property;
			this.target = target;
			this.indexes = indexes;
		}

		internal override void EmitGet(CodeGen g)
		{
			PropertyInfo pi = (PropertyInfo)property.Method.Member;
			MethodInfo mi = pi.GetGetMethod(true);

			if (mi == null)
			{
				base.EmitGet(g);
				return;
			}

			if (!mi.IsStatic)
				target.EmitRef(g);

			property.EmitArgs(g, indexes);
			g.EmitCallHelper(mi, target);
		}

		internal override void EmitSet(CodeGen g, Operand value, bool allowExplicitConversion)
		{
			PropertyInfo pi = (PropertyInfo)property.Method.Member;
			MethodInfo mi = pi.GetSetMethod(true);

			if (mi == null)
			{
				base.EmitSet(g, value, allowExplicitConversion);
				return;
			}

			if (!mi.IsStatic)
				target.EmitRef(g);

			property.EmitArgs(g, indexes);
			g.EmitGetHelper(value, Type, allowExplicitConversion);
			g.EmitCallHelper(mi, target);
		}

		public override Type Type
		{
			get
			{
				return property.Method.ReturnType;
			}
		}
	}
}
