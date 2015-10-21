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
using System.Diagnostics;
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
	interface IDelayedDefinition
	{
		void EndDefinition();
	}

	interface IDelayedCompletion
	{
		void Complete();
	}

	public class TypeGen : ITypeInfoProvider
	{
		class InterfaceImplEntry
		{
		    public InterfaceImplEntry(IMemberInfo interfaceMethod)
			{
				InterfaceMethod = interfaceMethod;
			}

			public bool Match(MethodGen candidate)
			{
				return candidate.Name == InterfaceMethod.Name &&
						candidate.ReturnType == InterfaceMethod.ReturnType &&
						ArrayUtils.Equals(candidate.ParameterTypes, InterfaceMethod.ParameterTypes);
			}

			public IMemberInfo InterfaceMethod { get; }
		    public Type InterfaceType => InterfaceMethod.Member.DeclaringType;
		    public MethodGen BoundMethod { get; set; }

		    public bool IsBound => BoundMethod != null;

		    public void Bind(MethodGen implementation)
			{
				BoundMethod = implementation;
			}
		}

	    readonly AssemblyGen _owner;
        public ITypeMapper TypeMapper => _owner.TypeMapper;
	    Type[] _interfaces;
	    Type _type;
		MethodGen _commonCtor;
		ConstructorGen _staticCtor;
	    readonly List<IDelayedDefinition> _definitionQueue = new List<IDelayedDefinition>();
	    readonly List<IDelayedCompletion> _completionQueue = new List<IDelayedCompletion>();
	    readonly List<TypeGen> _nestedTypes = new List<TypeGen>();
	    readonly List<InterfaceImplEntry> _implementations = new List<InterfaceImplEntry>();
	    readonly List<IMemberInfo> _constructors = new List<IMemberInfo>();
	    readonly List<IMemberInfo> _fields = new List<IMemberInfo>();
	    readonly List<IMemberInfo> _properties = new List<IMemberInfo>();
	    readonly List<IMemberInfo> _events = new List<IMemberInfo>();
	    readonly List<IMemberInfo> _methods = new List<IMemberInfo>();
		List<AttributeGen> _customAttributes = new List<AttributeGen>();
		string _indexerName;
		
		internal TypeBuilder TypeBuilder { get; }
	    internal Type BaseType { get; }

	    public string Name { get; }

	    internal TypeGen(AssemblyGen owner, string name, TypeAttributes attrs, Type baseType, Type[] interfaces)
		{
			_owner = owner;
			Name = name;
			BaseType = baseType;
			_interfaces = interfaces;

			TypeBuilder = owner.ModuleBuilder.DefineType(name, attrs, baseType, interfaces);
			owner.AddType(this);
			ScanMethodsToImplement(interfaces);

			TypeInfo.RegisterProvider(TypeBuilder, this);
			ResetAttrs();
		}

		internal TypeGen(TypeGen owner, string name, TypeAttributes attrs, Type baseType, Type[] interfaces)
		{
			_owner = owner._owner;
			Name = name;
			BaseType = baseType;
			_interfaces = interfaces;

			TypeBuilder = owner.TypeBuilder.DefineNestedType(name, attrs, baseType, interfaces);
			owner._nestedTypes.Add(this);
			ScanMethodsToImplement(interfaces);

			TypeInfo.RegisterProvider(TypeBuilder, this);
		}

		void ScanMethodsToImplement(Type[] interfaces)
		{
			if (interfaces == null)
				return;

			foreach (Type t in interfaces)
			{
                foreach (Type @interface in TypeInfo.SearchInterfaces(t))
			    {
                    foreach (IMemberInfo mi in TypeInfo.GetMethods(@interface))
                        _implementations.Add(new InterfaceImplEntry(mi));
			    }
			}
		}

		internal MethodAttributes PreprocessAttributes(MethodGen mg, MethodAttributes attrs)
		{
			bool requireVirtual = false;

			foreach (InterfaceImplEntry implEntry in _implementations)
			{
				if (!implEntry.IsBound && implEntry.Match(mg))
				{
					implEntry.Bind(mg);
					requireVirtual = true;
				}
			}

			if (requireVirtual && ((attrs & MethodAttributes.Virtual) == 0))
				// create an exclusive VTable entry for the method
				attrs |= MethodAttributes.Virtual | MethodAttributes.NewSlot | MethodAttributes.Final;

			return attrs;
		}

		internal void RegisterForCompletion(ICodeGenContext routine)
		{
			_definitionQueue.Add(routine);
			_completionQueue.Add(routine);
		}

		internal void RegisterForCompletion(IDelayedCompletion completion)
		{
			_completionQueue.Add(completion);
		}

		#region Modifiers
		MethodAttributes _mthVis, _mthFlags, _mthVirt;
		FieldAttributes _fldVis, _fldFlags;
		TypeAttributes _typeVis, _typeFlags, _typeVirt;
		MethodImplAttributes _implFlags;

		void SetVisibility(MethodAttributes mthVis, FieldAttributes fldVis, TypeAttributes typeVis)
		{
			if (_mthVis != 0)
				throw new InvalidOperationException(Properties.Messages.ErrMultiVisibility);

			_mthVis = mthVis;
			_fldVis = fldVis;
			_typeVis = typeVis;
		}

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public TypeGen Public { get { SetVisibility(MethodAttributes.Public, FieldAttributes.Public, TypeAttributes.NestedPublic); return this; } }
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public TypeGen Private { get { SetVisibility(MethodAttributes.Private, FieldAttributes.Private, TypeAttributes.NestedPrivate); return this; } }
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public TypeGen Protected { get { SetVisibility(MethodAttributes.Family, FieldAttributes.Family, TypeAttributes.NestedFamily); return this; } }
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public TypeGen Internal { get { SetVisibility(MethodAttributes.Assembly, FieldAttributes.Assembly, TypeAttributes.NestedAssembly); return this; } }
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public TypeGen ProtectedOrInternal { get { SetVisibility(MethodAttributes.FamORAssem, FieldAttributes.FamORAssem, TypeAttributes.NestedFamORAssem); return this; } }
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public TypeGen ProtectedAndInternal { get { SetVisibility(MethodAttributes.FamANDAssem, FieldAttributes.FamANDAssem, TypeAttributes.NestedFamANDAssem); return this; } }

		void SetVirtual(MethodAttributes mthVirt, TypeAttributes typeVirt)
		{
			if (_mthVirt != 0)
				throw new InvalidOperationException(Properties.Messages.ErrMultiVTable);

			_mthVirt = mthVirt;
			_typeVirt = typeVirt;
		}

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public TypeGen Sealed { get { SetVirtual(MethodAttributes.Virtual | MethodAttributes.Final, TypeAttributes.Sealed); return this; } }
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public TypeGen Virtual { get { SetVirtual(MethodAttributes.Virtual | MethodAttributes.NewSlot, 0); return this; } }
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public TypeGen Override { get { SetVirtual(MethodAttributes.Virtual, 0); return this; } }
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public TypeGen Abstract { get { SetVirtual(MethodAttributes.Virtual | MethodAttributes.Abstract, TypeAttributes.Abstract); return this; } }

		void SetFlag(MethodAttributes mthFlag, FieldAttributes fldFlag, TypeAttributes typeFlag)
		{
			if ((_mthFlags & mthFlag) != 0 ||
				(_fldFlags & fldFlag) != 0 ||
				(_typeFlags & typeFlag) != 0)
				throw new InvalidOperationException(string.Format(null, Properties.Messages.ErrMultiAttribute, mthFlag));

			_mthFlags |= mthFlag;
			_fldFlags |= fldFlag;
			_typeFlags |= typeFlag;
		}

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public TypeGen Static { get { SetFlag(MethodAttributes.Static, FieldAttributes.Static, TypeAttributes.Sealed | TypeAttributes.Abstract); return this; } }
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public TypeGen ReadOnly { get { SetFlag(0, FieldAttributes.InitOnly, 0); return this; } }
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public TypeGen NoBeforeFieldInit { get { SetFlag(0, 0, TypeAttributes.BeforeFieldInit); return this; } }

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		internal TypeGen RuntimeImpl { get { _implFlags |= MethodImplAttributes.Runtime | MethodImplAttributes.Managed; return this; } }

		void ResetAttrs()
		{
			if (TypeBuilder.IsInterface)
			{
				_mthVis = MethodAttributes.Public;
				_mthVirt = MethodAttributes.Virtual | MethodAttributes.NewSlot | MethodAttributes.Abstract;
				_mthFlags = 0;
			}
			else
				_mthVis = _mthVirt = _mthFlags = 0;

			_fldVis = _fldFlags = 0;
			_typeVis = _typeVirt = _typeFlags = 0;
			_implFlags = 0;
		}
		#endregion

		#region Custom Attributes

		public TypeGen Attribute(AttributeType type)
		{
			BeginAttribute(type);
			return this;
		}

		public TypeGen Attribute(AttributeType type, params object[] args)
		{
			BeginAttribute(type, args);
			return this;
		}

		public AttributeGen<TypeGen> BeginAttribute(AttributeType type)
		{
			return BeginAttribute(type, EmptyArray<object>.Instance);
		}

		public AttributeGen<TypeGen> BeginAttribute(AttributeType type, params object[] args)
		{
			AttributeTargets target = AttributeTargets.Class;

			if (BaseType == null)
				target = AttributeTargets.Interface;
			else if (BaseType == typeof(ValueType))
				target = AttributeTargets.Struct;
			else
				target = AttributeTargets.Class;

			return AttributeGen<TypeGen>.CreateAndAdd(this, ref _customAttributes, target, type, args);
		}

		#endregion

		#region Members
		public MethodGen CommonConstructor()
		{
			if (TypeBuilder.IsValueType)
				throw new InvalidOperationException(Properties.Messages.ErrStructNoDefaultCtor);
			if (TypeBuilder.IsInterface)
				throw new InvalidOperationException(Properties.Messages.ErrInterfaceNoCtor);

			if (_commonCtor == null)
			{
				_commonCtor = new MethodGen(this, "$$ctor", 0, typeof(void), 0).LockSignature();
			}

			return _commonCtor;
		}

		public ConstructorGen Constructor()
		{
			if (TypeBuilder.IsInterface)
				throw new InvalidOperationException(Properties.Messages.ErrInterfaceNoCtor);

			ConstructorGen cg = new ConstructorGen(this, _mthVis, _implFlags);
			ResetAttrs();
			return cg;
		}

		public ConstructorGen StaticConstructor()
		{
			if (TypeBuilder.IsInterface)
				throw new InvalidOperationException(Properties.Messages.ErrInterfaceNoCtor);
			
			if (_staticCtor == null)
			{
				_staticCtor = new ConstructorGen(this, MethodAttributes.Static, 0).LockSignature();
			}

			return _staticCtor;
		}

		public FieldGen Field(Type type, string name)
		{
			if (TypeBuilder.IsInterface)
				throw new InvalidOperationException(Properties.Messages.ErrInterfaceNoField);

			if (_fldVis == 0)
				_fldVis |= FieldAttributes.Private;

			FieldGen fld = new FieldGen(this, name, type, _fldVis | _fldFlags);
			_fields.Add(fld);
			ResetAttrs();
			return fld;
		}

		public FieldGen Field(Type type, string name, Operand initialValue)
		{
			FieldGen fld = Field(type, name);

			CodeGen initCode = fld.IsStatic ? StaticConstructor().GetCode(): CommonConstructor().GetCode();
			initCode.Assign(fld, initialValue);
			return fld;
		}

		public PropertyGen Property(Type type, string name)
		{
			if (_mthVis == 0)
				_mthVis |= MethodAttributes.Private;

			if (TypeBuilder.IsInterface)
				_mthVirt |= MethodAttributes.Virtual | MethodAttributes.Abstract;

			PropertyGen pg = new PropertyGen(this, _mthVis | _mthVirt | _mthFlags, type, name);
			_properties.Add(pg);
			ResetAttrs();

			return pg;
		}

		public PropertyGen Indexer(Type type)
		{
			return Indexer(type, "Item");
		}

		public PropertyGen Indexer(Type type, string name)
		{
			if (_indexerName != null && _indexerName != name)
				throw new InvalidOperationException(Properties.Messages.ErrAmbiguousIndexerName);

			PropertyGen pg = Property(type, name);
			_indexerName = name;
			return pg;
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters", Justification = "It is invalid to use anything else than a Field as a base for SimpleProperty")]
		public PropertyGen SimpleProperty(FieldGen field, string name)
		{
			if ((object)field == null)
				throw new ArgumentNullException(nameof(field));

			PropertyGen pg = Property(field.Type, name);
			pg.Getter().GetCode().Return(field);
			pg.Setter().GetCode().Assign(field, pg.Setter().GetCode().PropertyValue());
			return pg;
		}

		public EventGen Event(Type handlerType, string name)
		{
			return CustomEvent(handlerType, name).WithStandardImplementation();
		}

		public EventGen CustomEvent(Type handlerType, string name)
		{
			EventGen eg = new EventGen(this, name, handlerType, _mthVis | _mthVirt | _mthFlags);
			_events.Add(eg);
			ResetAttrs();

			return eg;
		}

		public MethodGen Method(Type returnType, string name)
		{
			if (_mthVis == 0)
				_mthVis |= MethodAttributes.Private;
			if (TypeBuilder.IsInterface)
				_mthVirt |= MethodAttributes.Virtual | MethodAttributes.Abstract;

			MethodGen mg = new MethodGen(this, name, _mthVis | _mthVirt | _mthFlags, returnType, _implFlags);
			ResetAttrs();
			return mg;
		}

		public MethodGen ImplicitConversionFrom(Type fromType)
		{
			return ImplicitConversionFrom(fromType, "value");
		}

		public MethodGen ImplicitConversionFrom(Type fromType, string parameterName)
		{
			if (TypeBuilder.IsInterface)
				throw new InvalidOperationException(Properties.Messages.ErrInterfaceNoConversion);

			ResetAttrs();
			_mthFlags = MethodAttributes.SpecialName | MethodAttributes.Static;
			_mthVis = MethodAttributes.Public;
			return Method(TypeBuilder, "op_Implicit").Parameter(fromType, parameterName);
		}

		public MethodGen ImplicitConversionTo(Type toType)
		{
			return ImplicitConversionTo(toType, "value");
		}

		public MethodGen ImplicitConversionTo(Type toType, string parameterName)
		{
			if (TypeBuilder.IsInterface)
				throw new InvalidOperationException(Properties.Messages.ErrInterfaceNoConversion);

			ResetAttrs();
			_mthFlags = MethodAttributes.SpecialName | MethodAttributes.Static;
			_mthVis = MethodAttributes.Public;
			return Method(toType, "op_Implicit").Parameter(TypeBuilder, parameterName);
		}

		public MethodGen ExplicitConversionFrom(Type fromType)
		{
			return ExplicitConversionFrom(fromType, "value");
		}

		public MethodGen ExplicitConversionFrom(Type fromType, string parameterName)
		{
			if (TypeBuilder.IsInterface)
				throw new InvalidOperationException(Properties.Messages.ErrInterfaceNoConversion);

			ResetAttrs();
			_mthFlags = MethodAttributes.SpecialName | MethodAttributes.Static;
			_mthVis = MethodAttributes.Public;
			return Method(TypeBuilder, "op_Explicit").Parameter(fromType, parameterName);
		}

		public MethodGen ExplicitConversionTo(Type toType)
		{
			return ExplicitConversionTo(toType, "value");
		}

		public MethodGen ExplicitConversionTo(Type toType, string parameterName)
		{
			if (TypeBuilder.IsInterface)
				throw new InvalidOperationException(Properties.Messages.ErrInterfaceNoConversion);

			ResetAttrs();
			_mthFlags = MethodAttributes.SpecialName | MethodAttributes.Static;
			_mthVis = MethodAttributes.Public;
			return Method(toType, "op_Explicit").Parameter(TypeBuilder, parameterName);
		}

		public MethodGen Operator(Operator op, Type returnType, Type operandType)
		{
			return Operator(op, returnType, operandType, "operand");
		}

		public MethodGen Operator(Operator op, Type returnType, Type operandType, string operandName)
		{
			if (op == null)
				throw new ArgumentNullException(nameof(op));

			ResetAttrs();
			_mthFlags = MethodAttributes.SpecialName | MethodAttributes.Static;
			_mthVis = MethodAttributes.Public;
			return Method(returnType, "op_" + op.MethodName).Parameter(operandType, operandName);
		}

		public MethodGen Operator(Operator op, Type returnType, Type leftType, Type rightType)
		{
			return Operator(op, returnType, leftType, "left", rightType, "right");
		}

		public MethodGen Operator(Operator op, Type returnType, Type leftType, string leftName, Type rightType, string rightName)
		{
			if (op == null)
				throw new ArgumentNullException(nameof(op));

			ResetAttrs();
			_mthFlags = MethodAttributes.SpecialName | MethodAttributes.Static;
			_mthVis = MethodAttributes.Public;
			return Method(returnType, "op_" + op.MethodName)
				.Parameter(leftType, leftName)
				.Parameter(rightType, rightName)
				;
		}

		public TypeGen Class(string name)
		{
			return Class(name, typeof(object), Type.EmptyTypes);
		}

		public TypeGen Class(string name, Type baseType)
		{
			return Class(name, baseType, Type.EmptyTypes);
		}

		public TypeGen Class(string name, Type baseType, params Type[] interfaces)
		{
			if (TypeBuilder.IsInterface)
				throw new InvalidOperationException(Properties.Messages.ErrInterfaceNoNested);

			if (_typeVis == 0)
				_typeVis |= TypeAttributes.NestedPrivate;

			TypeGen tg = new TypeGen(this, name, (_typeVis | _typeVirt | _typeFlags | TypeAttributes.Class) ^ TypeAttributes.BeforeFieldInit, baseType, interfaces);
			ResetAttrs();
			return tg;
		}

		public TypeGen Struct(string name)
		{
			return Struct(name, Type.EmptyTypes);
		}

		public TypeGen Struct(string name, params Type[] interfaces)
		{
			if (TypeBuilder.IsInterface)
				throw new InvalidOperationException(Properties.Messages.ErrInterfaceNoNested);

			if (_typeVis == 0)
				_typeVis |= TypeAttributes.NestedPrivate;

			TypeGen tg = new TypeGen(this, name, (_typeVis | _typeVirt | _typeFlags | TypeAttributes.Sealed | TypeAttributes.SequentialLayout) ^ TypeAttributes.BeforeFieldInit, typeof(ValueType), interfaces);
			ResetAttrs();
			return tg;
		}

        public DelegateGen Delegate(Type returnType, string name)
        {
            DelegateGen dg = new DelegateGen(this, name, returnType, (_typeVis | _typeVirt | _typeFlags | TypeAttributes.Class | TypeAttributes.Sealed) ^ TypeAttributes.BeforeFieldInit);
            ResetAttrs();
            return dg;
        }   
		#endregion

		#region Interface implementations
		void DefineMethodOverride(MethodGen methodBody, MethodInfo methodDeclaration)
		{
			foreach (InterfaceImplEntry iie in _implementations)
			{
				if (iie.InterfaceMethod.Member == methodDeclaration)
				{
					iie.Bind(methodBody);
					return;
				}
			}
		}

		public MethodGen MethodImplementation(Type interfaceType, Type returnType, string name)
		{
			if (TypeBuilder.IsInterface)
				throw new InvalidOperationException(Properties.Messages.ErrInterfaceNoExplicitImpl);

			MethodGen mg = new MethodGen(this, name,
				MethodAttributes.Private | MethodAttributes.NewSlot | MethodAttributes.Virtual | MethodAttributes.Final,
				returnType, 0);
			mg.ImplementedInterface = interfaceType;
			return mg;
		}

		public PropertyGen PropertyImplementation(Type interfaceType, Type type, string name)
		{
			if (TypeBuilder.IsInterface)
				throw new InvalidOperationException(Properties.Messages.ErrInterfaceNoExplicitImpl);

			PropertyGen pg = new PropertyGen(this,
				MethodAttributes.Private | MethodAttributes.NewSlot | MethodAttributes.Virtual | MethodAttributes.Final,
				type, name);
			pg.ImplementedInterface = interfaceType;
			return pg;
		}

        public EventGen EventImplementation(Type interfaceType, Type eventHandlerType, string name)
        {
            if (TypeBuilder.IsInterface)
                throw new InvalidOperationException(Properties.Messages.ErrInterfaceNoExplicitImpl);

            EventGen eg = new EventGen(this, name, eventHandlerType,
                MethodAttributes.Private | MethodAttributes.NewSlot | MethodAttributes.Virtual | MethodAttributes.Final);
            eg.ImplementedInterface = interfaceType;
            return eg;
        }

		#endregion

		public Type GetCompletedType()
		{
			return GetCompletedType(false);
		}

		public Type GetCompletedType(bool completeIfNeeded)
		{
			if (_type != null)
				return _type;

			if (completeIfNeeded)
			{
				Complete();
				return _type;
			}

			throw new InvalidOperationException(Properties.Messages.ErrTypeNotCompleted);
		}

		public bool IsCompleted => _type != null;

	    void FlushDefinitionQueue()
		{
			// cannot use foreach, because it is possible that new objects
			// will be appended while completing the existing ones
			for (int i = 0; i < _definitionQueue.Count; i++)
			{
				_definitionQueue[i].EndDefinition();
			}
			_definitionQueue.Clear();
		}

		void FlushCompletionQueue()
		{
			// cannot use foreach, because it is possible that new objects
			// will be appended while completing the existing ones
			for (int i = 0; i < _completionQueue.Count; i++)
			{
				_completionQueue[i].Complete();
			}
			_completionQueue.Clear();
		}

		public void Complete()
		{
			if (_type != null)
				return;

			foreach (TypeGen nested in _nestedTypes)
				nested.Complete();

			// ensure creation of default constructor
			EnsureDefaultConstructor();

			FlushDefinitionQueue();
			FlushCompletionQueue();

			// implement all interfaces
			foreach (InterfaceImplEntry iie in _implementations)
			{
				if (!iie.IsBound)
					throw new NotImplementedException(string.Format(null, Properties.Messages.ErrInterfaceNotImplemented,
						iie.InterfaceType, iie.InterfaceMethod.Member));

				TypeBuilder.DefineMethodOverride(iie.BoundMethod.GetMethodBuilder(), (MethodInfo) iie.InterfaceMethod.Member);
			}

			// set indexer name
			if (_indexerName != null)
			{
				CustomAttributeBuilder cab = new CustomAttributeBuilder(
					typeof(DefaultMemberAttribute).GetConstructor(new Type[] { typeof(string) }),
					new object[] { _indexerName });
				TypeBuilder.SetCustomAttribute(cab);
			}

			AttributeGen.ApplyList(ref _customAttributes, TypeBuilder.SetCustomAttribute);

			_type = TypeBuilder.CreateType();

			TypeInfo.UnregisterProvider(TypeBuilder);
		}

		public static implicit operator Type(TypeGen tg)
		{
			if (tg == null)
				return null;

			if (tg._type != null)
				return tg._type;

			return tg.TypeBuilder;
		}

		public override string ToString()
		{
			return TypeBuilder.FullName;
		}

		void EnsureDefaultConstructor()
		{
			if (_constructors.Count == 0 && TypeBuilder.IsClass)
			{
				// create default constructor
				ResetAttrs();
				Public.Constructor().LockSignature();
			}
		}

		#region Member registration
		internal void Register(ConstructorGen constructor)
		{
			if (constructor.IsStatic)
				return;
			
			if (constructor.ParameterCount == 0 && TypeBuilder.IsValueType)
				throw new InvalidOperationException(Properties.Messages.ErrStructNoDefaultCtor);

			_constructors.Add(constructor);
		}

		internal void Register(MethodGen method)
		{
			if (_owner.AssemblyBuilder.EntryPoint == null && method.Name == "Main" && method.IsStatic && (
				method.ParameterCount == 0 ||
				(method.ParameterCount == 1 && method.ParameterTypes[0] == typeof(string[]))))
				_owner.AssemblyBuilder.SetEntryPoint(method.GetMethodBuilder());

			// match explicit interface implementations
			if (method.ImplementedInterface != null)
			{
				foreach (IMemberInfo mi in TypeInfo.Filter(TypeInfo.GetMethods(method.ImplementedInterface), method.Name, false, false, true))
				{
					if (ArrayUtils.Equals(mi.ParameterTypes, method.ParameterTypes))
					{
						DefineMethodOverride(method, (MethodInfo)mi.Member);
						return;
					}
				}

				throw new MissingMethodException(Properties.Messages.ErrMissingMethod);
			}
	
			_methods.Add(method);
		}
		#endregion

		#region ITypeInfoProvider implementation
		IEnumerable<IMemberInfo> ITypeInfoProvider.GetConstructors()
		{
			EnsureDefaultConstructor();
			FlushDefinitionQueue();
			return _constructors;
		}

		IEnumerable<IMemberInfo> ITypeInfoProvider.GetFields()
		{
			FlushDefinitionQueue();
			return _fields;
		}

		IEnumerable<IMemberInfo> ITypeInfoProvider.GetProperties()
		{
			FlushDefinitionQueue();
			return _properties;
		}

		IEnumerable<IMemberInfo> ITypeInfoProvider.GetEvents()
		{
			FlushDefinitionQueue();
			return _events;
		}

		IEnumerable<IMemberInfo> ITypeInfoProvider.GetMethods()
		{
			FlushDefinitionQueue();
			return _methods;
		}

		string ITypeInfoProvider.DefaultMember => _indexerName;

	    #endregion
	}
}
