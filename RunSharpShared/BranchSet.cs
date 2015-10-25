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

namespace TriAxis.RunSharp
{
	enum BranchInstruction
	{
		None, True, False, Eq, Ne, Lt, Gt, Le, Ge
	}

	sealed class BranchSet
	{
		public static readonly BranchSet Normal = new BranchSet(
			OpCodes.Brtrue, OpCodes.Brfalse, OpCodes.Beq, OpCodes.Bne_Un,
			OpCodes.Blt, OpCodes.Blt_Un, OpCodes.Bgt, OpCodes.Bgt_Un,
			OpCodes.Ble, OpCodes.Ble_Un, OpCodes.Bge, OpCodes.Bge_Un);

		public static readonly BranchSet Inverse = new BranchSet(
			OpCodes.Brfalse, OpCodes.Brtrue, OpCodes.Bne_Un, OpCodes.Beq,
			OpCodes.Bge, OpCodes.Bge_Un, OpCodes.Ble, OpCodes.Ble_Un,
			OpCodes.Bgt, OpCodes.Bgt_Un, OpCodes.Blt, OpCodes.Blt_Un);

		public readonly OpCode BrTrue, BrFalse, BrEq, BrNe, BrLt, BrLtUn, BrGt, BrGtUn, BrLe, BrLeUn, BrGe, BrGeUn;

		public OpCode Get(BranchInstruction ins, bool unsigned)
		{
			switch (ins)
			{
				case BranchInstruction.True: return BrTrue;
				case BranchInstruction.False: return BrFalse;
				case BranchInstruction.Eq: return BrEq;
				case BranchInstruction.Ne: return BrNe;
				case BranchInstruction.Lt: return unsigned ? BrLtUn : BrLt;
				case BranchInstruction.Gt: return unsigned ? BrGtUn : BrGt;
				case BranchInstruction.Le: return unsigned ? BrLeUn : BrLe;
				case BranchInstruction.Ge: return unsigned ? BrGeUn : BrGe;
				default:
					throw new NotSupportedException();
			}
		}

		private BranchSet(OpCode brTrue, OpCode brFalse, OpCode brEq, OpCode brNe, OpCode brLt, OpCode brLtUn, OpCode brGt, OpCode brGtUn, OpCode brLe, OpCode brLeUn, OpCode brGe, OpCode brGeUn)
		{
			BrTrue = brTrue; BrFalse = brFalse;
			BrEq = brEq; BrNe = brNe;
			BrLt = brLt; BrLtUn = brLtUn;
			BrGt = brGt; BrGtUn = brGtUn;
			BrLe = brLe; BrLeUn = brLeUn;
			BrGe = brGe; BrGeUn = brGeUn;
		}
	}
}
