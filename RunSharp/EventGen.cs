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
	public sealed class EventGen : Operand, IMemberInfo, IDelayedCompletion
	{
		TypeGen owner;
		MethodAttributes attrs;
		Type type;
		string name;
		EventBuilder eb;
		FieldGen handler = null;
		List<AttributeGen> customAttributes;

		MethodGen adder, remover;

		internal EventGen(TypeGen owner, string name, Type type, MethodAttributes mthAttr)
		{
			this.owner = owner;
			this.name = name;
			this.type = type;
			this.attrs = mthAttr;

			eb = owner.TypeBuilder.DefineEvent(name, EventAttributes.None, type);
			owner.RegisterForCompletion(this);
		}

		public MethodGen AddMethod()
		{
			return AddMethod("handler");
		}

		public MethodGen AddMethod(string parameterName)
		{
			if (adder == null)
			{
				adder = new MethodGen(owner, "add_" + name, attrs | MethodAttributes.SpecialName, typeof(void), 0);
				adder.Parameter(type, parameterName);
				eb.SetAddOnMethod(adder.GetMethodBuilder());
			}

			return adder;
		}

		public MethodGen RemoveMethod()
		{
			return RemoveMethod("handler");
		}

		public MethodGen RemoveMethod(string parameterName)
		{
			if (remover == null)
			{
				remover = new MethodGen(owner, "remove_" + name, attrs | MethodAttributes.SpecialName, typeof(void), 0);
				remover.Parameter(type, parameterName);
				eb.SetRemoveOnMethod(remover.GetMethodBuilder());
			}

			return remover;
		}

		public EventGen WithStandardImplementation()
		{
			if ((object)handler == null)
			{
				if (IsStatic)
					handler = owner.Private.Static.Field(type, name);
				else
					handler = owner.Private.Field(type, name);

				CodeGen g = AddMethod();
				g.AssignAdd(handler, g.Arg("handler"));
				adder.GetMethodBuilder().SetImplementationFlags(MethodImplAttributes.IL | MethodImplAttributes.Managed | MethodImplAttributes.Synchronized);

				g = RemoveMethod();
				g.AssignSubtract(handler, g.Arg("handler"));
				remover.GetMethodBuilder().SetImplementationFlags(MethodImplAttributes.IL | MethodImplAttributes.Managed | MethodImplAttributes.Synchronized);
			};
				
			return this;
		}

		void IDelayedCompletion.Complete()
		{
			if ((adder == null) != (remover == null))
				throw new InvalidOperationException(Properties.Messages.ErrInvalidEventAccessors);

			AttributeGen.ApplyList(ref customAttributes, eb.SetCustomAttribute);
		}

		internal override void EmitGet(CodeGen g)
		{
			if ((object)handler == null)
				throw new InvalidOperationException(Properties.Messages.ErrCustomEventFieldAccess);

			handler.EmitGet(g);
		}

		internal override void EmitSet(CodeGen g, Operand value, bool allowExplicitConversion)
		{
			if ((object)handler == null)
				throw new InvalidOperationException(Properties.Messages.ErrCustomEventFieldAccess);

			handler.EmitSet(g, value, allowExplicitConversion);
		}

		public override Type Type
		{
			get
			{
				if ((object)handler == null)
					throw new InvalidOperationException(Properties.Messages.ErrCustomEventFieldAccess);

				return type;
			}
		}

		#region Custom Attributes

		public EventGen Attribute(AttributeType type)
		{
			BeginAttribute(type);
			return this;
		}

		public EventGen Attribute(AttributeType type, params object[] args)
		{
			BeginAttribute(type, args);
			return this;
		}

		public AttributeGen<EventGen> BeginAttribute(AttributeType type)
		{
			return BeginAttribute(type, EmptyArray<object>.Instance);
		}

		public AttributeGen<EventGen> BeginAttribute(AttributeType type, params object[] args)
		{
			return AttributeGen<EventGen>.CreateAndAdd(this, ref customAttributes, AttributeTargets.Event, type, args);
		}

		#endregion

		#region IMemberInfo Members

		public MemberInfo Member
		{
			get { return new EventInfoProxy(this); }
		}

		public string Name
		{
			get { return name; }
		}

		Type IMemberInfo.ReturnType
		{
			get { return type; }
		}

		Type[] IMemberInfo.ParameterTypes
		{
			get { return Type.EmptyTypes; }
		}

		bool IMemberInfo.IsParameterArray
		{
			get { return false; }
		}

		public bool IsStatic
		{
			get { return (attrs & MethodAttributes.Static) != 0; }
		}

		public bool IsOverride
		{
			get { return Utils.IsOverride(attrs); }
		}

		#endregion

		class EventInfoProxy : EventInfo
		{
			EventGen eg;

			public EventInfoProxy(EventGen eg) { this.eg = eg; }

			public override EventAttributes Attributes
			{
				get { return EventAttributes.None; }
			}

			public override MethodInfo GetAddMethod(bool nonPublic)
			{
				return eg.adder == null ? null : eg.adder.GetMethodBuilder();
			}

			public override MethodInfo GetRaiseMethod(bool nonPublic)
			{
				return null;
			}

			public override MethodInfo GetRemoveMethod(bool nonPublic)
			{
				return eg.remover == null ? null : eg.remover.GetMethodBuilder();
			}

			public override Type DeclaringType
			{
				get { return eg.owner; }
			}

			public override object[] GetCustomAttributes(Type attributeType, bool inherit)
			{
				return null;
			}

			public override object[] GetCustomAttributes(bool inherit)
			{
				return null;
			}

			public override bool IsDefined(Type attributeType, bool inherit)
			{
				return false;
			}

			public override string Name
			{
				get { return eg.name; }
			}

			public override Type ReflectedType
			{
				get { return DeclaringType; }
			}
		}
	}
}
