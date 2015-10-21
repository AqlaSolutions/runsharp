/*
 * Copyright (c) 2015, Stefan Simek, Vladyslav Taranov
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
		static readonly IMemberInfo[] _stdPlusOperators = {
			UnaryOp<int>.Instance, UnaryOp<uint>.Instance, UnaryOp<long>.Instance, UnaryOp<ulong>.Instance, 
			UnaryOp<float>.Instance, UnaryOp<double>.Instance
		};

		static readonly IMemberInfo[] _stdMinusOperators = {
			UnaryOp<int>.Instance, UnaryOp<long>.Instance, UnaryOp<float>.Instance, UnaryOp<double>.Instance
		};

		static readonly IMemberInfo[] _stdNotOperators = {
			UnaryOp<int>.Instance, UnaryOp<uint>.Instance, UnaryOp<long>.Instance, UnaryOp<ulong>.Instance,
		};

		static readonly SpecificOperatorProvider[] _stdNotTemplates = {
			UnaryEnumSpecific
		};

		static readonly IMemberInfo[] _stdUnaryBoolOperators = {
			UnaryOp<bool>.Instance
		};

		static readonly IMemberInfo[] _stdIncOperators = {
			IncOp<sbyte>.Instance, IncOp<byte>.Instance, IncOp<short>.Instance, IncOp<ushort>.Instance,
			IncOp<int>.Instance, IncOp<uint>.Instance, IncOp<long>.Instance, IncOp<ulong>.Instance,
			IncOp<char>.Instance, IncOp<float>.Instance, IncOp<double>.Instance
		};

		static readonly SpecificOperatorProvider[] _stdIncTemplates = {
			IncEnumSpecific
		};

		static readonly IMemberInfo[] _stdAddOperators = {
			SameOp<int>.Instance, SameOp<uint>.Instance, SameOp<long>.Instance, SameOp<ulong>.Instance, SameOp<float>.Instance, SameOp<double>.Instance,
			StringConcatOp<string, string>.Instance, StringConcatOp<string, object>.Instance, StringConcatOp<object, string>.Instance
		};

		static readonly SpecificOperatorProvider[] _stdAddTemplates = {
			AddEnumSpecific, AddDelegateSpecific
		};

		static readonly IMemberInfo[] _stdSubOperators = {
			SameOp<int>.Instance, SameOp<uint>.Instance, SameOp<long>.Instance, SameOp<ulong>.Instance, SameOp<float>.Instance, SameOp<double>.Instance
		};

		static readonly SpecificOperatorProvider[] _stdSubTemplates = {
			SubEnumSpecific, SubDelegateSpecific
		};

		static readonly IMemberInfo[] _stdArithOperators = {
			SameOp<int>.Instance, SameOp<uint>.Instance, SameOp<long>.Instance, SameOp<ulong>.Instance, SameOp<float>.Instance, SameOp<double>.Instance
		};

		static readonly IMemberInfo[] _stdBitOperators = {
			SameOp<bool>.Instance, SameOp<int>.Instance, SameOp<uint>.Instance, SameOp<long>.Instance, SameOp<ulong>.Instance
		};

		static readonly SpecificOperatorProvider[] _stdBitTemplates = {
			BitEnumSpecific
		};

		static readonly IMemberInfo[] _stdShiftOperators = {
			ShiftOp<int>.Instance, ShiftOp<uint>.Instance, ShiftOp<long>.Instance, ShiftOp<ulong>.Instance
		};

		static readonly IMemberInfo[] _stdEqOperators = {
			CmpOp<bool>.Instance, CmpOp<int>.Instance, CmpOp<uint>.Instance, CmpOp<long>.Instance, CmpOp<ulong>.Instance, CmpOp<float>.Instance, CmpOp<double>.Instance,
			CmpOp<object>.Instance
		};

		static readonly SpecificOperatorProvider[] _stdEqTemplates = {
			CmpEnumSpecific
		};

		static readonly IMemberInfo[] _stdCmpOperators = {
			CmpOp<int>.Instance, CmpOp<uint>.Instance, CmpOp<long>.Instance, CmpOp<ulong>.Instance, CmpOp<float>.Instance, CmpOp<double>.Instance
		};

		static readonly SpecificOperatorProvider[] _stdCmpTemplates = {
			CmpEnumSpecific
		};

		static readonly IMemberInfo[] _stdNone = { };

		sealed class UnaryOp<T> : StdOp
		{
			public static readonly UnaryOp<T> Instance = new UnaryOp<T>();
			private UnaryOp() : base(typeof(T), typeof(T)) { }
		}

		static IMemberInfo[] UnaryEnumSpecific(Operand[] args, ITypeMapper typeMapper)
		{
			Type t = Operand.GetType(args[0], typeMapper);

			if (t == null || !t.IsEnum)
				return _stdNone;

			return new IMemberInfo[] { new StdOp(t, t) };
		}

		sealed class IncOp<T> : IncOp
		{
			public static readonly IncOp<T> Instance = new IncOp<T>();
			private IncOp() : base(typeof(T)) { }
		}

		class IncOp : StdOp
		{
		    readonly OpCode _convCode;

			public IncOp(Type t) : base(t, t)
			{
				switch (Type.GetTypeCode(t))
				{
					case TypeCode.Single:
						_convCode = OpCodes.Conv_R4;
						break;
					case TypeCode.Double:
						_convCode = OpCodes.Conv_R8;
						break;
					case TypeCode.Int64:
					case TypeCode.UInt64:
						_convCode = OpCodes.Conv_I8;
						break;
					default:
						_convCode = OpCodes.Nop;
						break;
				}
			}

			public override void Emit(CodeGen g, Operator op)
			{
				g.IL.Emit(OpCodes.Ldc_I4_1);
				if (_convCode != OpCodes.Nop)
					g.IL.Emit(_convCode);

				base.Emit(g, op);
			}
		}

		static IMemberInfo[] IncEnumSpecific(Operand[] args, ITypeMapper typeMapper)
		{
			Type t = Operand.GetType(args[0], typeMapper);

			if (t == null || !t.IsEnum)
				return _stdNone;

			return new IMemberInfo[] { new IncOp(t) };
		}

		sealed class SameOp<T> : StdOp
		{
			public static readonly SameOp<T> Instance = new SameOp<T>();
			private SameOp() : base(typeof(T), typeof(T), typeof(T)) { }
		}

		static IMemberInfo[] AddEnumSpecific(Operand[] args, ITypeMapper typeMapper)
		{
			Type t1 = Operand.GetType(args[0], typeMapper), t2 = Operand.GetType(args[1], typeMapper);

			if (t1 == null || t2 == null || t1.IsEnum == t2.IsEnum)	// if none or both types are enum, no operator can be valid
				return _stdNone;

			Type e = t1.IsEnum ? t1 : t2;

            Type u = Helpers.GetEnumEnderlyingType(e);
			return new IMemberInfo[] { new StdOp(e, e, u), new StdOp(e, u, e) };
		}

		static IMemberInfo[] AddDelegateSpecific(Operand[] args, ITypeMapper typeMapper)
		{
			Type t1 = Operand.GetType(args[0], typeMapper), t2 = Operand.GetType(args[1], typeMapper);

			if (t1 != t2 || t1 == null || !t1.IsSubclassOf(typeof(Delegate)))	// if the types are not the same, no operator can be valid
				return _stdNone;

			return new IMemberInfo[] { new DelegateCombineOp(t1) };
		}

		sealed class DelegateCombineOp : StdOp
		{
			public DelegateCombineOp(Type t) : base(t, t, t) { }
			static readonly MethodInfo _miCombine = typeof(Delegate).GetMethod("Combine", BindingFlags.Public | BindingFlags.Static, null, new Type[] { typeof(Delegate), typeof(Delegate) }, null);

			public override void Emit(CodeGen g, Operator op)
			{
				g.IL.Emit(OpCodes.Call, _miCombine);
				g.IL.Emit(OpCodes.Castclass, ReturnType);
			}
		}

		static IMemberInfo[] SubEnumSpecific(Operand[] args, ITypeMapper typeMapper)
		{
			Type t1 = Operand.GetType(args[0], typeMapper), t2 = Operand.GetType(args[1], typeMapper);

			if (t1 == null || t2 == null || !t1.IsEnum || (t2.IsEnum && t2 != t1))	// if the types are not the same, no operator can be valid
				return _stdNone;

			Type e = t1;
            Type u = Helpers.GetEnumEnderlyingType(e);
            return new IMemberInfo[] { new StdOp(u, e, e), new StdOp(e, e, u) };
		}

		static IMemberInfo[] SubDelegateSpecific(Operand[] args, ITypeMapper typeMapper)
		{
			Type t1 = Operand.GetType(args[0], typeMapper), t2 = Operand.GetType(args[1], typeMapper);

			if (t1 != t2 || t1 == null || !t1.IsSubclassOf(typeof(Delegate)))	// if the types are not the same, no operator can be valid
				return _stdNone;

			return new IMemberInfo[] { new DelegateRemoveOp(t1) };
		}

		sealed class DelegateRemoveOp : StdOp
		{
			public DelegateRemoveOp(Type t) : base(t, t, t) { }
			static readonly MethodInfo _miRemove = typeof(Delegate).GetMethod("Remove", BindingFlags.Public | BindingFlags.Static, null, new Type[] { typeof(Delegate), typeof(Delegate) }, null);

			public override void Emit(CodeGen g, Operator op)
			{
				g.IL.Emit(OpCodes.Call, _miRemove);
				g.IL.Emit(OpCodes.Castclass, ReturnType);
			}
		}

		sealed class StringConcatOp<T1, T2> : StdOp
		{
			public static readonly StringConcatOp<T1, T2> Instance = new StringConcatOp<T1, T2>();

		    readonly MethodInfo _method;
			private StringConcatOp() : base(typeof(string), typeof(T1), typeof(T2))
			{
				_method = typeof(string).GetMethod("Concat", BindingFlags.Public | BindingFlags.Static, null, ParameterTypes, null);
			}

			public override void Emit(CodeGen g, Operator op)
			{
				g.IL.Emit(OpCodes.Call, _method);
			}
		}

		static IMemberInfo[] BitEnumSpecific(Operand[] args, ITypeMapper typeMapper)
		{
			Type t1 = Operand.GetType(args[0], typeMapper), t2 = Operand.GetType(args[1], typeMapper);

			if (t1 != t2 || t1 == null || !t1.IsEnum)	// if both types are not the same enum, no operator can be valid
				return _stdNone;

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
				Unsigned = IsUnsigned(typeof(T));
			}
		}

		static IMemberInfo[] CmpEnumSpecific(Operand[] args, ITypeMapper typeMapper)
		{
			if ((object)args[0] == null || (object)args[1] == null)	// if any of the operands is null, it can't be an enum
				return _stdNone;

			Type t1 = args[0].GetReturnType(typeMapper), t2 = args[1].GetReturnType(typeMapper);

			if (t1 != t2 || t1 == null || !t1.IsEnum)	// if both types are not the same enum, no operator can be valid
				return _stdNone;

			return new IMemberInfo[] { new StdOp(typeof(bool), t1, t1) };
		}

		class StdOp : IMemberInfo, IStandardOperation
		{
		    protected bool Unsigned;

			public StdOp(Type returnType, params Type[] opTypes)
			{
				ReturnType = returnType; ParameterTypes = opTypes;
				Unsigned = IsUnsigned(returnType);
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

			bool IStandardOperation.IsUnsigned => Unsigned;

		    #region IMemberInfo Members

			public MemberInfo Member => null;

		    public string Name => null;

		    public Type ReturnType { get; }

		    public Type[] ParameterTypes { get; }

		    public bool IsParameterArray => false;

		    public bool IsStatic => true;

		    public bool IsOverride => false;

		    #endregion

#region IStandardOperation Members

			public virtual void Emit(CodeGen g, Operator op)
			{
				if (op.OpCode != OpCodes.Nop)
					g.IL.Emit(Unsigned ? op.OpCodeUn : op.OpCode);

				if (op.InvertOpResult)
				{
					g.IL.Emit(OpCodes.Ldc_I4_0);
					g.IL.Emit(OpCodes.Ceq);
				}
			}

#endregion
		}

		delegate IMemberInfo[] SpecificOperatorProvider(Operand[] args, ITypeMapper typeMapper);
        #endregion

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes", Justification = "The type is immutable")]
        public static readonly Operator Plus = new Operator(OpCodes.Nop, false, 0, "UnaryPlus", _stdPlusOperators, null);
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes", Justification = "The type is immutable")]
        public static readonly Operator Minus = new Operator(OpCodes.Neg, false, 0, "UnaryMinus", _stdMinusOperators, null);
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes", Justification = "The type is immutable")]
        public static readonly Operator LogicalNot = new Operator(OpCodes.Nop, true, BranchInstruction.False, "LogicalNot", _stdUnaryBoolOperators, null);
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes", Justification = "The type is immutable")]
        public static readonly Operator Not = new Operator(OpCodes.Not, false, 0, "OnesComplement", _stdNotOperators, _stdNotTemplates);
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes", Justification = "The type is immutable")]
        public static readonly Operator Increment = new Operator(OpCodes.Add, OpCodes.Add, OpCodes.Add_Ovf, OpCodes.Add_Ovf_Un, false, 0, "Increment", _stdIncOperators, _stdIncTemplates);
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes", Justification = "The type is immutable")]
        public static readonly Operator Decrement = new Operator(OpCodes.Sub, OpCodes.Sub, OpCodes.Sub_Ovf, OpCodes.Sub_Ovf_Un, false, 0, "Decrement", _stdIncOperators, _stdIncTemplates);
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes", Justification = "The type is immutable")]
        public static readonly Operator True = new Operator(OpCodes.Nop, false, BranchInstruction.True, "True", _stdUnaryBoolOperators, null);
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes", Justification = "The type is immutable")]
        public static readonly Operator False = new Operator(OpCodes.Nop, true, BranchInstruction.False, "False", _stdUnaryBoolOperators, null);

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes", Justification = "The type is immutable")]
        public static readonly Operator Add = new Operator(OpCodes.Add, OpCodes.Add, OpCodes.Add_Ovf, OpCodes.Add_Ovf_Un, false, 0, "Addition", _stdAddOperators, _stdAddTemplates);
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes", Justification = "The type is immutable")]
        public static readonly Operator Subtract = new Operator(OpCodes.Sub, OpCodes.Sub, OpCodes.Sub_Ovf, OpCodes.Sub_Ovf_Un, false, 0, "Subtraction", _stdSubOperators, _stdSubTemplates);
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes", Justification = "The type is immutable")]
        public static readonly Operator Multiply = new Operator(OpCodes.Mul, OpCodes.Mul, OpCodes.Mul_Ovf, OpCodes.Mul_Ovf_Un, false, 0, "Multiply", _stdArithOperators, null);
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes", Justification = "The type is immutable")]
        public static readonly Operator Divide = new Operator(OpCodes.Div, OpCodes.Div_Un, false, 0, "Division", _stdArithOperators, null);
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes", Justification = "The type is immutable")]
        public static readonly Operator Modulus = new Operator(OpCodes.Rem, OpCodes.Rem_Un, false, 0, "Modulus", _stdArithOperators, null);

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes", Justification = "The type is immutable")]
        public static readonly Operator And = new Operator(OpCodes.And, false, 0, "BitwiseAnd", _stdBitOperators, _stdBitTemplates);
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes", Justification = "The type is immutable")]
        public static readonly Operator Or = new Operator(OpCodes.Or, false, 0, "BitwiseOr", _stdBitOperators, _stdBitTemplates);
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes", Justification = "The type is immutable")]
        public static readonly Operator Xor = new Operator(OpCodes.Xor, false, 0, "ExclusiveOr", _stdBitOperators, _stdBitTemplates);

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes", Justification = "The type is immutable")]
        public static readonly Operator LeftShift = new Operator(OpCodes.Shl, false, 0, "LeftShift", _stdShiftOperators, null);
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes", Justification = "The type is immutable")]
        public static readonly Operator RightShift = new Operator(OpCodes.Shr, OpCodes.Shr_Un, false, 0, "RightShift", _stdShiftOperators, null);

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes", Justification = "The type is immutable")]
        public static readonly Operator Equality = new Operator(OpCodes.Ceq, false, BranchInstruction.Eq, "Equality", _stdEqOperators, _stdEqTemplates);
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes", Justification = "The type is immutable")]
        public static readonly Operator Inequality = new Operator(OpCodes.Ceq, true, BranchInstruction.Ne, "Inequality", _stdEqOperators, _stdEqTemplates);
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes", Justification = "The type is immutable")]
        public static readonly Operator LessThan = new Operator(OpCodes.Clt, OpCodes.Clt_Un, false, BranchInstruction.Lt, "LessThan", _stdCmpOperators, _stdCmpTemplates);
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes", Justification = "The type is immutable")]
        public static readonly Operator LessThanOrEqual = new Operator(OpCodes.Cgt, OpCodes.Cgt_Un, true, BranchInstruction.Le, "LessThanOrEqual", _stdCmpOperators, _stdCmpTemplates);
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes", Justification = "The type is immutable")]
        public static readonly Operator GreaterThan = new Operator(OpCodes.Cgt, OpCodes.Cgt_Un, false, BranchInstruction.Gt, "GreaterThan", _stdCmpOperators, _stdCmpTemplates);
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes", Justification = "The type is immutable")]
        public static readonly Operator GreaterThanOrEqual = new Operator(OpCodes.Clt, OpCodes.Clt_Un, true, BranchInstruction.Ge, "GreaterThanOrEqual", _stdCmpOperators, _stdCmpTemplates);

        internal readonly OpCode OpCode, OpCodeUn;
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields", Justification = "Prepared for future use")]
		internal readonly OpCode OpCodeChk, OpCodeChkUn;
		internal readonly bool InvertOpResult;
		internal readonly BranchInstruction BranchOp;
		internal readonly string MethodName;
		readonly IMemberInfo[] _standardCandidates;
		readonly SpecificOperatorProvider[] _standardTemplates;
	    
	    private Operator(OpCode opCode, bool invertOpResult, BranchInstruction branchOp, string methodName, IMemberInfo[] standardCandidates, SpecificOperatorProvider[] standardTemplates)
		{
			OpCode = OpCodeUn = OpCodeChk = OpCodeChkUn = opCode;
			InvertOpResult = invertOpResult;
			BranchOp = branchOp;
			MethodName = methodName;
			_standardCandidates = standardCandidates;
			_standardTemplates = standardTemplates;
		}

		private Operator(OpCode opCode, OpCode opCodeUn, bool invertOpResult, BranchInstruction branchOp, string methodName, IMemberInfo[] standardCandidates, SpecificOperatorProvider[] standardTemplates)
		{
			OpCode = OpCodeChk = opCode;
			OpCodeUn = OpCodeChkUn = opCodeUn;
			InvertOpResult = invertOpResult;
			BranchOp = branchOp;
			MethodName = methodName;
			_standardCandidates = standardCandidates;
			_standardTemplates = standardTemplates;
		}

		private Operator(OpCode opCode, OpCode opCodeUn, OpCode opCodeChk, OpCode opCodeChkUn, bool invertOpResult, BranchInstruction branchOp, string methodName, IMemberInfo[] standardCandidates, SpecificOperatorProvider[] standardTemplates)
		{
			OpCode = opCode;
			OpCodeUn = opCodeUn;
			OpCodeChk = opCodeChk;
			OpCodeChkUn = opCodeChkUn;
			InvertOpResult = invertOpResult;
			BranchOp = branchOp;
			MethodName = methodName;
			_standardCandidates = standardCandidates;
			_standardTemplates = standardTemplates;
		}

		internal IEnumerable<IMemberInfo> GetStandardCandidates(ITypeMapper typeMapper, params Operand[] args)
		{
			if (_standardTemplates == null)
				return _standardCandidates;
			else
				return GetStandardCandidatesT(args, typeMapper);
		}

		IEnumerable<IMemberInfo> GetStandardCandidatesT(Operand[] args, ITypeMapper typeMapper)
		{
			foreach (IMemberInfo op in _standardCandidates)
				yield return op;

			foreach (SpecificOperatorProvider tpl in _standardTemplates)
			{
				foreach (IMemberInfo inst in tpl(args, typeMapper))
					yield return inst;
			}
		}

		internal List<ApplicableFunction> FindUserCandidates(ITypeMapper typeMapper, params Operand[] args)
		{
			List<Type> usedTypes = new List<Type>();
			List<ApplicableFunction> candidates = null;
			string name = "op_" + MethodName;
			bool expandedCandidates = false;

			foreach (Operand arg in args)
			{
				for (Type t = Operand.GetType(arg, typeMapper); t != null && t != typeof(object) && (t.IsClass || t.IsValueType) && !usedTypes.Contains(t); t = t.IsValueType ? null : t.BaseType)
				{
					usedTypes.Add(t);

					OverloadResolver.FindApplicable(ref candidates, ref expandedCandidates, typeMapper.TypeInfo.Filter(typeMapper.TypeInfo.GetMethods(t), name, true, true, false), typeMapper, args);
				}
			}

			if (expandedCandidates)
				OverloadResolver.RemoveExpanded(candidates);

			return candidates;
		}
	}
}
