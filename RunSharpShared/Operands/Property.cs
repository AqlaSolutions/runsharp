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
	class Property : Operand
	{
	    readonly ApplicableFunction _property;
	    readonly Operand _target;
	    readonly Operand[] _indexes;

		public Property(ApplicableFunction property, Operand target, Operand[] indexes)
		{
			_property = property;
			_target = target;
			_indexes = indexes;
		}

		internal override void EmitGet(CodeGen g)
		{
			PropertyInfo pi = (PropertyInfo)_property.Method.Member;
			MethodInfo mi = pi.GetGetMethod(true);

			if (mi == null)
			{
				base.EmitGet(g);
				return;
			}

			if (!mi.IsStatic)
				_target.EmitRef(g);

			_property.EmitArgs(g, _indexes);
			g.EmitCallHelper(mi, _target);
		}

		internal override void EmitSet(CodeGen g, Operand value, bool allowExplicitConversion)
		{
			PropertyInfo pi = (PropertyInfo)_property.Method.Member;
			MethodInfo mi = pi.GetSetMethod(true);

			if (mi == null)
			{
				base.EmitSet(g, value, allowExplicitConversion);
				return;
			}

			if (!mi.IsStatic)
				_target.EmitRef(g);

			_property.EmitArgs(g, _indexes);
			g.EmitGetHelper(value, GetReturnType(g.TypeMapper), allowExplicitConversion);
			g.EmitCallHelper(mi, _target);
		}

	    public override Type GetReturnType(ITypeMapper typeMapper) => _property.Method.ReturnType;
	}
}
