/*
Copyright(c) 2009, Stefan Simek
Copyright(c) 2016, Vladyslav Taranov

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
	class ArrayLength : Operand
	{
	    protected override bool DetectsLeaking => false;

	    readonly Operand _array;
	    readonly bool _asLong;

	    protected override void ResetLeakedStateRecursively()
	    {
	        base.ResetLeakedStateRecursively();
            _array.SetLeakedState(false);
        }

	    public ArrayLength(Operand array, bool asLong)
		{
			_array = array;
			_asLong = asLong;
		}

		internal override void EmitGet(CodeGen g) 
{
		    this.SetLeakedState(false); 
            if (!_array.GetReturnType(g.TypeMapper).IsArray)
                throw new InvalidOperationException(Properties.Messages.ErrArrayOnly);

            _array.EmitGet(g);

			if (_array.GetReturnType(g.TypeMapper).GetArrayRank() == 1 && (!_asLong || IntPtr.Size == 8))
			{
				g.IL.Emit(OpCodes.Ldlen);
				g.IL.Emit(_asLong ? OpCodes.Conv_I8 : OpCodes.Conv_I4);
				return;
			}

		    Type arrayType = g.TypeMapper.MapType(typeof(Array));
		    g.IL.Emit(OpCodes.Call, _asLong ? (MethodInfo)arrayType.GetProperty("LongLength").GetGetMethod() : (MethodInfo)arrayType.GetProperty("Length").GetGetMethod());
		}

	    public override Type GetReturnType(ITypeMapper typeMapper) => typeMapper.MapType(_asLong ? typeof(long) : typeof(int));
	}
}
