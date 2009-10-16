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
using System.Reflection;
using System.Reflection.Emit;

namespace TriAxis.RunSharp
{
	public struct AttributeType
	{
		Type t;

		private AttributeType(Type t)
		{
			this.t = t;
		}

		public static implicit operator Type(AttributeType t)
		{
			return t.t;
		}

		public static implicit operator AttributeType(Type t)
		{
			if (!typeof(Attribute).IsAssignableFrom(t))
				throw new ArgumentException("Attribute types must derive from the 'Attribute' class", "t");

			return new AttributeType(t);
		}

		public static implicit operator AttributeType(TypeGen tg)
		{
			if (!typeof(Attribute).IsAssignableFrom(tg.TypeBuilder))
				throw new ArgumentException("Attribute types must derive from the 'Attribute' class", "t");

			return new AttributeType(tg.TypeBuilder);
		}
	}

	public class AttributeGen
	{
		Type attributeType;
		object[] args;
		ApplicableFunction ctor;
		Dictionary<PropertyInfo, object> namedProperties;
		Dictionary<FieldInfo, object> namedFields;

		internal AttributeGen(AttributeTargets target, AttributeType attributeType, object[] args)
		{
			if (args != null)
			{
				foreach (object arg in args)
				{
					CheckValue(arg);
				}
			}

			// TODO: target validation

			this.attributeType = attributeType;

			Operand[] argOperands;
			if (args == null || args.Length == 0)
			{
				this.args = EmptyArray<object>.Instance;
				argOperands = Operand.EmptyArray;
			}
			else
			{
				this.args = args;
				argOperands = new Operand[args.Length];
				for (int i = 0; i < args.Length; i++)
				{
					argOperands[i] = GetOperand(args[i]);
				}
			}

			this.ctor = TypeInfo.FindConstructor(attributeType, argOperands);
		}

		static bool IsValidAttributeParamType(Type t)
		{
			return t != null && (t.IsPrimitive || t.IsEnum || typeof(Type).IsAssignableFrom(t) || t == typeof(string));
		}

		static bool IsSingleDimensionalZeroBasedArray(Array a)
		{
			return a != null && a.Rank == 1 && a.GetLowerBound(0) == 0;
		}

		static void CheckValue(object arg)
		{
			if (arg == null)
				throw new ArgumentNullException();
			Type t = arg.GetType();

			if (IsValidAttributeParamType(t))
				return;
			if (IsSingleDimensionalZeroBasedArray(arg as Array) && IsValidAttributeParamType(t.GetElementType()))
				return;

			throw new ArgumentException();
		}

		static Operand GetOperand(object arg)
		{
			return Operand.FromObject(arg);
		}

		public AttributeGen SetField(string name, object value)
		{
			CheckValue(value);

			FieldInfo fi = (FieldInfo)TypeInfo.FindField(attributeType, name, false).Member;

			SetFieldIntl(fi, value);
			return this;
		}

		void SetFieldIntl(FieldInfo fi, object value)
		{
			if (namedFields != null)
			{
				if (namedFields.ContainsKey(fi))
					throw new InvalidOperationException(string.Format(Properties.Messages.ErrAttributeMultiField, fi.Name));
			}
			else
			{
				namedFields = new Dictionary<FieldInfo, object>();
			}

			namedFields[fi] = value;
		}

		public AttributeGen SetProperty(string name, object value)
		{
			CheckValue(value);

			PropertyInfo pi = (PropertyInfo)TypeInfo.FindProperty(attributeType, name, null, false).Method.Member;

			SetPropertyIntl(pi, value);
			return this;
		}

		void SetPropertyIntl(PropertyInfo pi, object value)
		{
			if (!pi.CanWrite)
				throw new InvalidOperationException(string.Format(Properties.Messages.ErrAttributeReadOnlyProperty, pi.Name));

			if (namedProperties != null)
			{
				if (namedProperties.ContainsKey(pi))
					throw new InvalidOperationException(string.Format(Properties.Messages.ErrAttributeMultiProperty, pi.Name));
			}
			else
			{
				namedProperties = new Dictionary<PropertyInfo, object>();
			}

			namedProperties[pi] = value;
		}

		public AttributeGen Set(string name, object value)
		{
			CheckValue(value);

			for (Type t = attributeType; t != null; t = t.BaseType)
			{
				foreach (IMemberInfo mi in TypeInfo.GetFields(t))
				{
					if (mi.Name == name && !mi.IsStatic)
					{
						SetFieldIntl((FieldInfo)mi.Member, value);
						return this;
					}
				}

				ApplicableFunction af = OverloadResolver.Resolve(TypeInfo.Filter(TypeInfo.GetProperties(t), name, false, false, false), Operand.EmptyArray);
				if (af != null)
				{
					SetPropertyIntl((PropertyInfo)af.Method.Member, value);
					return this;
				}
			}

			throw new MissingMemberException(Properties.Messages.ErrMissingProperty);
		}

		CustomAttributeBuilder GetAttributeBuilder()
		{
			ConstructorInfo ci = (ConstructorInfo)ctor.Method.Member;

			if (namedProperties == null && namedFields == null)
			{
				return new CustomAttributeBuilder(ci, args);
			}

			if (namedProperties == null)
			{
				return new CustomAttributeBuilder(ci, args, ArrayUtils.ToArray(namedFields.Keys), ArrayUtils.ToArray(namedFields.Values));
			}

			if (namedFields == null)
			{
				return new CustomAttributeBuilder(ci, args, ArrayUtils.ToArray(namedProperties.Keys), ArrayUtils.ToArray(namedProperties.Values));
			}

			return new CustomAttributeBuilder(ci, args,
				ArrayUtils.ToArray(namedProperties.Keys), ArrayUtils.ToArray(namedProperties.Values),
				ArrayUtils.ToArray(namedFields.Keys), ArrayUtils.ToArray(namedFields.Values));
		}

		internal static void ApplyList(ref List<AttributeGen> customAttributes, Action<CustomAttributeBuilder> setCustomAttribute)
		{
			if (customAttributes != null)
			{
				foreach (AttributeGen ag in customAttributes)
					setCustomAttribute(ag.GetAttributeBuilder());

				customAttributes = null;
			}
		}
	}

	public class AttributeGen<TOuterContext> : AttributeGen
	{
		TOuterContext context;

		internal AttributeGen(TOuterContext context, AttributeTargets target, AttributeType attributeType, object[] args)
			: base(target, attributeType, args)
		{
			this.context = context;
		}

		internal static AttributeGen<TOuterContext> CreateAndAdd(TOuterContext context, ref List<AttributeGen> list, AttributeTargets target, AttributeType attributeType, object[] args)
		{
			AttributeGen<TOuterContext> ag = new AttributeGen<TOuterContext>(context, target, attributeType, args);
			if (list == null)
				list = new List<AttributeGen>();
			list.Add(ag);
			return ag;
		}

		public new AttributeGen<TOuterContext> SetField(string name, object value)
		{
			base.SetField(name, value);
			return this;
		}

		public new AttributeGen<TOuterContext> SetProperty(string name, object value)
		{
			base.SetProperty(name, value);
			return this;
		}

		public new AttributeGen<TOuterContext> Set(string name, object value)
		{
			base.Set(name, value);
			return this;
		}

		public TOuterContext End()
		{
			return context;
		}
	}
}
