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
	using Operands;

	abstract class Conversion
	{
	    readonly ITypeMapper _typeMapper;

		public abstract void Emit(CodeGen g, Type from, Type to);
		public virtual bool IsAmbiguous => false;
	    public virtual bool IsValid => true;

	    const byte D = 0;	// direct conversion
		const byte I = 1;	// implicit conversion
		const byte E = 2;	// explicit conversion
		const byte X = 3;	// no conversion

		static readonly byte[][] _convTable = { // indexed by TypeCode [from,to]
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

	    protected Conversion(ITypeMapper typeMapper)
	    {
	        _typeMapper = typeMapper;
	    }

	    delegate Conversion ConversionProvider(Operand op, Type to, bool onlyStandard, ITypeMapper typeMapper);

		#region Conversions
		sealed class Direct : Conversion
		{
		    public Direct(ITypeMapper typeMapper)
		        : base(typeMapper)
		    {
		    }
            
			public override void Emit(CodeGen g, Type from, Type to)
			{
			}
		}

		sealed class Primitive : Conversion
		{
		    public Primitive(ITypeMapper typeMapper)
		        : base(typeMapper)
		    {
		    }

		    public override void Emit(CodeGen g, Type from, Type to)
			{
				g.EmitConvHelper(Type.GetTypeCode(to));
			}
		}

		sealed class Boxing : Conversion
		{
		    public Boxing(ITypeMapper typeMapper)
		        : base(typeMapper)
		    {
		    }

		    public override void Emit(CodeGen g, Type from, Type to)
			{
				g.IL.Emit(OpCodes.Box, from);
			}
		}

		sealed class Unboxing : Conversion
		{
		    public Unboxing(ITypeMapper typeMapper)
		        : base(typeMapper)
		    {
		    }

		    public override void Emit(CodeGen g, Type from, Type to)
			{
				g.IL.Emit(OpCodes.Unbox_Any, to);
			}
		}

		sealed class Cast : Conversion
		{
		    public Cast(ITypeMapper typeMapper)
		        : base(typeMapper)
		    {
		    }

			public override void Emit(CodeGen g, Type from, Type to)
			{
				g.IL.Emit(OpCodes.Castclass, to);
			}
		}

		class Invalid : Conversion
		{
		    public Invalid(ITypeMapper typeMapper)
		        : base(typeMapper)
		    {
		    }

		    public override void Emit(CodeGen g, Type from, Type to)
			{
				throw new InvalidCastException(string.Format(null, Properties.Messages.ErrInvalidConversion, from == null ? "<null>" : from.FullName, to.FullName));
			}

			public override bool IsValid => false;
		}

		class Ambiguous : Conversion
		{
		    public Ambiguous(ITypeMapper typeMapper)
		        : base(typeMapper)
		    {
		    }

		    public override void Emit(CodeGen g, Type from, Type to)
			{
				throw new AmbiguousMatchException(string.Format(null, Properties.Messages.ErrAmbiguousConversion, from.FullName, to.FullName));
			}

			public override bool IsAmbiguous => true;

		    public override bool IsValid => false;
		}

		class UserDefined : Conversion
		{
		    readonly Conversion _before;
		    readonly Conversion _after;
		    readonly IMemberInfo _method;
		    readonly Type _fromType;
		    readonly Type _toType;
		    bool _sxSubset, _txSubset;

			public UserDefined(Conversion before, IMemberInfo method, Conversion after, ITypeMapper typeMapper)
			    : base(typeMapper)
			{
				_before = before;
				_method = method;
				_fromType = method.ParameterTypes[0];
				_toType = method.ReturnType;
				_after = after;
			}

			public override void Emit(CodeGen g, Type from, Type to)
			{
				_before.Emit(g, from, _fromType);
				g.IL.Emit(OpCodes.Call, (MethodInfo)_method.Member);
				_after.Emit(g, _toType, to);
			}

			public static Conversion FindImplicit(List<UserDefined> collection, Type @from, Type to, ITypeMapper typeMapper)
			{
				Type sx = null, tx = null;
				bool any = false;

				for (int i = 0; i < collection.Count; i++)
				{
					UserDefined udc = collection[i];
					any = true;
					if (sx == null && udc._fromType == from)
						sx = from;
					if (tx == null && udc._toType == to)
						tx = to;
				}

				if (!any)
					return new Invalid(typeMapper);

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

							if (sxMatch && GetImplicit(udc._fromType, udc2._fromType, true, typeMapper) == null)
								sxMatch = false;

							if (txMatch && GetImplicit(udc2._toType, udc._toType, true, typeMapper) == null)
								txMatch = false;

							if (!(sxMatch || txMatch))
								break;
						}

						if (sxMatch)
							sx = udc._fromType;
						if (txMatch)
							tx = udc._toType;

						if (sx != null && tx != null)
							break;
					}
				}

				if (sx == null || tx == null)
					return new Ambiguous(typeMapper);

				UserDefined match = null;

				for (int i = 0; i < collection.Count; i++)
				{
					UserDefined udc = collection[i];
					if (udc._fromType == sx && udc._toType == tx)
					{
						if (match != null)
							return new Ambiguous(typeMapper);	// ambiguous match
						else
							match = udc;
					}
				}

				if (match == null)
					return new Ambiguous(typeMapper);

				return match;
			}

			public static Conversion FindExplicit(List<UserDefined> collection, Type @from, Type to, ITypeMapper typeMapper)
			{
				Type sx = null, tx = null;
				bool sxSubset = false, txSubset = false;
				bool any = false;

				for (int i = 0; i < collection.Count; i++)
				{
					UserDefined udc = collection[i];
					any = true;

					if (sx == null && udc._fromType == from)
						sx = from;
					if (tx == null && udc._toType == to)
						tx = to;

					if (udc._sxSubset = GetImplicit(@from, udc._fromType, true, typeMapper) != null)
						sxSubset = true;
					if (udc._txSubset = GetImplicit(udc._toType, to, true, typeMapper) != null)
						txSubset = true;
				}

				if (!any)
					return new Invalid(typeMapper);

				if (sx == null || tx == null)
				{
					for (int i = 0; i < collection.Count; i++)
					{
						UserDefined udc = collection[i];
						bool sxMatch = sx == null && !sxSubset || udc._sxSubset;
						bool txMatch = tx == null && !txSubset || udc._txSubset;

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
									if (udc._sxSubset && GetImplicit(udc._fromType, udc2._fromType, true, typeMapper) == null)
										sxMatch = false;
								}
								else
								{
									if (GetImplicit(udc2._fromType, udc._fromType, true, typeMapper) == null)
										sxMatch = false;
								}
							}

							if (txMatch)
							{
								if (txSubset)
								{
									if (udc._txSubset && GetImplicit(udc2._toType, udc._toType, true, typeMapper) == null)
										txMatch = false;
								}
								else
								{
									if (GetImplicit(udc._toType, udc2._toType, true, typeMapper) == null)
										txMatch = false;
								}
							}

							if (!(sxMatch || txMatch))
								break;
						}

						if (sxMatch)
							sx = udc._fromType;
						if (txMatch)
							tx = udc._toType;

						if (sx != null && tx != null)
							break;
					}
				}

				if (sx == null || tx == null)
					return new Ambiguous(typeMapper);

				UserDefined match = null;

				for (int i = 0; i < collection.Count; i++)
				{
					UserDefined udc = collection[i];
					if (udc._fromType == sx && udc._toType == tx)
					{
						if (match != null)
							return new Ambiguous(typeMapper);	// ambiguous match
						else
							match = udc;
					}
				}

				if (match == null)
					return new Ambiguous(typeMapper);
				
				return match;
			}
		}
		#endregion

		sealed class FakeTypedOperand : Operand
		{
		    public FakeTypedOperand(Type t) { Type = t; }

			public override Type Type { get; }
		}
        
	    public static Conversion GetImplicit(Type @from, Type to, bool onlyStandard, ITypeMapper typeMapper)
		{
			return GetImplicit(new FakeTypedOperand(@from), to, onlyStandard, typeMapper);
		}

		// the sections mentioned in comments of this method are from C# specification v1.2
		public static Conversion GetImplicit(Operand op, Type to, bool onlyStandard, ITypeMapper typeMapper)
		{
			Type from = Operand.GetType(op);

			if (to.Equals(from))
				return new Direct(typeMapper);

			// required for arrays created from TypeBuilder-s
			if (from != null && to.IsArray && from.IsArray)
			{
				if (to.GetArrayRank() == from.GetArrayRank())
				{
					if (to.GetElementType().Equals(from.GetElementType()))
						return new Direct(typeMapper);
				}
			}

			TypeCode tcFrom = Type.GetTypeCode(from);
			TypeCode tcTo = Type.GetTypeCode(to);
			byte ct = _convTable[(int)tcFrom][(int)tcTo];

			// section 6.1.2 - Implicit numeric conversions
			if (from != null && (from.IsPrimitive || Helpers.AreTypesEqual(from, typeof(decimal))) && (to.IsPrimitive || Helpers.AreTypesEqual(to, typeof(decimal))))
			{
				if (ct <= I)
				{
				    if (Helpers.AreTypesEqual(from, typeof(decimal)) || Helpers.AreTypesEqual(to, typeof(decimal)))
						// decimal is handled as user-defined conversion, but as it is a standard one, always enable UDC processing
						onlyStandard = false;
					else
						return new Primitive(typeMapper);
				}
			}

			IntLiteral intLit = op as IntLiteral;

			// section 6.1.3 - Implicit enumeration conversions
			if (!onlyStandard && to.IsEnum && (object)intLit != null && intLit.Value == 0)
				return new Primitive(typeMapper);

			// section 6.1.4 - Implicit reference conversions
			if ((from == null || !from.IsValueType) && !to.IsValueType)
			{
				if (from == null) // from the null type to any reference type
					return new Direct(typeMapper);

				if (to.IsAssignableFrom(from))	// the rest
					return new Direct(typeMapper);
			}

			if (from == null)	// no other conversion from null type is possible
				return new Invalid(typeMapper);

			// section 6.1.5 - Boxing conversions
			if (from.IsValueType)
			{
				if (to.IsAssignableFrom(from))
					return new Boxing(typeMapper);
			}

			// section 6.1.6 - Implicit constant expression conversions
			if ((object)intLit != null && Helpers.AreTypesEqual(from, typeof(int)) && to.IsPrimitive)
			{
				int val = intLit.Value;

				switch (tcTo)
				{
					case TypeCode.SByte:
						if (val >= sbyte.MinValue && val <= sbyte.MaxValue)
							return new Direct(typeMapper);
						break;
					case TypeCode.Byte:
						if (val >= byte.MinValue && val <= byte.MaxValue)
                            return new Direct(typeMapper);
                        break;
					case TypeCode.Int16:
						if (val >= short.MinValue && val <= short.MaxValue)
                            return new Direct(typeMapper);
                        break;
					case TypeCode.UInt16:
						if (val >= ushort.MinValue && val <= ushort.MaxValue)
                            return new Direct(typeMapper);
                        break;
					case TypeCode.UInt32:
						if (val >= 0)
                            return new Direct(typeMapper);
                        break;
					case TypeCode.UInt64:
						if (val >= 0)
                            return new Direct(typeMapper);
                        break;
				}
			}
			if (Helpers.AreTypesEqual(from, typeof(long)))
			{
				LongLiteral longLit = op as LongLiteral;
				if ((object)longLit != null && longLit.Value > 0)
					return new Direct(typeMapper);
			}

			// section 6.1.7 - User-defined implicit conversions (details in section 6.4.3)
			if (onlyStandard || Helpers.AreTypesEqual(from, typeof(object)) || Helpers.AreTypesEqual(to, typeof(object)) || from.IsInterface || to.IsInterface ||
				to.IsSubclassOf(from) || from.IsSubclassOf(to))
                return new Invalid(typeMapper);  // skip not-permitted conversion attempts (section 6.4.1)

            List<UserDefined> candidates = null;
			FindCandidates(ref candidates, FindImplicitMethods(from, to, typeMapper), op, to, GetImplicit, typeMapper);

			if (candidates == null)
                return new Invalid(typeMapper);

            if (candidates.Count == 1)
				return candidates[0];

			return UserDefined.FindImplicit(candidates, @from, to, typeMapper);
		}

		static IEnumerable<IMemberInfo> FindImplicitMethods(Type from, Type to, ITypeMapper typeMapper)
		{
			while (from != null)
			{
				foreach (IMemberInfo mi in typeMapper.TypeInfo.GetMethods(from))
				{
					if (mi.IsStatic && mi.Name.Equals("op_Implicit", StringComparison.OrdinalIgnoreCase) && mi.ParameterTypes.Length == 1)
						yield return mi;
				}

				from = from.BaseType;
			}

			foreach (IMemberInfo mi in typeMapper.TypeInfo.GetMethods(to))
			{
				if (mi.IsStatic && mi.Name.Equals("op_Implicit", StringComparison.OrdinalIgnoreCase) && mi.ParameterTypes.Length == 1)
					yield return mi;
			}
		}

		static void FindCandidates(ref List<UserDefined> candidates, IEnumerable<IMemberInfo> methods, Operand from, Type to, ConversionProvider extraConv, ITypeMapper typeMapper)
		{
			foreach (IMemberInfo mi in methods)
			{
				Conversion before = extraConv(from, mi.ParameterTypes[0], true, typeMapper);
				if (!before.IsValid)
					continue;

				Conversion after = extraConv(new FakeTypedOperand(mi.ReturnType), to, true, typeMapper);
				if (!after.IsValid)
					continue;

				if (candidates == null)
					candidates = new List<UserDefined>();

				candidates.Add(new UserDefined(before, mi, after, typeMapper));
			}
		}

		public static Conversion GetExplicit(Operand op, Type to, bool onlyStandard, ITypeMapper typeMapper)
		{
			// try implicit
			Conversion conv = GetImplicit(op, to, onlyStandard, typeMapper);
			if (conv.IsValid)
				return conv;

			Type from = Operand.GetType(op);

			// section 6.3.2 - Standard explicit conversions
			if (onlyStandard)
			{
				if (from == null || !GetImplicit(to, @from, true, typeMapper).IsValid)
					return new Invalid(typeMapper);
			}

			TypeCode tcFrom = Type.GetTypeCode(from);
			TypeCode tcTo = Type.GetTypeCode(to);
			byte ct = _convTable[(int)tcFrom][(int)tcTo];

			// section 6.2.1 - Explicit numeric conversions, 6.2.2 - Explicit enumeration conversions
			if ((from.IsPrimitive || from.IsEnum || Helpers.AreTypesEqual(from, typeof(decimal))) && (to.IsPrimitive || to.IsEnum || Helpers.AreTypesEqual(to, typeof(decimal))))
			{
				if (ct == D)
					return new Direct(typeMapper);	// this can happen for conversions involving enum-s

				if (ct <= E)
				{
				    if (Helpers.AreTypesEqual(from, typeof(decimal)) || Helpers.AreTypesEqual(to, typeof(decimal)))
						// decimal is handled as user-defined conversion, but as it is a standard one, always enable UDC processing
						onlyStandard = false;
					else
                        return new Direct(typeMapper);
				}
			}

			// section 6.2.5 - User-defined explicit conversions (details in section 6.4.4)
		    if (!(onlyStandard || Helpers.AreTypesEqual(from, typeof(object)) || Helpers.AreTypesEqual(to, typeof(object)) || from.IsInterface || to.IsInterface ||
				to.IsSubclassOf(from) || from.IsSubclassOf(to)))
			{
				List<UserDefined> candidates = null;
				FindCandidates(ref candidates, FindExplicitMethods(from, to, typeMapper), op, to, GetExplicit, typeMapper);

				if (candidates != null)
				{
					if (candidates.Count == 1)
						return candidates[0];

					return UserDefined.FindExplicit(candidates, @from, to, typeMapper);
				}
			}

			// section 6.2.3 - Explicit reference conversions, 6.2.4 - Unboxing conversions
			// TODO: not really according to spec, but mostly works
			if (!from.IsValueType && from.IsAssignableFrom(to))
			{
				if (to.IsValueType)
					return new Unboxing(typeMapper);
				else
					return new Cast(typeMapper);
			}

			return new Invalid(typeMapper);
		}

		static IEnumerable<IMemberInfo> FindExplicitMethods(Type from, Type to, ITypeMapper typeMapper)
		{
			while (from != null)
			{
				foreach (IMemberInfo mi in typeMapper.TypeInfo.GetMethods(from))
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
				foreach (IMemberInfo mi in typeMapper.TypeInfo.GetMethods(to))
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
