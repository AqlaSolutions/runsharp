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
	public sealed class EventGen : Operand, IMemberInfo, IDelayedCompletion
	{
	    readonly TypeGen _owner;
	    readonly MethodAttributes _attrs;
	    readonly Type _type;
	    readonly string _name;
		EventBuilder _eb;
		FieldGen _handler = null;
		List<AttributeGen> _customAttributes;

		MethodGen _adder, _remover;

	    private Type _interfaceType;

	    internal EventGen(TypeGen owner, string name, Type type, MethodAttributes mthAttr)
		{
			this._owner = owner;
			this._name = name;
			this._type = type;
			this._attrs = mthAttr;
		}

        void LockSignature()
        {
            if (_eb == null)
            {
                _eb = _owner.TypeBuilder.DefineEvent(_interfaceType == null ? _name : _interfaceType.FullName + "." + _name, EventAttributes.None, _type);
                _owner.RegisterForCompletion(this);
            }
        }

        internal Type ImplementedInterface
        {
            get { return _interfaceType; }
            set { _interfaceType = value; }
        }

		public MethodGen AddMethod()
		{
			return AddMethod("handler");
		}

		public MethodGen AddMethod(string parameterName)
		{
			if (_adder == null)
			{
			    LockSignature();
				_adder = new MethodGen(_owner, "add_" + _name, _attrs | MethodAttributes.SpecialName, typeof(void), 0);
			    _adder.ImplementedInterface = _interfaceType;
				_adder.Parameter(_type, parameterName);
				_eb.SetAddOnMethod(_adder.GetMethodBuilder());
			}

			return _adder;
		}

		public MethodGen RemoveMethod()
		{
			return RemoveMethod("handler");
		}

		public MethodGen RemoveMethod(string parameterName)
		{
			if (_remover == null)
			{
			    LockSignature();
				_remover = new MethodGen(_owner, "remove_" + _name, _attrs | MethodAttributes.SpecialName, typeof(void), 0);
			    _remover.ImplementedInterface = _interfaceType;
                _remover.Parameter(_type, parameterName);
				_eb.SetRemoveOnMethod(_remover.GetMethodBuilder());
			}

			return _remover;
		}

		public EventGen WithStandardImplementation()
		{
			if ((object)_handler == null)
			{
				if (IsStatic)
					_handler = _owner.Private.Static.Field(_type, _name);
				else
					_handler = _owner.Private.Field(_type, _name);

				CodeGen g = AddMethod();
				g.AssignAdd(_handler, g.Arg("handler"));
				_adder.GetMethodBuilder().SetImplementationFlags(MethodImplAttributes.IL | MethodImplAttributes.Managed | MethodImplAttributes.Synchronized);

				g = RemoveMethod();
				g.AssignSubtract(_handler, g.Arg("handler"));
				_remover.GetMethodBuilder().SetImplementationFlags(MethodImplAttributes.IL | MethodImplAttributes.Managed | MethodImplAttributes.Synchronized);
			};
				
			return this;
		}

		void IDelayedCompletion.Complete()
		{
			if ((_adder == null) != (_remover == null))
				throw new InvalidOperationException(Properties.Messages.ErrInvalidEventAccessors);

			AttributeGen.ApplyList(ref _customAttributes, _eb.SetCustomAttribute);
		}

		internal override void EmitGet(CodeGen g)
		{
			if ((object)_handler == null)
				throw new InvalidOperationException(Properties.Messages.ErrCustomEventFieldAccess);

			_handler.EmitGet(g);
		}

		internal override void EmitSet(CodeGen g, Operand value, bool allowExplicitConversion)
		{
			if ((object)_handler == null)
				throw new InvalidOperationException(Properties.Messages.ErrCustomEventFieldAccess);

			_handler.EmitSet(g, value, allowExplicitConversion);
		}

		public override Type Type
		{
			get
			{
				if ((object)_handler == null)
					throw new InvalidOperationException(Properties.Messages.ErrCustomEventFieldAccess);

				return _type;
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
			return AttributeGen<EventGen>.CreateAndAdd(this, ref _customAttributes, AttributeTargets.Event, type, args);
		}

		#endregion

		#region IMemberInfo Members

		public MemberInfo Member
		{
			get { return new EventInfoProxy(this); }
		}

		public string Name
		{
			get { return _name; }
		}

		Type IMemberInfo.ReturnType
		{
			get { return _type; }
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
			get { return (_attrs & MethodAttributes.Static) != 0; }
		}

		public bool IsOverride
		{
			get { return Utils.IsOverride(_attrs); }
		}

		#endregion

		class EventInfoProxy : EventInfo
		{
		    readonly EventGen _eg;

			public EventInfoProxy(EventGen eg) { this._eg = eg; }

			public override EventAttributes Attributes
			{
				get { return EventAttributes.None; }
			}

		    public override MethodInfo GetAddMethod(bool nonPublic)
			{
				return _eg._adder == null ? null : _eg._adder.GetMethodBuilder();
			}

			public override MethodInfo GetRaiseMethod(bool nonPublic)
			{
				return null;
			}

			public override MethodInfo GetRemoveMethod(bool nonPublic)
			{
				return _eg._remover == null ? null : _eg._remover.GetMethodBuilder();
			}
#if FEAT_IKVM

		    public override Type EventHandlerType
		    {
		        get { return _eg._type; }
		    }

		    public override MethodInfo[] GetOtherMethods(bool nonPublic)
		    {
		        return new MethodInfo[0];
		    }

		    public override MethodInfo[] __GetMethods()
		    {
		        var list = new List<MethodInfo>();
		        var m = GetAddMethod(true);
		        if (m != null)
		            list.Add(m);
		        m = GetRemoveMethod(true);
		        if (m != null)
		            list.Add(m);
		        m = GetRaiseMethod(true);
		        if (m != null)
		            list.Add(m);
		        list.AddRange(GetOtherMethods(true));
		        return list.ToArray();
		    }

            public override Module Module
            {
                get { return _eg._type.Module; }
            }
#else
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
#endif
            public override Type DeclaringType
			{
				get { return _eg._owner; }
			}

			public override string Name
			{
				get { return _eg._name; }
			}

			public override Type ReflectedType
			{
				get { return DeclaringType; }
			}
		}
	}
}
