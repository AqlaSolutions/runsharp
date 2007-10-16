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
	using Operands;

	partial class CodeGen
	{
		internal void EmitLdargHelper(ushort index)
		{
			OpCode opCode;

			switch (index)
			{
				case 0: opCode = OpCodes.Ldarg_0; break;
				case 1: opCode = OpCodes.Ldarg_1; break;
				case 2: opCode = OpCodes.Ldarg_2; break;
				case 3: opCode = OpCodes.Ldarg_3; break;
				default:
					if (index <= byte.MaxValue)
						il.Emit(OpCodes.Ldarg_S, (byte)index);
					else
						il.Emit(OpCodes.Ldarg, index);
					return;
			}

			il.Emit(opCode);
		}

		internal void EmitStargHelper(ushort index)
		{
			if (index <= byte.MaxValue)
				il.Emit(OpCodes.Starg_S, (byte)index);
			else
				il.Emit(OpCodes.Starg, index);
		}

		internal void EmitLdelemHelper(Type elementType)
		{
			OpCode op;

			if (elementType.IsPrimitive)
			{
				switch (Type.GetTypeCode(elementType))
				{
					case TypeCode.SByte:
					case TypeCode.Boolean:
						op = OpCodes.Ldelem_I1;
						break;

					case TypeCode.Byte:
						op = OpCodes.Ldelem_U1;
						break;

					case TypeCode.Int16:
						op = OpCodes.Ldelem_I2;
						break;

					case TypeCode.UInt16:
					case TypeCode.Char:
						op = OpCodes.Ldelem_U2;
						break;

					case TypeCode.Int32:
						op = OpCodes.Ldelem_I4;
						break;

					case TypeCode.UInt32:
						op = OpCodes.Ldelem_U4;
						break;

					case TypeCode.Int64:
					case TypeCode.UInt64:
						op = OpCodes.Ldelem_I8;
						break;

					case TypeCode.Single:
						op = OpCodes.Ldelem_R4;
						break;

					case TypeCode.Double:
						op = OpCodes.Ldelem_R8;
						break;

					default:
						throw new NotSupportedException();
				}
			}
			else if (elementType.IsValueType)
			{
				il.Emit(OpCodes.Ldelema, elementType);
				il.Emit(OpCodes.Ldobj, elementType);
				return;
			}
			else
				op = OpCodes.Ldelem_Ref;

			il.Emit(op);
		}

		internal static OpCode GetStelemOpCode(Type elementType)
		{
			if (elementType.IsPrimitive)
			{
				switch (Type.GetTypeCode(elementType))
				{
					case TypeCode.Byte:
					case TypeCode.SByte:
					case TypeCode.Boolean:
						return OpCodes.Stelem_I1;

					case TypeCode.Int16:
					case TypeCode.UInt16:
					case TypeCode.Char:
						return OpCodes.Stelem_I2;

					case TypeCode.Int32:
					case TypeCode.UInt32:
						return OpCodes.Stelem_I4;

					case TypeCode.Int64:
					case TypeCode.UInt64:
						return OpCodes.Stelem_I8;

					case TypeCode.Single:
						return OpCodes.Stelem_R4;

					case TypeCode.Double:
						return OpCodes.Stelem_R8;

					default:
						throw new NotSupportedException();
				}
			}
			else if (elementType.IsValueType)
				return OpCodes.Stobj;
			else
				return OpCodes.Stelem_Ref;
		}

		internal void EmitStelemHelper(Type elementType, Operand element, bool allowExplicitConversion)
		{
			OpCode op = GetStelemOpCode(elementType);

			if (op == OpCodes.Stobj)
				il.Emit(OpCodes.Ldelema, elementType);
			EmitGetHelper(element, elementType, allowExplicitConversion);
			if (op == OpCodes.Stobj)
				il.Emit(OpCodes.Stobj, elementType);
			else
				il.Emit(op);
		}

		internal void EmitLdindHelper(Type type)
		{
			OpCode op;

			if (type.IsPrimitive)
			{
				switch (Type.GetTypeCode(type))
				{
					case TypeCode.SByte:
					case TypeCode.Boolean:
						op = OpCodes.Ldind_I1;
						break;

					case TypeCode.Byte:
						op = OpCodes.Ldind_U1;
						break;

					case TypeCode.Int16:
						op = OpCodes.Ldind_I2;
						break;

					case TypeCode.UInt16:
					case TypeCode.Char:
						op = OpCodes.Ldind_U2;
						break;

					case TypeCode.Int32:
						op = OpCodes.Ldind_I4;
						break;

					case TypeCode.UInt32:
						op = OpCodes.Ldind_U4;
						break;

					case TypeCode.Int64:
					case TypeCode.UInt64:
						op = OpCodes.Ldind_I8;
						break;

					case TypeCode.Single:
						op = OpCodes.Ldind_R4;
						break;

					case TypeCode.Double:
						op = OpCodes.Ldind_R8;
						break;

					default:
						throw new NotSupportedException();
				}
			}
			else if (type.IsValueType)
			{
				il.Emit(OpCodes.Ldobj, type);
				return;
			}
			else
				op = OpCodes.Ldind_Ref;

			il.Emit(op);
		}

		internal static OpCode GetStindOpCode(Type type)
		{
			if (type.IsPrimitive)
			{
				switch (Type.GetTypeCode(type))
				{
					case TypeCode.Byte:
					case TypeCode.SByte:
					case TypeCode.Boolean:
						return OpCodes.Stind_I1;

					case TypeCode.Int16:
					case TypeCode.UInt16:
					case TypeCode.Char:
						return OpCodes.Stind_I2;

					case TypeCode.Int32:
					case TypeCode.UInt32:
						return OpCodes.Stind_I4;

					case TypeCode.Int64:
					case TypeCode.UInt64:
						return OpCodes.Stind_I8;

					case TypeCode.Single:
						return OpCodes.Stind_R4;

					case TypeCode.Double:
						return OpCodes.Stind_R8;

					default:
						throw new NotSupportedException();
				}
			}
			else if (type.IsValueType)
				return OpCodes.Stobj;
			else
				return OpCodes.Stind_Ref;
		}

		internal void EmitStindHelper(Type type, Operand value, bool allowExplicitConversion)
		{
			OpCode op = GetStindOpCode(type);

			EmitGetHelper(value, type, allowExplicitConversion);
			if (op == OpCodes.Stobj)
				il.Emit(OpCodes.Stobj, type);
			else
				il.Emit(op);
		}

		internal void EmitI4Helper(int value)
		{
			OpCode code;

			switch (value)
			{
				case 0: code = OpCodes.Ldc_I4_0; break;
				case 1: code = OpCodes.Ldc_I4_1; break;
				case 2: code = OpCodes.Ldc_I4_2; break;
				case 3: code = OpCodes.Ldc_I4_3; break;
				case 4: code = OpCodes.Ldc_I4_4; break;
				case 5: code = OpCodes.Ldc_I4_5; break;
				case 6: code = OpCodes.Ldc_I4_6; break;
				case 7: code = OpCodes.Ldc_I4_7; break;
				case 8: code = OpCodes.Ldc_I4_8; break;
				case -1: code = OpCodes.Ldc_I4_M1; break;
				default:
					if (value >= sbyte.MinValue && value <= sbyte.MaxValue)
						il.Emit(OpCodes.Ldc_I4_S, (sbyte)value);
					else
						il.Emit(OpCodes.Ldc_I4, value);
					return;
			}

			il.Emit(code);
		}

		internal void EmitI8Helper(long value, bool signed)
		{
			if (value >= int.MinValue && value <= uint.MaxValue)
			{
				EmitI4Helper((int)value);
				if (value < 0 && signed)
					il.Emit(OpCodes.Conv_I8);
				else
					il.Emit(OpCodes.Conv_U8);
			}
			else
				il.Emit(OpCodes.Ldc_I8, value);
		}

		internal void EmitConvHelper(TypeCode to)
		{
			OpCode op;

			switch (to)
			{
				case TypeCode.SByte:
					op = OpCodes.Conv_I1; break;
				case TypeCode.Byte:
					op = OpCodes.Conv_U1; break;
				case TypeCode.Int16:
					op = OpCodes.Conv_I2; break;
				case TypeCode.UInt16:
				case TypeCode.Char:
					op = OpCodes.Conv_U2; break;
				case TypeCode.Int32:
					op = OpCodes.Conv_I4; break;
				case TypeCode.UInt32:
					op = OpCodes.Conv_U4; break;
				case TypeCode.Int64:
					op = OpCodes.Conv_I8; break;
				case TypeCode.UInt64:
					op = OpCodes.Conv_U8; break;
				case TypeCode.Single:
					op = OpCodes.Conv_R4; break;
				case TypeCode.Double:
					op = OpCodes.Conv_R8; break;
				default:
					throw new NotSupportedException();
			}

			il.Emit(op);
		}

		internal void EmitGetHelper(Operand op, Type desiredType, bool allowExplicitConversion)
		{
			if (desiredType.IsByRef)
			{
				if (op.Type != desiredType.GetElementType())
					throw new InvalidOperationException(Properties.Messages.ErrByRefTypeMismatch);

				op.EmitAddressOf(this);
				return;
			}

			if ((object)op == null)
			{
				if (desiredType.IsValueType)
					throw new ArgumentNullException("op");
				il.Emit(OpCodes.Ldnull);
				return;
			}

			op.EmitGet(this);
			Convert(op, desiredType, allowExplicitConversion);
		}

		internal void EmitCallHelper(MethodBase mth, Operand target)
		{
			MethodInfo mi = mth as MethodInfo;
			if (mi != null)
			{
				il.Emit((((object)target != null && target.SuppressVirtual) || mi.IsStatic) ? OpCodes.Call : OpCodes.Callvirt, mi);
				return;
			}

			ConstructorInfo ci = mth as ConstructorInfo;
			if (ci != null)
			{
				il.Emit(OpCodes.Call, ci);
				return;
			}

			throw new ArgumentException(Properties.Messages.ErrInvalidMethodBase, "mth");
		}

		internal void Convert(Operand op, Type to, bool allowExplicit)
		{
			Conversion conv = allowExplicit ? Conversion.GetExplicit(op, to, false) : Conversion.GetImplicit(op, to, false);
			conv.Emit(this, (object)op == null ? null : op.Type, to);
		}
	}
}
