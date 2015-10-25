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
	class NewArray : Operand
	{
	    readonly Type _t;
	    readonly Operand[] _indexes;

		public NewArray(Type t, Operand[] indexes)
		{
			_t = t;
			_indexes = indexes;
		}

		internal override void EmitGet(CodeGen g)
		{
			for (int i = 0; i < _indexes.Length; i++)
				g.EmitGetHelper(_indexes[i], g.TypeMapper.MapType(typeof(int)), false);

			if (_indexes.Length == 1)
				g.IL.Emit(OpCodes.Newarr, _t);
			else
			{
				Type[] argTypes = new Type[_indexes.Length];
				for (int i = 0; i < argTypes.Length; i++)
					argTypes[i] = g.TypeMapper.MapType(typeof(int));

				ModuleBuilder mb = _t.Module as ModuleBuilder;

				if (mb != null)
					g.IL.Emit(OpCodes.Newobj, mb.GetArrayMethod(GetReturnType(g.TypeMapper), ".ctor", CallingConventions.HasThis, null, argTypes));
				else
					g.IL.Emit(OpCodes.Newobj, GetReturnType(g.TypeMapper).GetConstructor(argTypes));
			}
		}

	    public override Type GetReturnType(ITypeMapper typeMapper) => _indexes.Length == 1 ? _t.MakeArrayType() : _t.MakeArrayType(_indexes.Length);
	}
}
