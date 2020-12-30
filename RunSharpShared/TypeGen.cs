/*
Copyright(c) 2009, Stefan Simek
Copyright(c) 2016, Vladyslav Taranov

MIT License

Permission is hereby granted, free of charge, to any person obtaining
a copy of this software and associated documentation files (the
"Software"), to deal in the Software without restriction, including
without limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to
permit persons to whom the Software is furnished to do so, subject to
the following conditions:

The above copyright notice and this permission notice shall be
included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
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
    public interface IDelayedDefinition
    {
        void EndDefinition();
    }

    public interface IDelayedCompletion
    {
        void Complete();
    }

#if !PHONE8

    public class TypeGen : MemberGenBase<TypeGen>, ITypeInfoProvider, ICodeGenBasicContext
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
        
        public ExpressionFactory ExpressionFactory => _owner?.ExpressionFactory ?? _ownExpressionFactory;
        public StaticFactory StaticFactory => _owner?.StaticFactory ?? _ownStaticFactory;
        StaticFactory _ownStaticFactory;
        ExpressionFactory _ownExpressionFactory;
        Type[] _interfaces;
        readonly ITypeMapper _typeMapper;
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

        internal AssemblyBuilder AssemblyBuilder => _owner.AssemblyBuilder;
        internal TypeBuilder TypeBuilder { get; }
        internal Type BaseType { get; }

        public string Name { get; }

        public TypeGen(TypeBuilder typeBuilder, ITypeMapper typeMapper)
            :base(typeMapper)
        {
            Name = typeBuilder.Name;
            BaseType = typeBuilder.BaseType;
            _interfaces = typeBuilder.GetInterfaces();
            _typeMapper = typeMapper;

            _ownExpressionFactory = new ExpressionFactory(typeMapper);
            _ownStaticFactory = new StaticFactory(TypeMapper);

            TypeBuilder = typeBuilder;
            typeMapper.TypeInfo.RegisterProvider(TypeBuilder, this);

            ResetAttrs();
        }

        internal TypeGen(AssemblyGen owner, string name, TypeAttributes attrs, Type baseType, Type[] interfaces, ITypeMapper typeMapper)
            : base(typeMapper)
        {
            _owner = owner;
            Name = name;
            BaseType = baseType;
            _interfaces = interfaces;
            _typeMapper = typeMapper;

            TypeBuilder = owner.ModuleBuilder.DefineType(name, attrs, baseType, interfaces);
            owner.AddType(this);
            ScanMethodsToImplement(interfaces);

            typeMapper.TypeInfo.RegisterProvider(TypeBuilder, this);
            ResetAttrs();
        }

        internal TypeGen(TypeGen owner, string name, TypeAttributes attrs, Type baseType, Type[] interfaces, ITypeMapper typeMapper)
            : base(typeMapper)
        {
            _owner = owner._owner;
            Name = name;
            BaseType = baseType;
            _interfaces = interfaces;
            _typeMapper = typeMapper;

            TypeBuilder = owner.TypeBuilder.DefineNestedType(name, attrs, baseType, interfaces);
            owner._nestedTypes.Add(this);
            ScanMethodsToImplement(interfaces);

            typeMapper.TypeInfo.RegisterProvider(TypeBuilder, this);
        }

        void ScanMethodsToImplement(Type[] interfaces)
        {
            if (interfaces == null)
                return;

            foreach (Type t in interfaces)
            {
                foreach (Type @interface in _typeMapper.TypeInfo.SearchInterfaces(t))
                {
                    foreach (IMemberInfo mi in _typeMapper.TypeInfo.GetMethods(@interface))
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
        

        public override TypeGen Attribute(AttributeType type)
        {
            BeginAttribute(type);
            return this;
        }
        

        public override TypeGen Attribute(AttributeType type, params object[] args)
        {
            BeginAttribute(type, args);
            return this;
        }
        

        public override AttributeGen<TypeGen> BeginAttribute(AttributeType type)
        {
            return BeginAttribute(type, EmptyArray<object>.Instance);
        }
        
        public override AttributeGen<TypeGen> BeginAttribute(AttributeType type, params object[] args)
        {
            AttributeTargets target = AttributeTargets.Class;

            if (BaseType == null)
                target = AttributeTargets.Interface;
            else if (BaseType == TypeMapper.MapType(typeof(ValueType)))
                target = AttributeTargets.Struct;
            else
                target = AttributeTargets.Class;

            return AttributeGen<TypeGen>.CreateAndAdd(this, ref _customAttributes, target, type, args, TypeMapper);
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
                _commonCtor = new MethodGen(this, "$$ctor", 0, TypeMapper.MapType(typeof(void)), 0).LockSignature();
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

#if FEAT_IKVM

        public FieldGen Field(System.Type type, string name)
        {
            return Field(TypeMapper.MapType(type), name);
        }
        
#endif

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

#if FEAT_IKVM

        public FieldGen Field(System.Type type, string name, Operand initialValue)
        {
            return Field(TypeMapper.MapType(type), name, initialValue);
        }
        
#endif

        public FieldGen Field(Type type, string name, Operand initialValue)
        {
            FieldGen fld = Field(type, name);

            CodeGen initCode = fld.IsStatic ? StaticConstructor().GetCode(): CommonConstructor().GetCode();
            initCode.Assign(fld, initialValue);
            return fld;
        }

#if FEAT_IKVM

        public PropertyGen Property(System.Type type, string name)
        {
            return Property(TypeMapper.MapType(type), name);
        }
        
#endif

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

#if FEAT_IKVM

        public PropertyGen Indexer(System.Type type)
        {
            return Indexer(TypeMapper.MapType(type));
        }
        
#endif

        public PropertyGen Indexer(Type type)
        {
            return Indexer(type, "Item");
        }

#if FEAT_IKVM

        public PropertyGen Indexer(System.Type type, string name)
        {
            return Indexer(TypeMapper.MapType(type), name);
        }
        
#endif

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

            PropertyGen pg = Property(field.GetReturnType(_typeMapper), name);
            pg.Getter().GetCode().Return(field);
            pg.Setter().GetCode().Assign(field, pg.Setter().GetCode().PropertyValue());
            return pg;
        }

#if FEAT_IKVM

        public EventGen Event(System.Type handlerType, string name)
        {
            return Event(TypeMapper.MapType(handlerType), name);
        }
        
#endif

        public EventGen Event(Type handlerType, string name)
        {
            return CustomEvent(handlerType, name).WithStandardImplementation();
        }

#if FEAT_IKVM

        public EventGen CustomEvent(System.Type handlerType, string name)
        {
            return CustomEvent(TypeMapper.MapType(handlerType), name);
        }
        
#endif

        public EventGen CustomEvent(Type handlerType, string name)
        {
            EventGen eg = new EventGen(this, name, handlerType, _mthVis | _mthVirt | _mthFlags);
            _events.Add(eg);
            ResetAttrs();

            return eg;
        }

#if FEAT_IKVM
        public MethodGen Method(System.Type returnType, string name)
        {
            return Method(TypeMapper.MapType(returnType), name);
        }
#endif

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

#if FEAT_IKVM

        public MethodGen ImplicitConversionFrom(System.Type fromType)
        {
            return ImplicitConversionFrom(TypeMapper.MapType(fromType));
        }
        
#endif

        public MethodGen ImplicitConversionFrom(Type fromType)
        {
            return ImplicitConversionFrom(fromType, "value");
        }

#if FEAT_IKVM
        public MethodGen ImplicitConversionFrom(System.Type fromType, string parameterName)
        {
            return ImplicitConversionFrom(TypeMapper.MapType(fromType), parameterName);
        }
        
#endif
        public MethodGen ImplicitConversionFrom(Type fromType, string parameterName)
        {
            if (TypeBuilder.IsInterface)
                throw new InvalidOperationException(Properties.Messages.ErrInterfaceNoConversion);

            ResetAttrs();
            _mthFlags = MethodAttributes.SpecialName | MethodAttributes.Static;
            _mthVis = MethodAttributes.Public;
            return Method(TypeBuilder, "op_Implicit").Parameter(fromType, parameterName);
        }

#if FEAT_IKVM

        public MethodGen ImplicitConversionTo(System.Type toType)
        {
            return ImplicitConversionTo(TypeMapper.MapType(toType));
        }
        
#endif

        public MethodGen ImplicitConversionTo(Type toType)
        {
            return ImplicitConversionTo(toType, "value");
        }

#if FEAT_IKVM

        public MethodGen ImplicitConversionTo(System.Type toType, string parameterName)
        {
            return ImplicitConversionTo(TypeMapper.MapType(toType), parameterName);
        }
        
#endif

        public MethodGen ImplicitConversionTo(Type toType, string parameterName)
        {
            if (TypeBuilder.IsInterface)
                throw new InvalidOperationException(Properties.Messages.ErrInterfaceNoConversion);

            ResetAttrs();
            _mthFlags = MethodAttributes.SpecialName | MethodAttributes.Static;
            _mthVis = MethodAttributes.Public;
            return Method(toType, "op_Implicit").Parameter(TypeBuilder, parameterName);
        }

#if FEAT_IKVM

        public MethodGen ExplicitConversionFrom(System.Type fromType)
        {
            return ExplicitConversionFrom(TypeMapper.MapType(fromType));
        }
        
#endif

        public MethodGen ExplicitConversionFrom(Type fromType)
        {
            return ExplicitConversionFrom(fromType, "value");
        }

#if FEAT_IKVM

        public MethodGen ExplicitConversionFrom(System.Type fromType, string parameterName)
        {
            return ExplicitConversionFrom(TypeMapper.MapType(fromType), parameterName);
        }
#endif
    
        public MethodGen ExplicitConversionFrom(Type fromType, string parameterName)
        {
            if (TypeBuilder.IsInterface)
                throw new InvalidOperationException(Properties.Messages.ErrInterfaceNoConversion);

            ResetAttrs();
            _mthFlags = MethodAttributes.SpecialName | MethodAttributes.Static;
            _mthVis = MethodAttributes.Public;
            return Method(TypeBuilder, "op_Explicit").Parameter(fromType, parameterName);
        }

#if FEAT_IKVM

        public MethodGen ExplicitConversionTo(System.Type toType)
        {
            return ExplicitConversionTo(TypeMapper.MapType(toType));
        }
        
#endif

        public MethodGen ExplicitConversionTo(Type toType)
        {
            return ExplicitConversionTo(toType, "value");
        }

#if FEAT_IKVM
        public MethodGen ExplicitConversionTo(System.Type toType, string parameterName)
        {
            return ExplicitConversionTo(TypeMapper.MapType(toType), parameterName);
        }

#endif

        public MethodGen ExplicitConversionTo(Type toType, string parameterName)
        {
            if (TypeBuilder.IsInterface)
                throw new InvalidOperationException(Properties.Messages.ErrInterfaceNoConversion);

            ResetAttrs();
            _mthFlags = MethodAttributes.SpecialName | MethodAttributes.Static;
            _mthVis = MethodAttributes.Public;
            return Method(toType, "op_Explicit").Parameter(TypeBuilder, parameterName);
        }

#if FEAT_IKVM

        public MethodGen Operator(Operator op, System.Type returnType, System.Type operandType)
        {
            return Operator(op, TypeMapper.MapType(returnType), TypeMapper.MapType(operandType));
        }
        
#endif

        public MethodGen Operator(Operator op, Type returnType, Type operandType)
        {
            return Operator(op, returnType, operandType, "operand");
        }

#if FEAT_IKVM

        public MethodGen Operator(Operator op, System.Type returnType, System.Type operandType, string operandName)
        {
            return Operator(op, TypeMapper.MapType(returnType), TypeMapper.MapType(operandType), operandName);
        }

        public MethodGen Operator(Operator op, Type returnType, System.Type operandType, string operandName)
        {
            return Operator(op, returnType, TypeMapper.MapType(operandType), operandName);
        }

        public MethodGen Operator(Operator op, System.Type returnType, Type operandType, string operandName)
        {
            return Operator(op, TypeMapper.MapType(returnType), operandType, operandName);
        }
        
#endif

        public MethodGen Operator(Operator op, Type returnType, Type operandType, string operandName)
        {
            if (op == null)
                throw new ArgumentNullException(nameof(op));

            ResetAttrs();
            _mthFlags = MethodAttributes.SpecialName | MethodAttributes.Static;
            _mthVis = MethodAttributes.Public;
            return Method(returnType, "op_" + op.MethodName).Parameter(operandType, operandName);
        }

#if FEAT_IKVM

        public MethodGen Operator(Operator op, System.Type returnType, System.Type leftType, System.Type rightType)
        {
            return Operator(op, TypeMapper.MapType(returnType), TypeMapper.MapType(leftType), TypeMapper.MapType(rightType));
        }
#endif


        public MethodGen Operator(Operator op, Type returnType, Type leftType, Type rightType)
        {
            return Operator(op, returnType, leftType, "left", rightType, "right");
        }

#if FEAT_IKVM

        public MethodGen Operator(Operator op, System.Type returnType, System.Type leftType, string leftName, System.Type rightType, string rightName)
        {
            return Operator(op, TypeMapper.MapType(returnType), TypeMapper.MapType(leftType), leftName, TypeMapper.MapType(rightType), rightName);
        }
        
#endif

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
            return Class(name, TypeMapper.MapType(typeof(object)), Type.EmptyTypes);
        }

#if FEAT_IKVM

        public TypeGen Class(string name, System.Type baseType)
        {
            return Class(name, TypeMapper.MapType(baseType));
        }
        
#endif

        public TypeGen Class(string name, Type baseType)
        {
            return Class(name, baseType, Type.EmptyTypes);
        }

#if FEAT_IKVM

        public TypeGen Class(string name, System.Type baseType, params Type[] interfaces)
        {
            return Class(name, TypeMapper.MapType(baseType), interfaces);
        }
        
#endif

        public TypeGen Class(string name, Type baseType, params Type[] interfaces)
        {
            if (TypeBuilder.IsInterface)
                throw new InvalidOperationException(Properties.Messages.ErrInterfaceNoNested);

            if (_typeVis == 0)
                _typeVis |= TypeAttributes.NestedPrivate;

            TypeGen tg = new TypeGen(this, name, (_typeVis | _typeVirt | _typeFlags | TypeAttributes.Class) ^ TypeAttributes.BeforeFieldInit, baseType, interfaces, _typeMapper);
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

            TypeGen tg = new TypeGen(this, name, (_typeVis | _typeVirt | _typeFlags | TypeAttributes.Sealed | TypeAttributes.SequentialLayout) ^ TypeAttributes.BeforeFieldInit, TypeMapper.MapType(typeof(ValueType)), interfaces, _typeMapper);
            ResetAttrs();
            return tg;
        }

#if FEAT_IKVM

        public DelegateGen Delegate(System.Type returnType, string name)
        {
            return Delegate(TypeMapper.MapType(returnType), name);
        }
        
#endif

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
                if (iie.InterfaceMethod.Member == methodDeclaration
                    && iie.InterfaceType == methodDeclaration.DeclaringType)
                {
                    iie.Bind(methodBody);
                    return;
                }
            }
        }

#if FEAT_IKVM

        public MethodGen MethodImplementation(System.Type interfaceType, System.Type returnType, string name)
        {
            return MethodImplementation(TypeMapper.MapType(interfaceType), TypeMapper.MapType(returnType), name);
        }

        public MethodGen MethodImplementation(System.Type interfaceType, Type returnType, string name)
        {
            return MethodImplementation(TypeMapper.MapType(interfaceType), returnType, name);
        }

        public MethodGen MethodImplementation(Type interfaceType, System.Type returnType, string name)
        {
            return MethodImplementation(interfaceType, TypeMapper.MapType(returnType), name);
        }
        
#endif

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

#if FEAT_IKVM

        public PropertyGen PropertyImplementation(System.Type interfaceType, System.Type type, string name)
        {
            return PropertyImplementation(TypeMapper.MapType(interfaceType), TypeMapper.MapType(type), name);
        }
#endif


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

#if FEAT_IKVM

        public EventGen EventImplementation(System.Type interfaceType, System.Type eventHandlerType, string name)
        {
            return EventImplementation(TypeMapper.MapType(interfaceType), TypeMapper.MapType(eventHandlerType), name);
        }
        
#endif

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

            if (_owner != null && TypeBuilder.IsValueType)
            {
                if (_fields.Count == 0 && _properties.Count == 0)
                {
                    // otherwise  "Value class has neither fields nor size parameter."
                    Private.ReadOnly.Field(_typeMapper.MapType(typeof(int)), "_____");
                }
            }

            foreach (TypeGen nested in _nestedTypes)
                nested.Complete();

            if (_owner != null)
            {
                // ensure creation of default constructor
                EnsureDefaultConstructor();
            }

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
                    TypeMapper.MapType(typeof(DefaultMemberAttribute)).GetConstructor(new Type[] { TypeMapper.MapType(typeof(string)) }),
                    new object[] { _indexerName });
                TypeBuilder.SetCustomAttribute(cab);
            }

            AttributeGen.ApplyList(ref _customAttributes, TypeBuilder.SetCustomAttribute);

            _type = TypeBuilder.CreateType();

            _typeMapper.TypeInfo.UnregisterProvider(TypeBuilder);
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
#if !SILVERLIGHT && !NET5_0 && !NETSTANDARD
            if (_owner != null && _owner.AssemblyBuilder.EntryPoint == null && method.Name == "Main" && method.IsStatic &&
                (
                    method.ParameterCount == 0 ||
                    (method.ParameterCount == 1 &&
                     method.ParameterTypes[0] == TypeMapper.MapType(typeof(string[])))))
            {
                _owner.AssemblyBuilder.SetEntryPoint(method.GetMethodBuilder());
            }
#endif
            // match explicit interface implementations
            if (method.ImplementedInterface != null)
            {
                foreach (IMemberInfo mi in _typeMapper.TypeInfo.Filter(_typeMapper.TypeInfo.GetMethods(method.ImplementedInterface), method.Name, false, false, true))
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
#endif
}
