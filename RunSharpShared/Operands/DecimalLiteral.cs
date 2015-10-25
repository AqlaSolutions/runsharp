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
	class DecimalLiteral : Operand
	{
	    readonly decimal _value;

		public DecimalLiteral(decimal value) { _value = value; }

		internal override void EmitGet(CodeGen g)
		{
			int[] bits = decimal.GetBits(_value);
			byte exponent = unchecked((byte)((bits[3] >> 16) & 0x1f));
			bool sign = bits[3] < 0;

		    Type intType = g.TypeMapper.MapType(typeof(int));
		    if (exponent == 0 && bits[2] == 0)
			{
				if (bits[1] == 0 && (bits[0] > 0 || (bits[0] == 0 && !sign)))	// fits in int32 - use the basic int constructor
				{
					g.EmitI4Helper(sign ? -bits[0] : bits[0]);
					g.IL.Emit(OpCodes.Newobj, (ConstructorInfo)g.TypeMapper.MapType(typeof(decimal)).GetConstructor(new Type[] { intType }));
					return;
				}
				if (bits[1] > 0)	// fits in int64
				{
					long l = unchecked((long)(((ulong)(uint)bits[1] << 32) | (ulong)(uint)bits[0]));
					g.IL.Emit(OpCodes.Ldc_I8, sign ? -l : l);
					g.IL.Emit(OpCodes.Newobj, (ConstructorInfo)g.TypeMapper.MapType(typeof(decimal)).GetConstructor(new Type[] { g.TypeMapper.MapType(typeof(long)) }));
					return;
				}
			}

			g.EmitI4Helper(bits[0]);
			g.EmitI4Helper(bits[1]);
			g.EmitI4Helper(bits[2]);
			g.EmitI4Helper(sign ? 1 : 0);
			g.EmitI4Helper(exponent);
			g.IL.Emit(OpCodes.Newobj, (ConstructorInfo)g.TypeMapper.MapType(typeof(decimal)).GetConstructor(
                new Type[] { intType, intType, intType, g.TypeMapper.MapType(typeof(bool)), g.TypeMapper.MapType(typeof(byte)) }));
		}

	    public override Type GetReturnType(ITypeMapper typeMapper) => typeMapper.MapType(typeof(decimal));

	    internal override object ConstantValue => _value;
	}
}
