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
	interface IStandardOperation
	{
		void Emit(CodeGen g, Operator op);
		bool IsUnsigned { get; }
	}

	[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords", Justification = "Other name would be unclear")]
	public sealed class Operator
	{
		internal delegate IEnumerable<IMemberInfo> StandardCandidateProvider(Operand[] operands);

		#region Standard operations
		static IMemberInfo[] stdPlusOperators = {
			UnaryOp<int>.Instance, UnaryOp<uint>.Instance, UnaryOp<long>.Instance, UnaryOp<ulong>.Instance, 
			UnaryOp<float>.Instance, UnaryOp<double>.Instance
		};

		static IMemberInfo[] stdMinusOperators = {
			UnaryOp<int>.Instance, UnaryOp<long>.Instance, UnaryOp<float>.Instance, UnaryOp<double>.Instance
		};

		static IMemberInfo[] stdNotOperators = {
			UnaryOp<int>.Instance, UnaryOp<uint>.Instance, UnaryOp<long>.Instance, UnaryOp<ulong>.Instance,
		};

		static SpecificOperatorProvider[] stdNotTemplates = {
			UnaryEnumSpecific
		};

		static IMemberInfo[] stdUnaryBoolOperators = {
			UnaryOp<bool>.Instance
		};

		static IMemberInfo[] stdIncOperators = {
			IncOp<sbyte>.Instance, IncOp<byte>.Instance, IncOp<short>.Instance, IncOp<ushort>.Instance,
			IncOp<int>.Instance, IncOp<uint>.Instance, IncOp<long>.Instance, IncOp<ulong>.Instance,
			IncOp<char>.Instance, IncOp<float>.Instance, IncOp<double>.Instance
		};

		static SpecificOperatorProvider[] stdIncTemplates = {
			IncEnumSpecific
		};

		static IMemberInfo[] stdAddOperators = {
			SameOp<int>.Instance, SameOp<uint>.Instance, SameOp<long>.Instance, SameOp<ulong>.Instance, SameOp<float>.Instance, SameOp<double>.Instance,
			StringConcatOp<string, string>.Instance, StringConcatOp<string, object>.Instance, StringConcatOp<object, string>.Instance
		};

		static SpecificOperatorProvider[] stdAddTemplates = {
			AddEnumSpecific, AddDelegateSpecific
		};

		static IMemberInfo[] stdSubOperators = {
			SameOp<int>.Instance, SameOp<uint>.Instance, SameOp<long>.Instance, SameOp<ulong>.Instance, SameOp<float>.Instance, SameOp<double>.Instance
		};

		static SpecificOperatorProvider[] stdSubTemplates = {
			SubEnumSpecific, SubDelegateSpecific
		};

		static IMemberInfo[] stdArithOperators = {
			SameOp<int>.Instance, SameOp<uint>.Instance, SameOp<long>.Instance, SameOp<ulong>.Instance, SameOp<float>.Instance, SameOp<double>.Instance
		};

		static IMemberInfo[] stdBitOperators = {
			SameOp<bool>.Instance, SameOp<int>.Instance, SameOp<uint>.Instance, SameOp<long>.Instance, SameOp<ulong>.Instance
		};

		static SpecificOperatorProvider[] stdBitTemplates = {
			BitEnumSpecific
		};

		static IMemberInfo[] stdShiftOperators = {
			ShiftOp<int>.Instance, ShiftOp<uint>.Instance, ShiftOp<long>.Instance, ShiftOp<ulong>.Instance
		};

		static IMemberInfo[] stdEqOperators = {
			CmpOp<bool>.Instance, CmpOp<int>.Instance, CmpOp<uint>.Instance, CmpOp<long>.Instance, CmpOp<ulong>.Instance, CmpOp<float>.Instance, CmpOp<double>.Instance,
			CmpOp<object>.Instance
		};

		static SpecificOperatorProvider[] stdEqTemplates = {
			CmpEnumSpecific
		};

		static IMemberInfo[] stdCmpOperators = {
			CmpOp<int>.Instance, CmpOp<uint>.Instance, CmpOp<long>.Instance, CmpOp<ulong>.Instance, CmpOp<float>.Instance, CmpOp<double>.Instance
		};

		static SpecificOperatorProvider[] stdCmpTemplates = {
			CmpEnumSpecific
		};

		static IMemberInfo[] stdNone = { };

		sealed class UnaryOp<T> : StdOp
		{
			public static readonly UnaryOp<T> Instance = new UnaryOp<T>();
			private UnaryOp() : base(typeof(T), typeof(T)) { }
		}

		static IMemberInfo[] UnaryEnumSpecific(Operand[] args)
		{
			Type t = Operand.GetType(args[0]);

			if (t == null || !t.IsEnum)
				return stdNone;

			return new IMemberInfo[] { new StdOp(t, t) };
		}

		sealed class IncOp<T> : IncOp
		{
			public static readonly IncOp<T> Instance = new IncOp<T>();
			private IncOp() : base(typeof(T)) { }
		}

		class IncOp : StdOp
		{
			OpCode convCode;

			public IncOp(Type t) : base(t, t)
			{
				switch (Type.GetTypeCode(t))
				{
					case TypeCode.Single:
						convCode = OpCodes.Conv_R4;
						break;
					case TypeCode.Double:
						convCode = OpCodes.Conv_R8;
						break;
					case TypeCode.Int64:
					case TypeCode.UInt64:
						convCode = OpCodes.Conv_I8;
						break;
					default:
						convCode = OpCodes.Nop;
						break;
				}
			}

			public override void Emit(CodeGen g, Operator op)
			{
				g.IL.Emit(OpCodes.Ldc_I4_1);
				if (convCode != OpCodes.Nop)
					g.IL.Emit(convCode);

				base.Emit(g, op);
			}
		}

		static IMemberInfo[] IncEnumSpecific(Operand[] args)
		{
			Type t = Operand.GetType(args[0]);

			if (t == null || !t.IsEnum)
				return stdNone;

			return new IMemberInfo[] { new IncOp(t) };
		}

		sealed class SameOp<T> : StdOp
		{
			public static readonly SameOp<T> Instance = new SameOp<T>();
			private SameOp() : base(typeof(T), typeof(T), typeof(T)) { }
		}

		static IMemberInfo[] AddEnumSpecific(Operand[] args)
		{
			Type t1 = Operand.GetType(args[0]), t2 = Operand.GetType(args[1]);

			if (t1 == null || t2 == null || t1.IsEnum == t2.IsEnum)	// if none or both types are enum, no operator can be valid
				return stdNone;

			Type e = t1.IsEnum ? t1 : t2;
			Type u = Enum.GetUnderlyingType(e);

			return new IMemberInfo[] { new StdOp(e, e, u), new StdOp(e, u, e) };
		}

		static IMemberInfo[] AddDelegateSpecific(Operand[] args)
		{
			Type t1 = Operand.GetType(args[0]), t2 = Operand.GetType(args[1]);

			if (t1 != t2 || t1 == null || !t1.IsSubclassOf(typeof(Delegate)))	// if the types are not the same, no operator can be valid
				return stdNone;

			return new IMemberInfo[] { new DelegateCombineOp(t1) };
		}

		sealed class DelegateCombineOp : StdOp
		{
			public DelegateCombineOp(Type t) : base(t, t, t) { }
			static MethodInfo miCombine = typeof(Delegate).GetMethod("Combine", BindingFlags.Public | BindingFlags.Static, null, new Type[] { typeof(Delegate), typeof(Delegate) }, null);

			public override void Emit(CodeGen g, Operator op)
			{
				g.IL.Emit(OpCodes.Call, miCombine);
				g.IL.Emit(OpCodes.Castclass, ReturnType);
			}
		}

		static IMemberInfo[] SubEnumSpecific(Operand[] args)
		{
			Type t1 = Operand.GetType(args[0]), t2 = Operand.GetType(args[1]);

			if (t1 == null || t2 == null || !t1.IsEnum || (t2.IsEnum && t2 != t1))	// if the types are not the same, no operator can be valid
				return stdNone;

			Type e = t1;
			Type u = Enum.GetUnderlyingType(e);

			return new IMemberInfo[] { new StdOp(u, e, e), new StdOp(e, e, u) };
		}

		static IMemberInfo[] SubDelegateSpecific(Operand[] args)
		{
			Type t1 = Operand.GetType(args[0]), t2 = Operand.GetType(args[1]);

			if (t1 != t2 || t1 == null || !t1.IsSubclassOf(typeof(Delegate)))	// if the types are not the same, no operator can be valid
				return stdNone;

			return new IMemberInfo[] { new DelegateRemoveOp(t1) };
		}

		sealed class DelegateRemoveOp : StdOp
		{
			public DelegateRemoveOp(Type t) : base(t, t, t) { }
			static MethodInfo miRemove = typeof(Delegate).GetMethod("Remove", BindingFlags.Public | BindingFlags.Static, null, new Type[] { typeof(Delegate), typeof(Delegate) }, null);

			public override void Emit(CodeGen g, Operator op)
			{
				g.IL.Emit(OpCodes.Call, miRemove);
				g.IL.Emit(OpCodes.Castclass, ReturnType);
			}
		}

		sealed class StringConcatOp<T1, T2> : StdOp
		{
			public static readonly StringConcatOp<T1, T2> Instance = new StringConcatOp<T1, T2>();

			MethodInfo method;
			private StringConcatOp() : base(typeof(string), typeof(T1), typeof(T2))
			{
				method = typeof(string).GetMethod("Concat", BindingFlags.Public | BindingFlags.Static, null, ParameterTypes, null);
			}

			public override void Emit(CodeGen g, Operator op)
			{
				g.IL.Emit(OpCodes.Call, method);
			}
		}

		static IMemberInfo[] BitEnumSpecific(Operand[] args)
		{
			Type t1 = Operand.GetType(args[0]), t2 = Operand.GetType(args[1]);

			if (t1 != t2 || t1 == null || !t1.IsEnum)	// if both types are not the same enum, no operator can be valid
				return stdNone;

			return new IMemberInfo[] { new StdOp(t1, t1, t1) };
		}

		sealed class ShiftOp<T> : StdOp
		{
			public static readonly ShiftOp<T> Instance = new ShiftOp<T>();
			private ShiftOp() : base(typeof(T), typeof(T), typeof(int)) { }

			public override void Emit(CodeGen g, Operator op)
			{
				base.Emit(g, op);
			}
		}

		sealed class CmpOp<T> : StdOp
		{
			public static readonly CmpOp<T> Instance = new CmpOp<T>();
			private CmpOp()
				: base(typeof(bool), typeof(T), typeof(T))
			{
				// unsigned is calculated from return type by default
				unsigned = IsUnsigned(typeof(T));
			}
		}

		static IMemberInfo[] CmpEnumSpecific(Operand[] args)
		{
			if ((object)args[0] == null || (object)args[1] == null)	// if any of the operands is null, it can't be an enum
				return stdNone;

			Type t1 = args[0].Type, t2 = args[1].Type;

			if (t1 != t2 || t1 == null || !t1.IsEnum)	// if both types are not the same enum, no operator can be valid
				return stdNone;

			return new IMemberInfo[] { new StdOp(typeof(bool), t1, t1) };
		}

		class StdOp : IMemberInfo, IStandardOperation
		{
			Type retType;
			Type[] opTypes;
			protected bool unsigned;

			public StdOp(Type returnType, params Type[] opTypes)
			{
				this.retType = returnType; this.opTypes = opTypes;
				unsigned = IsUnsigned(returnType);
			}

			protected static bool IsUnsigned(Type t)
			{
				switch (Type.GetTypeCode(t))
				{
					case TypeCode.Byte:
					case TypeCode.UInt16:
					case TypeCode.UInt32:
					case TypeCode.UInt64:
					case TypeCode.Char:
						return true;

					default:
						return false;
				}
			}

			bool IStandardOperation.IsUnsigned { get { return unsigned; } }

			#region IMemberInfo Members

			public System.Reflection.MemberInfo Member
			{
				get { return null; }
			}

			public string Name
			{
				get { return null; }
			}

			public Type ReturnType
			{
				get { return retType; }
			}

			public Type[] ParameterTypes
			{
				get { return opTypes; }
			}

			public bool IsParameterArray
			{
				get { return false; }
			}

			public bool IsStatic
			{
				get { return true; }
			}

			public bool IsOverride
			{
				get { return false; }
			}

			#endregion

			#region IStandardOperation Members

			public virtual void Emit(CodeGen g, Operator op)
			{
				if (op.opCode != OpCodes.Nop)
					g.IL.Emit(unsigned ? op.opCodeUn : op.opCode);

				if (op.invertOpResult)
				{
					g.IL.Emit(OpCodes.Ldc_I4_0);
					g.IL.Emit(OpCodes.Ceq);
				}
			}

			#endregion
		}

		delegate IMemberInfo[] SpecificOperatorProvider(Operand[] args);
		#endregion

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes", Justification = "The type is immutable")]
		public static readonly Operator Plus = new Operator(OpCodes.Nop, false, 0, "UnaryPlus", stdPlusOperators, null);
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes", Justification = "The type is immutable")]
		public static readonly Operator Minus = new Operator(OpCodes.Neg, false, 0, "UnaryMinus", stdMinusOperators, null);
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes", Justification = "The type is immutable")]
		public static readonly Operator LogicalNot = new Operator(OpCodes.Nop, true, BranchInstruction.False, "LogicalNot", stdUnaryBoolOperators, null);
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes", Justification = "The type is immutable")]
		public static readonly Operator Not = new Operator(OpCodes.Not, false, 0, "OnesComplement", stdNotOperators, stdNotTemplates);
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes", Justification = "The type is immutable")]
		public static readonly Operator Increment = new Operator(OpCodes.Add, OpCodes.Add, OpCodes.Add_Ovf, OpCodes.Add_Ovf_Un, false, 0, "Increment", stdIncOperators, stdIncTemplates);
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes", Justification = "The type is immutable")]
		public static readonly Operator Decrement = new Operator(OpCodes.Sub, OpCodes.Sub, OpCodes.Sub_Ovf, OpCodes.Sub_Ovf_Un, false, 0, "Decrement", stdIncOperators, stdIncTemplates);
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes", Justification = "The type is immutable")]
		public static readonly Operator True = new Operator(OpCodes.Nop, false, BranchInstruction.True, "True", stdUnaryBoolOperators, null);
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes", Justification = "The type is immutable")]
		public static readonly Operator False = new Operator(OpCodes.Nop, true, BranchInstruction.False, "False", stdUnaryBoolOperators, null);

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes", Justification = "The type is immutable")]
		public static readonly Operator Add = new Operator(OpCodes.Add, OpCodes.Add, OpCodes.Add_Ovf, OpCodes.Add_Ovf_Un, false, 0, "Addition", stdAddOperators, stdAddTemplates);
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes", Justification = "The type is immutable")]
		public static readonly Operator Subtract = new Operator(OpCodes.Sub, OpCodes.Sub, OpCodes.Sub_Ovf, OpCodes.Sub_Ovf_Un, false, 0, "Subtraction", stdSubOperators, stdSubTemplates);
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes", Justification = "The type is immutable")]
		public static readonly Operator Multiply = new Operator(OpCodes.Mul, OpCodes.Mul, OpCodes.Mul_Ovf, OpCodes.Mul_Ovf_Un, false, 0, "Multiply", stdArithOperators, null);
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes", Justification = "The type is immutable")]
		public static readonly Operator Divide = new Operator(OpCodes.Div, OpCodes.Div_Un, false, 0, "Division", stdArithOperators, null);
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes", Justification = "The type is immutable")]
		public static readonly Operator Modulus = new Operator(OpCodes.Rem, OpCodes.Rem_Un, false, 0, "Modulus", stdArithOperators, null);

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes", Justification = "The type is immutable")]
		public static readonly Operator And = new Operator(OpCodes.And, false, 0, "BitwiseAnd", stdBitOperators, stdBitTemplates);
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes", Justification = "The type is immutable")]
		public static readonly Operator Or = new Operator(OpCodes.Or, false, 0, "BitwiseOr", stdBitOperators, stdBitTemplates);
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes", Justification = "The type is immutable")]
		public static readonly Operator Xor = new Operator(OpCodes.Xor, false, 0, "ExclusiveOr", stdBitOperators, stdBitTemplates);

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes", Justification = "The type is immutable")]
		public static readonly Operator LeftShift = new Operator(OpCodes.Shl, false, 0, "LeftShift", stdShiftOperators, null);
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes", Justification = "The type is immutable")]
		public static readonly Operator RightShift = new Operator(OpCodes.Shr, OpCodes.Shr_Un, false, 0, "RightShift", stdShiftOperators, null);

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes", Justification = "The type is immutable")]
		public static readonly Operator Equality = new Operator(OpCodes.Ceq, false, BranchInstruction.Eq, "Equality", stdEqOperators, stdEqTemplates);
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes", Justification = "The type is immutable")]
		public static readonly Operator Inequality = new Operator(OpCodes.Ceq, true, BranchInstruction.Ne, "Inequality", stdEqOperators, stdEqTemplates);
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes", Justification = "The type is immutable")]
		public static readonly Operator LessThan = new Operator(OpCodes.Clt, OpCodes.Clt_Un, false, BranchInstruction.Lt, "LessThan", stdCmpOperators, stdCmpTemplates);
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes", Justification = "The type is immutable")]
		public static readonly Operator LessThanOrEqual = new Operator(OpCodes.Cgt, OpCodes.Cgt_Un, true, BranchInstruction.Le, "LessThanOrEqual", stdCmpOperators, stdCmpTemplates);
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes", Justification = "The type is immutable")]
		public static readonly Operator GreaterThan = new Operator(OpCodes.Cgt, OpCodes.Cgt_Un, false, BranchInstruction.Gt, "GreaterThan", stdCmpOperators, stdCmpTemplates);
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes", Justification = "The type is immutable")]
		public static readonly Operator GreaterThanOrEqual = new Operator(OpCodes.Clt, OpCodes.Clt_Un, true, BranchInstruction.Ge, "GreaterThanOrEqual", stdCmpOperators, stdCmpTemplates);

		internal readonly OpCode opCode, opCodeUn;
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields", Justification = "Prepared for future use")]
		internal readonly OpCode opCodeChk, opCodeChkUn;
		internal readonly bool invertOpResult;
		internal readonly BranchInstruction branchOp;
		internal readonly string methodName;
		readonly IMemberInfo[] standardCandidates;
		readonly SpecificOperatorProvider[] standardTemplates;

		private Operator(OpCode opCode, bool invertOpResult, BranchInstruction branchOp, string methodName, IMemberInfo[] standardCandidates, SpecificOperatorProvider[] standardTemplates)
		{
			this.opCode = this.opCodeUn = this.opCodeChk = this.opCodeChkUn = opCode;
			this.invertOpResult = invertOpResult;
			this.branchOp = branchOp;
			this.methodName = methodName;
			this.standardCandidates = standardCandidates;
			this.standardTemplates = standardTemplates;
		}

		private Operator(OpCode opCode, OpCode opCodeUn, bool invertOpResult, BranchInstruction branchOp, string methodName, IMemberInfo[] standardCandidates, SpecificOperatorProvider[] standardTemplates)
		{
			this.opCode = this.opCodeChk = opCode;
			this.opCodeUn = this.opCodeChkUn = opCodeUn;
			this.invertOpResult = invertOpResult;
			this.branchOp = branchOp;
			this.methodName = methodName;
			this.standardCandidates = standardCandidates;
			this.standardTemplates = standardTemplates;
		}

		private Operator(OpCode opCode, OpCode opCodeUn, OpCode opCodeChk, OpCode opCodeChkUn, bool invertOpResult, BranchInstruction branchOp, string methodName, IMemberInfo[] standardCandidates, SpecificOperatorProvider[] standardTemplates)
		{
			this.opCode = opCode;
			this.opCodeUn = opCodeUn;
			this.opCodeChk = opCodeChk;
			this.opCodeChkUn = opCodeChkUn;
			this.invertOpResult = invertOpResult;
			this.branchOp = branchOp;
			this.methodName = methodName;
			this.standardCandidates = standardCandidates;
			this.standardTemplates = standardTemplates;
		}

		internal IEnumerable<IMemberInfo> GetStandardCandidates(params Operand[] args)
		{
			if (standardTemplates == null)
				return standardCandidates;
			else
				return GetStandardCandidatesT(args);
		}

		IEnumerable<IMemberInfo> GetStandardCandidatesT(Operand[] args)
		{
			foreach (IMemberInfo op in standardCandidates)
				yield return op;

			foreach (SpecificOperatorProvider tpl in standardTemplates)
			{
				foreach (IMemberInfo inst in tpl(args))
					yield return inst;
			}
		}

		internal List<ApplicableFunction> FindUserCandidates(params Operand[] args)
		{
			List<Type> usedTypes = new List<Type>();
			List<ApplicableFunction> candidates = null;
			string name = "op_" + methodName;
			bool expandedCandidates = false;

			foreach (Operand arg in args)
			{
				for (Type t = Operand.GetType(arg); t != null && t != typeof(object) && (t.IsClass || t.IsValueType) && !usedTypes.Contains(t); t = t.IsValueType ? null : t.BaseType)
				{
					usedTypes.Add(t);

					OverloadResolver.FindApplicable(ref candidates, ref expandedCandidates, TypeInfo.Filter(TypeInfo.GetMethods(t), name, true, true, false), args);
				}
			}

			if (expandedCandidates)
				OverloadResolver.RemoveExpanded(candidates);

			return candidates;
		}
	}
}
