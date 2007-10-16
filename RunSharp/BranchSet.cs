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
using System.Reflection.Emit;

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

		public static readonly BranchSet Short = new BranchSet(
			OpCodes.Brtrue_S, OpCodes.Brfalse_S, OpCodes.Beq_S, OpCodes.Bne_Un_S, 
			OpCodes.Blt_S, OpCodes.Blt_Un_S, OpCodes.Bgt_S, OpCodes.Bgt_Un_S,
			OpCodes.Ble_S, OpCodes.Ble_Un_S, OpCodes.Bge_S, OpCodes.Bge_Un_S);

		public static readonly BranchSet Inverse = new BranchSet(
			OpCodes.Brfalse, OpCodes.Brtrue, OpCodes.Bne_Un, OpCodes.Beq,
			OpCodes.Bge, OpCodes.Bge_Un, OpCodes.Ble, OpCodes.Ble_Un,
			OpCodes.Bgt, OpCodes.Bgt_Un, OpCodes.Blt, OpCodes.Blt_Un);

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields", Justification = "Provided for symmetry, might be used in the future")]
		public static readonly BranchSet InverseShort = new BranchSet(
			OpCodes.Brfalse_S, OpCodes.Brtrue_S, OpCodes.Bne_Un_S, OpCodes.Beq_S,
			OpCodes.Bge_S, OpCodes.Bge_Un_S, OpCodes.Ble_S, OpCodes.Ble_Un_S,
			OpCodes.Bgt_S, OpCodes.Bgt_Un_S, OpCodes.Bge_S, OpCodes.Bge_Un_S);

		public readonly OpCode brTrue, brFalse, brEq, brNe, brLt, brLtUn, brGt, brGtUn, brLe, brLeUn, brGe, brGeUn;

		public OpCode Get(BranchInstruction ins, bool unsigned)
		{
			switch (ins)
			{
				case BranchInstruction.True: return brTrue;
				case BranchInstruction.False: return brFalse;
				case BranchInstruction.Eq: return brEq;
				case BranchInstruction.Ne: return brNe;
				case BranchInstruction.Lt: return unsigned ? brLtUn : brLt;
				case BranchInstruction.Gt: return unsigned ? brGtUn : brGt;
				case BranchInstruction.Le: return unsigned ? brLeUn : brLe;
				case BranchInstruction.Ge: return unsigned ? brGeUn : brGe;
				default:
					throw new NotSupportedException();
			}
		}

		private BranchSet(OpCode brTrue, OpCode brFalse, OpCode brEq, OpCode brNe, OpCode brLt, OpCode brLtUn, OpCode brGt, OpCode brGtUn, OpCode brLe, OpCode brLeUn, OpCode brGe, OpCode brGeUn)
		{
			this.brTrue = brTrue; this.brFalse = brFalse;
			this.brEq = brEq; this.brNe = brNe;
			this.brLt = brLt; this.brLtUn = brLtUn;
			this.brGt = brGt; this.brGtUn = brGtUn;
			this.brLe = brLe; this.brLeUn = brLeUn;
			this.brGe = brGe; this.brGeUn = brGeUn;
		}
	}
}
