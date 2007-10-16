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

	abstract class Conversion
	{
		public abstract void Emit(CodeGen g, Type from, Type to);
		public virtual bool IsAmbiguous { get { return false; } }
		public virtual bool IsValid { get { return true; } }

		const byte D = 0;	// direct conversion
		const byte I = 1;	// implicit conversion
		const byte E = 2;	// explicit conversion
		const byte X = 3;	// no conversion

		static byte[][] convTable = { // indexed by TypeCode [from,to]
			// FROM      TO:       NA,OB,DN,BL,CH,I1,U1,I2,U2,I4,U4,I8,U8,R4,R8,DC,DT,--,ST
			/* NA */ new byte[] { X, X, X, X, X, X, X, X, X, X, X, X, X, X, X, X, X, X, X },
			/* OB */ new byte[] { X, D, X, X, X, X, X, X, X, X, X, X, X, X, X, X, X, X, X },
			/* DN */ new byte[] { X, X, D, X, X, X, X, X, X, X, X, X, X, X, X, X, X, X, X },
			/* BL */ new byte[] { X, X, X, D, X, X, X, X, X, X, X, X, X, X, X, X, X, X, X },
			/* CH */ new byte[] { X, X, X, X, D, E, E, E, I, I, I, I, I, I, I, I, X, X, X },
			/* I1 */ new byte[] { X, X, X, X, E, D, E, I, E, I, E, I, E, I, I, I, X, X, X },
			/* U1 */ new byte[] { X, X, X, X, E, E, D, I, I, I, I, I, I, I, I, I, X, X, X },
			/* I2 */ new byte[] { X, X, X, X, E, E, E, D, E, I, E, I, E, I, I, I, X, X, X },
			/* U2 */ new byte[] { X, X, X, X, E, E, E, E, D, I, I, I, I, I, I, I, X, X, X },
			/* I4 */ new byte[] { X, X, X, X, E, E, E, E, E, D, E, I, E, I, I, I, X, X, X },
			/* U4 */ new byte[] { X, X, X, X, E, E, E, E, E, E, D, I, I, I, I, I, X, X, X },
			/* I8 */ new byte[] { X, X, X, X, E, E, E, E, E, E, E, D, E, I, I, I, X, X, X },
			/* U8 */ new byte[] { X, X, X, X, E, E, E, E, E, E, E, E, D, I, I, I, X, X, X },
			/* R4 */ new byte[] { X, X, X, X, E, E, E, E, E, E, E, E, E, D, I, E, X, X, X },
			/* R8 */ new byte[] { X, X, X, X, E, E, E, E, E, E, E, E, E, E, D, E, X, X, X },
			/* DC */ new byte[] { X, X, X, X, E, E, E, E, E, E, E, E, E, E, E, D, X, X, X },
			/* DT */ new byte[] { X, X, X, X, X, X, X, X, X, X, X, X, X, X, X, X, D, X, X },
			/* -- */ new byte[] { X, X, X, X, X, X, X, X, X, X, X, X, X, X, X, X, X, X, X },
			/* ST */ new byte[] { X, X, X, X, X, X, X, X, X, X, X, X, X, X, X, X, X, X, D },
		};

		delegate Conversion ConversionProvider(Operand op, Type to, bool onlyStandard);

		#region Conversions
		sealed class Direct : Conversion
		{
			public static readonly Direct Instance = new Direct();

			public override void Emit(CodeGen g, Type from, Type to)
			{
			}
		}

		sealed class Primitive : Conversion
		{
			public static readonly Primitive Instance = new Primitive();

			public override void Emit(CodeGen g, Type from, Type to)
			{
				g.EmitConvHelper(Type.GetTypeCode(to));
			}
		}

		sealed class Boxing : Conversion
		{
			public static readonly Boxing Instance = new Boxing();

			public override void Emit(CodeGen g, Type from, Type to)
			{
				g.IL.Emit(OpCodes.Box, from);
			}
		}

		sealed class Unboxing : Conversion
		{
			public static readonly Unboxing Instance = new Unboxing();

			public override void Emit(CodeGen g, Type from, Type to)
			{
				g.IL.Emit(OpCodes.Unbox_Any, to);
			}
		}

		sealed class Cast : Conversion
		{
			public static readonly Cast Instance = new Cast();

			public override void Emit(CodeGen g, Type from, Type to)
			{
				g.IL.Emit(OpCodes.Castclass, to);
			}
		}

		class Invalid : Conversion
		{
			public static readonly Invalid Instance = new Invalid();

			public override void Emit(CodeGen g, Type from, Type to)
			{
				throw new InvalidCastException(string.Format(null, Properties.Messages.ErrInvalidConversion, from == null ? "<null>" : from.FullName, to.FullName));
			}

			public override bool IsValid
			{
				get
				{
					return false;
				}
			}
		}

		class Ambiguous : Conversion
		{
			public static readonly Ambiguous Instance = new Ambiguous();

			public override void Emit(CodeGen g, Type from, Type to)
			{
				throw new AmbiguousMatchException(string.Format(null, Properties.Messages.ErrAmbiguousConversion, from.FullName, to.FullName));
			}

			public override bool IsAmbiguous
			{
				get
				{
					return true;
				}
			}

			public override bool IsValid
			{
				get
				{
					return false;
				}
			}
		}

		class UserDefined : Conversion
		{
			Conversion before, after;
			IMemberInfo method;
			Type fromType, toType;
			bool sxSubset, txSubset;

			public UserDefined(Conversion before, IMemberInfo method, Conversion after)
			{
				this.before = before;
				this.method = method;
				this.fromType = method.ParameterTypes[0];
				this.toType = method.ReturnType;
				this.after = after;
			}

			public override void Emit(CodeGen g, Type from, Type to)
			{
				before.Emit(g, from, fromType);
				g.IL.Emit(OpCodes.Call, (MethodInfo)method.Member);
				after.Emit(g, toType, to);
			}

			public static Conversion FindImplicit(List<UserDefined> collection, Type from, Type to)
			{
				Type sx = null, tx = null;
				bool any = false;

				for (int i = 0; i < collection.Count; i++)
				{
					UserDefined udc = collection[i];
					any = true;
					if (sx == null && udc.fromType == from)
						sx = from;
					if (tx == null && udc.toType == to)
						tx = to;
				}

				if (!any)
					return Invalid.Instance;

				if (sx == null || tx == null)
				{
					for (int i = 0; i < collection.Count; i++)
					{
						UserDefined udc = collection[i];
						bool sxMatch = sx == null, txMatch = tx == null;

						for (int j = 0; j < collection.Count; j++)
						{
							UserDefined udc2 = collection[j];
							if (udc2 == udc)
								continue;

							if (sxMatch && GetImplicit(udc.fromType, udc2.fromType, true) == null)
								sxMatch = false;

							if (txMatch && GetImplicit(udc2.toType, udc.toType, true) == null)
								txMatch = false;

							if (!(sxMatch || txMatch))
								break;
						}

						if (sxMatch)
							sx = udc.fromType;
						if (txMatch)
							tx = udc.toType;

						if (sx != null && tx != null)
							break;
					}
				}

				if (sx == null || tx == null)
					return Ambiguous.Instance;

				UserDefined match = null;

				for (int i = 0; i < collection.Count; i++)
				{
					UserDefined udc = collection[i];
					if (udc.fromType == sx && udc.toType == tx)
					{
						if (match != null)
							return Ambiguous.Instance;	// ambiguous match
						else
							match = udc;
					}
				}

				if (match == null)
					return Ambiguous.Instance;

				return match;
			}

			public static Conversion FindExplicit(List<UserDefined> collection, Type from, Type to)
			{
				Type sx = null, tx = null;
				bool sxSubset = false, txSubset = false;
				bool any = false;

				for (int i = 0; i < collection.Count; i++)
				{
					UserDefined udc = collection[i];
					any = true;

					if (sx == null && udc.fromType == from)
						sx = from;
					if (tx == null && udc.toType == to)
						tx = to;

					if (udc.sxSubset = GetImplicit(from, udc.fromType, true) != null)
						sxSubset = true;
					if (udc.txSubset = GetImplicit(udc.toType, to, true) != null)
						txSubset = true;
				}

				if (!any)
					return Invalid.Instance;

				if (sx == null || tx == null)
				{
					for (int i = 0; i < collection.Count; i++)
					{
						UserDefined udc = collection[i];
						bool sxMatch = sx == null && !sxSubset || udc.sxSubset;
						bool txMatch = tx == null && !txSubset || udc.txSubset;

						if (!(sxMatch || txMatch))
							continue;

						for (int j = 0; j < collection.Count; j++)
						{
							UserDefined udc2 = collection[j];
							if (udc2 == udc)
								continue;

							if (sxMatch)
							{
								if (sxSubset)
								{
									if (udc.sxSubset && GetImplicit(udc.fromType, udc2.fromType, true) == null)
										sxMatch = false;
								}
								else
								{
									if (GetImplicit(udc2.fromType, udc.fromType, true) == null)
										sxMatch = false;
								}
							}

							if (txMatch)
							{
								if (txSubset)
								{
									if (udc.txSubset && GetImplicit(udc2.toType, udc.toType, true) == null)
										txMatch = false;
								}
								else
								{
									if (GetImplicit(udc.toType, udc2.toType, true) == null)
										txMatch = false;
								}
							}

							if (!(sxMatch || txMatch))
								break;
						}

						if (sxMatch)
							sx = udc.fromType;
						if (txMatch)
							tx = udc.toType;

						if (sx != null && tx != null)
							break;
					}
				}

				if (sx == null || tx == null)
					return Ambiguous.Instance;

				UserDefined match = null;

				for (int i = 0; i < collection.Count; i++)
				{
					UserDefined udc = collection[i];
					if (udc.fromType == sx && udc.toType == tx)
					{
						if (match != null)
							return Ambiguous.Instance;	// ambiguous match
						else
							match = udc;
					}
				}

				if (match == null)
					return Ambiguous.Instance;
				
				return match;
			}
		}
		#endregion

		sealed class FakeTypedOperand : Operand
		{
			Type t;

			public FakeTypedOperand(Type t) { this.t = t; }

			public override Type Type
			{
				get
				{
					return t;
				}
			}
		}

		public static Conversion GetImplicit(Type from, Type to, bool onlyStandard)
		{
			return GetImplicit(new FakeTypedOperand(from), to, onlyStandard);
		}

		// the sections mentioned in comments of this method are from C# specification v1.2
		public static Conversion GetImplicit(Operand op, Type to, bool onlyStandard)
		{
			Type from = Operand.GetType(op);

			if (to.Equals(from))
				return Direct.Instance;

			// required for arrays created from TypeBuilder-s
			if (from != null && to.IsArray && from.IsArray)
			{
				if (to.GetArrayRank() == from.GetArrayRank())
				{
					if (to.GetElementType().Equals(from.GetElementType()))
						return Direct.Instance;
				}
			}

			TypeCode tcFrom = Type.GetTypeCode(from);
			TypeCode tcTo = Type.GetTypeCode(to);
			byte ct = convTable[(int)tcFrom][(int)tcTo];

			// section 6.1.2 - Implicit numeric conversions
			if ((from != null && (from.IsPrimitive || from == typeof(decimal))) && (to.IsPrimitive || to == typeof(decimal)))
			{
				if (ct <= I)
				{
					if (from == typeof(decimal) || to == typeof(decimal))
						// decimal is handled as user-defined conversion, but as it is a standard one, always enable UDC processing
						onlyStandard = false;
					else
						return Primitive.Instance;
				}
			}

			IntLiteral intLit = op as IntLiteral;

			// section 6.1.3 - Implicit enumeration conversions
			if (!onlyStandard && to.IsEnum && (object)intLit != null && intLit.Value == 0)
				return Primitive.Instance;

			// section 6.1.4 - Implicit reference conversions
			if ((from == null || !from.IsValueType) && !to.IsValueType)
			{
				if (from == null) // from the null type to any reference type
					return Direct.Instance;

				if (to.IsAssignableFrom(from))	// the rest
					return Direct.Instance;
			}

			if (from == null)	// no other conversion from null type is possible
				return Invalid.Instance;

			// section 6.1.5 - Boxing conversions
			if (from.IsValueType)
			{
				if (to.IsAssignableFrom(from))
					return Boxing.Instance;
			}

			// section 6.1.6 - Implicit constant expression conversions
			if ((object)intLit != null && from == typeof(int) && to.IsPrimitive)
			{
				int val = intLit.Value;

				switch (tcTo)
				{
					case TypeCode.SByte:
						if (val >= sbyte.MinValue && val <= sbyte.MaxValue)
							return Direct.Instance;
						break;
					case TypeCode.Byte:
						if (val >= byte.MinValue && val <= byte.MaxValue)
							return Direct.Instance;
						break;
					case TypeCode.Int16:
						if (val >= short.MinValue && val <= short.MaxValue)
							return Direct.Instance;
						break;
					case TypeCode.UInt16:
						if (val >= ushort.MinValue && val <= ushort.MaxValue)
							return Direct.Instance;
						break;
					case TypeCode.UInt32:
						if (val >= 0)
							return Direct.Instance;
						break;
					case TypeCode.UInt64:
						if (val >= 0)
							return Primitive.Instance;
						break;
				}
			}
			if (from == typeof(long))
			{
				LongLiteral longLit = op as LongLiteral;
				if ((object)longLit != null && longLit.Value > 0)
					return Direct.Instance;
			}

			// section 6.1.7 - User-defined implicit conversions (details in section 6.4.3)
			if (onlyStandard || from == typeof(object) || to == typeof(object) || from.IsInterface || to.IsInterface ||
				to.IsSubclassOf(from) || from.IsSubclassOf(to))
				return Invalid.Instance;	// skip not-permitted conversion attempts (section 6.4.1)

			List<UserDefined> candidates = null;
			FindCandidates(ref candidates, FindImplicitMethods(from, to), op, to, GetImplicit);

			if (candidates == null)
				return Invalid.Instance;

			if (candidates.Count == 1)
				return candidates[0];

			return UserDefined.FindImplicit(candidates, from, to);
		}

		static IEnumerable<IMemberInfo> FindImplicitMethods(Type from, Type to)
		{
			while (from != null)
			{
				foreach (IMemberInfo mi in TypeInfo.GetMethods(from))
				{
					if (mi.IsStatic && mi.Name.Equals("op_Implicit", StringComparison.OrdinalIgnoreCase) && mi.ParameterTypes.Length == 1)
						yield return mi;
				}

				from = from.BaseType;
			}

			foreach (IMemberInfo mi in TypeInfo.GetMethods(to))
			{
				if (mi.IsStatic && mi.Name.Equals("op_Implicit", StringComparison.OrdinalIgnoreCase) && mi.ParameterTypes.Length == 1)
					yield return mi;
			}
		}

		static void FindCandidates(ref List<UserDefined> candidates, IEnumerable<IMemberInfo> methods, Operand from, Type to, ConversionProvider extraConv)
		{
			foreach (IMemberInfo mi in methods)
			{
				Conversion before = extraConv(from, mi.ParameterTypes[0], true);
				if (!before.IsValid)
					continue;

				Conversion after = extraConv(new FakeTypedOperand(mi.ReturnType), to, true);
				if (!after.IsValid)
					continue;

				if (candidates == null)
					candidates = new List<UserDefined>();

				candidates.Add(new UserDefined(before, mi, after));
			}
		}

		public static Conversion GetExplicit(Operand op, Type to, bool onlyStandard)
		{
			// try implicit
			Conversion conv = GetImplicit(op, to, onlyStandard);
			if (conv.IsValid)
				return conv;

			Type from = Operand.GetType(op);

			// section 6.3.2 - Standard explicit conversions
			if (onlyStandard)
			{
				if (from == null || !GetImplicit(to, from, true).IsValid)
					return Invalid.Instance;
			}

			TypeCode tcFrom = Type.GetTypeCode(from);
			TypeCode tcTo = Type.GetTypeCode(to);
			byte ct = convTable[(int)tcFrom][(int)tcTo];

			// section 6.2.1 - Explicit numeric conversions, 6.2.2 - Explicit enumeration conversions
			if ((from.IsPrimitive || from.IsEnum || from == typeof(decimal)) && (to.IsPrimitive || to.IsEnum || to == typeof(decimal)))
			{
				if (ct == D)
					return Direct.Instance;	// this can happen for conversions involving enum-s

				if (ct <= E)
				{
					if (from == typeof(decimal) || to == typeof(decimal))
						// decimal is handled as user-defined conversion, but as it is a standard one, always enable UDC processing
						onlyStandard = false;
					else
						return Primitive.Instance;
				}
			}

			// section 6.2.5 - User-defined explicit conversions (details in section 6.4.4)
			if (!(onlyStandard || from == typeof(object) || to == typeof(object) || from.IsInterface || to.IsInterface ||
				to.IsSubclassOf(from) || from.IsSubclassOf(to)))
			{
				List<UserDefined> candidates = null;
				FindCandidates(ref candidates, FindExplicitMethods(from, to), op, to, GetExplicit);

				if (candidates != null)
				{
					if (candidates.Count == 1)
						return candidates[0];

					return UserDefined.FindExplicit(candidates, from, to);
				}
			}

			// section 6.2.3 - Explicit reference conversions, 6.2.4 - Unboxing conversions
			// TODO: not really according to spec, but mostly works
			if (!from.IsValueType && from.IsAssignableFrom(to))
			{
				if (to.IsValueType)
					return Unboxing.Instance;
				else
					return Cast.Instance;
			}

			return Invalid.Instance;
		}

		static IEnumerable<IMemberInfo> FindExplicitMethods(Type from, Type to)
		{
			while (from != null)
			{
				foreach (IMemberInfo mi in TypeInfo.GetMethods(from))
				{
					if (mi.IsStatic &&
						(mi.Name.Equals("op_Implicit", StringComparison.OrdinalIgnoreCase) || 
						mi.Name.Equals("op_Explicit", StringComparison.OrdinalIgnoreCase)) &&
						mi.ParameterTypes.Length == 1)
						yield return mi;
				}

				from = from.BaseType;
			}

			while (to != null)
			{
				foreach (IMemberInfo mi in TypeInfo.GetMethods(to))
				{
					if (mi.IsStatic &&
						(mi.Name.Equals("op_Implicit", StringComparison.OrdinalIgnoreCase) ||
						mi.Name.Equals("op_Explicit", StringComparison.OrdinalIgnoreCase)) &&
						mi.ParameterTypes.Length == 1)
						yield return mi;
				}

				to = to.BaseType;
			}
		}
	}
}
