// Modified by Vladyslav Taranov for AqlaSerializer, 2014
// Originally written in protobuf-net

using System;
using System.Collections;
using System.Diagnostics;

using Type = IKVM.Reflection.Type;
using IKVM.Reflection;
using IKVM.Reflection.Emit;


namespace AqlaSerializer.Compiler
{
    internal struct CodeLabel
    {
        public readonly Label Value;
        public readonly int Index;
        public CodeLabel(Label value, int index)
        {
            Value = value;
            Index = index;
        }
    }
    internal sealed class CompilerContext
    {
        internal CodeLabel DefineLabel()
        {
            CodeLabel result = new CodeLabel(_il.DefineLabel(), _nextLabel++);
            return result;
        }
        internal void MarkLabel(CodeLabel label)
        {
            _il.MarkLabel(label.Value);
#if DEBUG_COMPILE
            Debug.WriteLine("#: " + label.Index);
#endif
        }

        public void Return()
        {
            Emit(OpCodes.Ret);
        }

        static bool IsObject(Type type)
        {
            return type.FullName == "System.Object";
        }

        public void CastToObject(Type type)
        {
            if(IsObject(type))
            { }
            else if (type.IsValueType)
            {
                _il.Emit(OpCodes.Box, type);
#if DEBUG_COMPILE
                Debug.WriteLine(OpCodes.Box + ": " + type);
#endif
            }
            else
            {
                _il.Emit(OpCodes.Castclass, MapType(typeof(object)));
#if DEBUG_COMPILE
                Debug.WriteLine(OpCodes.Castclass + ": " + type);
#endif
            }
        }

        public void CastFromObject(Type type)
        {
            if (IsObject(type))
            { }
            else if (type.IsValueType)
            {
                
                        _il.Emit(OpCodes.Unbox_Any, type);
#if DEBUG_COMPILE
                        Debug.WriteLine(OpCodes.Unbox_Any + ": " + type);
#endif
            }
            else
            {
                _il.Emit(OpCodes.Castclass, type);
#if DEBUG_COMPILE
                Debug.WriteLine(OpCodes.Castclass + ": " + type);
#endif
            }
        }
        private readonly bool _isStatic;
        internal bool NonPublic { get; }

        public Local InputValue { get; }

        private readonly string _assemblyName;
        public CompilerContext(ILGenerator il, bool isStatic, string assemblyName, Type inputType, Universe universe, bool nonPublic)
        {
            if (il == null) throw new ArgumentNullException(nameof(il));
            if (string.IsNullOrEmpty(assemblyName)) throw new ArgumentNullException(nameof(assemblyName));
            _assemblyName = assemblyName;
            _universe = universe;
            NonPublic = nonPublic;
            _isStatic = isStatic;
            _il = il;
            if (inputType != null) InputValue = new Local(null, inputType);
            if (universe == null) throw new ArgumentNullException(nameof(universe));
        }

        private readonly ILGenerator _il;

        public void Emit(OpCode opcode)
        {
            _il.Emit(opcode);
#if DEBUG_COMPILE
            Debug.WriteLine(opcode.ToString());
#endif
        }
        public void LoadValue(string value)
        {
            if (value == null)
            {
                LoadNullRef();
            }
            else
            {
                _il.Emit(OpCodes.Ldstr, value);
#if DEBUG_COMPILE
                Debug.WriteLine(OpCodes.Ldstr + ": " + value);
#endif
            }
        }
        public void LoadValue(float value)
        {
            _il.Emit(OpCodes.Ldc_R4, value);
#if DEBUG_COMPILE
            Debug.WriteLine(OpCodes.Ldc_R4 + ": " + value);
#endif
        }
        public void LoadValue(double value)
        {
            _il.Emit(OpCodes.Ldc_R8, value);
#if DEBUG_COMPILE
            Debug.WriteLine(OpCodes.Ldc_R8 + ": " + value);
#endif
        }
        public void LoadValue(long value)
        {
            _il.Emit(OpCodes.Ldc_I8, value);
#if DEBUG_COMPILE
            Debug.WriteLine(OpCodes.Ldc_I8 + ": " + value);
#endif
        }

        public void LoadValue(bool value)
        {
            LoadValue(value ? 1 : 0);
        }

        public void LoadValue(int value)
        {
            switch (value)
            {
                case 0: Emit(OpCodes.Ldc_I4_0); break;
                case 1: Emit(OpCodes.Ldc_I4_1); break;
                case 2: Emit(OpCodes.Ldc_I4_2); break;
                case 3: Emit(OpCodes.Ldc_I4_3); break;
                case 4: Emit(OpCodes.Ldc_I4_4); break;
                case 5: Emit(OpCodes.Ldc_I4_5); break;
                case 6: Emit(OpCodes.Ldc_I4_6); break;
                case 7: Emit(OpCodes.Ldc_I4_7); break;
                case 8: Emit(OpCodes.Ldc_I4_8); break;
                case -1: Emit(OpCodes.Ldc_I4_M1); break;
                default:
                    if (value >= -128 && value <= 127)
                    {
                        _il.Emit(OpCodes.Ldc_I4_S, (sbyte)value);
#if DEBUG_COMPILE
                        Debug.WriteLine(OpCodes.Ldc_I4_S + ": " + value);
#endif
                    }
                    else
                    {
                        _il.Emit(OpCodes.Ldc_I4, value);
#if DEBUG_COMPILE
                        Debug.WriteLine(OpCodes.Ldc_I4 + ": " + value);
#endif
                    }
                    break;

            }
        }

        readonly ArrayList _locals = new ArrayList();
        internal LocalBuilder GetFromPool(Type type)
        {
            int count = _locals.Count;
            for (int i = 0; i < count; i++)
            {
                LocalBuilder item = (LocalBuilder)_locals[i];
                if (item != null && item.LocalType == type)
                {
                    _locals[i] = null; // remove from pool
                    return item;
                }
            }
            LocalBuilder result = _il.DeclareLocal(type);
#if DEBUG_COMPILE
            Debug.WriteLine("$ " + result + ": " + type);
#endif
            return result;
        }
        //
        internal void ReleaseToPool(LocalBuilder value)
        {
            int count = _locals.Count;
            for (int i = 0; i < count; i++)
            {
                if (_locals[i] == null)
                {
                    _locals[i] = value; // released into existing slot
                    return;
                }
            }
            _locals.Add(value); // create a new slot
        }
        
        public void StoreValue(Local local)
        {
            if (local == InputValue)
            {
                byte b = _isStatic ? (byte) 0 : (byte)1;
                _il.Emit(OpCodes.Starg_S, b);
#if DEBUG_COMPILE
                Debug.WriteLine(OpCodes.Starg_S + ": $" + b);
#endif
            }
            else
            {

                switch (local.Value.LocalIndex)
                {
                    case 0: Emit(OpCodes.Stloc_0); break;
                    case 1: Emit(OpCodes.Stloc_1); break;
                    case 2: Emit(OpCodes.Stloc_2); break;
                    case 3: Emit(OpCodes.Stloc_3); break;
                    default:
                        OpCode code = UseShortForm(local) ? OpCodes.Stloc_S : OpCodes.Stloc;
                        _il.Emit(code, local.Value);
#if DEBUG_COMPILE
                        Debug.WriteLine(code + ": $" + local.Value);
#endif
                        break;
                }
            }
        }

        public void LoadValue(Local local)
        {
            if (local == null) { /* nothing to do; top of stack */}
            else if (local == InputValue)
            {
                Emit(_isStatic ? OpCodes.Ldarg_0 : OpCodes.Ldarg_1);
            }
            else
            {
#if !FX11
                switch (local.Value.LocalIndex)
                {
                    case 0: Emit(OpCodes.Ldloc_0); break;
                    case 1: Emit(OpCodes.Ldloc_1); break;
                    case 2: Emit(OpCodes.Ldloc_2); break;
                    case 3: Emit(OpCodes.Ldloc_3); break;
                    default:
#endif
                        OpCode code = UseShortForm(local) ? OpCodes.Ldloc_S :  OpCodes.Ldloc;
                        _il.Emit(code, local.Value);
#if DEBUG_COMPILE
                        Debug.WriteLine(code + ": $" + local.Value);
#endif
                        break;
                }
            }
        }
        public Local GetLocalWithValue(Type type, Local fromValue)
        {
            if (fromValue != null)
            {
                if (fromValue.Type == type) return fromValue.AsCopy();
                // otherwise, load onto the stack and let the default handling (below) deal with it
                LoadValue(fromValue);
                if (!type.IsValueType && (fromValue.Type == null || !type.IsAssignableFrom(fromValue.Type)))
                { // need to cast
                    Cast(type);
                }
            }
            // need to store the value from the stack
            Local result = new Local(this, type);
            StoreValue(result);
            return result;
        }
        
        public void EmitCall(MethodInfo method)
        {
            CheckAccessibility(method);
            OpCode opcode = (method.IsStatic || method.DeclaringType.IsValueType) ? OpCodes.Call : OpCodes.Callvirt;
            _il.EmitCall(opcode, method, null);
#if DEBUG_COMPILE
            Debug.WriteLine(opcode + ": " + method + " on " + method.DeclaringType);
#endif
        }

        /// <summary>
        /// Pushes a null reference onto the stack. Note that this should only
        /// be used to return a null (or set a variable to null); for null-tests
        /// use BranchIfTrue / BranchIfFalse.
        /// </summary>
        public void LoadNullRef()
        {
            Emit(OpCodes.Ldnull);
        }

        private int _nextLabel;
        
        public void EmitCtor(Type type)
        {
            EmitCtor(type, new Type[0]);
        }

        public void EmitCtor(ConstructorInfo ctor)
        {
            if (ctor == null) throw new ArgumentNullException(nameof(ctor));
            CheckAccessibility(ctor);
            _il.Emit(OpCodes.Newobj, ctor);
#if DEBUG_COMPILE
            Debug.WriteLine(OpCodes.Newobj + ": " + ctor.DeclaringType);
#endif
        }

        public void EmitCtor(Type type, params Type[] parameterTypes)
        {
            if (type.IsValueType && parameterTypes.Length == 0)
            {
                _il.Emit(OpCodes.Initobj, type);
#if DEBUG_COMPILE
                Debug.WriteLine(OpCodes.Initobj + ": " + type);
#endif
            }
            else
            {
                ConstructorInfo ctor =  type.GetConstructor(parameterTypes);
                if (ctor == null) throw new InvalidOperationException("No suitable constructor found for " + type.FullName);
                EmitCtor(ctor);
            }
        }

        ArrayList _knownTrustedAssemblies, _knownUntrustedAssemblies;

        bool InternalsVisible(Assembly assembly)
        {
            if (string.IsNullOrEmpty(_assemblyName)) return false;
            if (_knownTrustedAssemblies?.IndexOf(assembly) >= 0)
            {
                return true;
            }
            if (_knownUntrustedAssemblies?.IndexOf(assembly) >= 0)
            {
                return false;
            }
            bool isTrusted = false;
            Type attributeType = MapType(typeof(System.Runtime.CompilerServices.InternalsVisibleToAttribute));
            if(attributeType == null) return false;

            foreach (CustomAttributeData attrib in assembly.__GetCustomAttributes(attributeType, false))
            {
                if (attrib.ConstructorArguments.Count == 1)
                {
                    string privelegedAssembly = attrib.ConstructorArguments[0].Value as string;
                    if (privelegedAssembly != null && (privelegedAssembly == _assemblyName || privelegedAssembly.StartsWith(_assemblyName + ",", StringComparison.Ordinal)))
                    {
                        isTrusted = true;
                        break;
                    }
                }
            }
            if (isTrusted)
            {
                if (_knownTrustedAssemblies == null) _knownTrustedAssemblies = new ArrayList();
                _knownTrustedAssemblies.Add(assembly);
            }
            else
            {
                if (_knownUntrustedAssemblies == null) _knownUntrustedAssemblies = new ArrayList();
                _knownUntrustedAssemblies.Add(assembly);
            }
            return isTrusted;
        }

        internal void CheckAccessibility(MemberInfo member)
        {
            if (member == null)
            {
                throw new ArgumentNullException(nameof(member));
            }

            MemberTypes memberType = member.MemberType;
            if (!NonPublic)
            {
                bool isPublic;
                Type type;
                switch (memberType)
                {
                    case MemberTypes.TypeInfo:
                        // top-level type
                        type = (Type)member;
                        isPublic = type.IsPublic || InternalsVisible(type.Assembly);
                        break;
                    case MemberTypes.NestedType:
                        type = (Type)member;
                        do
                        {
                            isPublic = type.IsNestedPublic || type.IsPublic || ((type.DeclaringType == null || type.IsNestedAssembly || type.IsNestedFamORAssem) && InternalsVisible(type.Assembly));
                        } while (isPublic && (type = type.DeclaringType) != null); // ^^^ !type.IsNested, but not all runtimes have that
                        break;
                    case MemberTypes.Field:
                        FieldInfo field = ((FieldInfo)member);
                        isPublic = field.IsPublic || ((field.IsAssembly || field.IsFamilyOrAssembly) && InternalsVisible(field.DeclaringType.Assembly));
                        break;
                    case MemberTypes.Constructor:
                        ConstructorInfo ctor = ((ConstructorInfo)member);
                        isPublic = ctor.IsPublic || ((ctor.IsAssembly || ctor.IsFamilyOrAssembly) && InternalsVisible(ctor.DeclaringType.Assembly));
                        break;
                    case MemberTypes.Method:
                        MethodInfo method = ((MethodInfo)member);
                        isPublic = method.IsPublic || ((method.IsAssembly || method.IsFamilyOrAssembly) && InternalsVisible(method.DeclaringType.Assembly));
                        if (!isPublic)
                        {
                            // TODO allow calls to TypeModel protected methods, and methods we are in the process of creating
                            if(member is MethodBuilder) isPublic = true; 
                        }
                        break;
                    case MemberTypes.Property:
                        isPublic = true; // defer to get/set
                        break;
                    default:
                        throw new NotSupportedException(memberType.ToString());
                }
                if (!isPublic)
                {
                    switch (memberType)
                    {
                        case MemberTypes.TypeInfo:
                        case MemberTypes.NestedType:
                            throw new InvalidOperationException("Non-public type cannot be used with full dll compilation: " +
                                ((Type)member).FullName);
                        default:
                            throw new InvalidOperationException("Non-public member cannot be used with full dll compilation: " +
                                member.DeclaringType.FullName + "." + member.Name);
                    }
                    
                }
            }
        }

        public void LoadValue(FieldInfo field)
        {
            CheckAccessibility(field);
            OpCode code = field.IsStatic ? OpCodes.Ldsfld : OpCodes.Ldfld;
            _il.Emit(code, field);
#if DEBUG_COMPILE
            Debug.WriteLine(code + ": " + field + " on " + field.DeclaringType);
#endif
        }

        public void StoreValue(System.Reflection.FieldInfo field)
        {
            StoreValue(MapType(field.DeclaringType).GetField(field.Name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance));
        }
        public void StoreValue(System.Reflection.PropertyInfo property)
        {
            StoreValue(MapType(property.DeclaringType).GetProperty(property.Name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance));
        }
        public void LoadValue(System.Reflection.FieldInfo field)
        {
            LoadValue(MapType(field.DeclaringType).GetField(field.Name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance));
        }
        public void LoadValue(System.Reflection.PropertyInfo property)
        {
            LoadValue(MapType(property.DeclaringType).GetProperty(property.Name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance));
        }

        public void StoreValue(FieldInfo field)
        {
            CheckAccessibility(field);
            OpCode code = field.IsStatic ? OpCodes.Stsfld : OpCodes.Stfld;
            _il.Emit(code, field);
#if DEBUG_COMPILE
            Debug.WriteLine(code + ": " + field + " on " + field.DeclaringType);
#endif
        }
        public void LoadValue(PropertyInfo property)
        {
            CheckAccessibility(property);
            EmitCall(property.GetGetMethod(true));
        }
        public void StoreValue(PropertyInfo property)
        {
            CheckAccessibility(property);
            EmitCall(property.GetSetMethod(true));
        }

        public static void LoadValue(ILGenerator il, int value)
        {
            switch (value)
            {
                case 0: il.Emit(OpCodes.Ldc_I4_0); break;
                case 1: il.Emit(OpCodes.Ldc_I4_1); break;
                case 2: il.Emit(OpCodes.Ldc_I4_2); break;
                case 3: il.Emit(OpCodes.Ldc_I4_3); break;
                case 4: il.Emit(OpCodes.Ldc_I4_4); break;
                case 5: il.Emit(OpCodes.Ldc_I4_5); break;
                case 6: il.Emit(OpCodes.Ldc_I4_6); break;
                case 7: il.Emit(OpCodes.Ldc_I4_7); break;
                case 8: il.Emit(OpCodes.Ldc_I4_8); break;
                case -1: il.Emit(OpCodes.Ldc_I4_M1); break;
                default: il.Emit(OpCodes.Ldc_I4, value); break;
            }
        }

        private bool UseShortForm(Local local)
        {
#if FX11
            return locals.Count < 256;
#else
            return local.Value.LocalIndex < 256;
#endif
        }

        public void LoadAddress(Local local, System.Type type)
        {
            LoadAddress(local, MapType(type));
        }

        public void LoadAddress(Local local, Type type)
        {
            if (type.IsValueType)
            {
                if (local == null)
                {
                    throw new InvalidOperationException("Cannot load the address of a struct at the head of the stack");
                }

                if (local == InputValue)
                {
                    _il.Emit(OpCodes.Ldarga_S, (_isStatic ? (byte)0 : (byte)1));
#if DEBUG_COMPILE
                    Debug.WriteLine(OpCodes.Ldarga_S + ": $" + (isStatic ? 0 : 1));
#endif
                }
                else
                {
                    OpCode code = UseShortForm(local) ? OpCodes.Ldloca_S : OpCodes.Ldloca;
                    _il.Emit(code, local.Value);
#if DEBUG_COMPILE
                    Debug.WriteLine(code + ": $" + local.Value);
#endif
                }

            }
            else
            {   // reference-type; already *is* the address; just load it
                LoadValue(local);
            }
        }
        public void Branch(CodeLabel label, bool @short)
        {
            OpCode code = @short ? OpCodes.Br_S : OpCodes.Br;
            _il.Emit(code, label.Value);
#if DEBUG_COMPILE
            Debug.WriteLine(code + ": " + label.Index);
#endif
        }
        public void BranchIfFalse(CodeLabel label, bool @short)
        {
            OpCode code = @short ? OpCodes.Brfalse_S :  OpCodes.Brfalse;
            _il.Emit(code, label.Value);
#if DEBUG_COMPILE
            Debug.WriteLine(code + ": " + label.Index);
#endif
        }


        public void BranchIfTrue(CodeLabel label, bool @short)
        {
            OpCode code = @short ? OpCodes.Brtrue_S : OpCodes.Brtrue;
            _il.Emit(code, label.Value);
#if DEBUG_COMPILE
            Debug.WriteLine(code + ": " + label.Index);
#endif
        }
        public void BranchIfEqual(CodeLabel label, bool @short)
        {
            OpCode code = @short ? OpCodes.Beq_S : OpCodes.Beq;
            _il.Emit(code, label.Value);
#if DEBUG_COMPILE
            Debug.WriteLine(code + ": " + label.Index);
#endif
        }

        internal void CopyValue()
        {
            Emit(OpCodes.Dup);
        }

        public void BranchIfGreater(CodeLabel label, bool @short)
        {
            OpCode code = @short ? OpCodes.Bgt_S : OpCodes.Bgt;
            _il.Emit(code, label.Value);
#if DEBUG_COMPILE
            Debug.WriteLine(code + ": " + label.Index);
#endif
        }

        public void BranchIfLess(CodeLabel label, bool @short)
        {
            OpCode code = @short ? OpCodes.Blt_S : OpCodes.Blt;
            _il.Emit(code, label.Value);
#if DEBUG_COMPILE
            Debug.WriteLine(code + ": " + label.Index);
#endif
        }

        internal void DiscardValue()
        {
            Emit(OpCodes.Pop);
        }

        public void Subtract()
        {
            Emit(OpCodes.Sub);
        }
        
        public void Switch(CodeLabel[] jumpTable)
        {
            const int maxJumps = 128;

            if (jumpTable.Length <= maxJumps)
            {
                // simple case
                Label[] labels = new Label[jumpTable.Length];
                for (int i = 0; i < labels.Length; i++)
                {
                    labels[i] = jumpTable[i].Value;
                }
#if DEBUG_COMPILE
                Debug.WriteLine(OpCodes.Switch.ToString());
#endif
                _il.Emit(OpCodes.Switch, labels);
            }
            else
            {
                // too many to jump easily (especially on Android) - need to split up (note: uses a local pulled from the stack)
                using (Local val = GetLocalWithValue(MapType(typeof(int)), null))
                {
                    int count = jumpTable.Length, offset = 0;
                    int blockCount = count / maxJumps;
                    if ((count % maxJumps) != 0) blockCount++;

                    Label[] blockLabels = new Label[blockCount];
                    for (int i = 0; i < blockCount; i++)
                    {
                        blockLabels[i] = _il.DefineLabel();
                    }
                    CodeLabel endOfSwitch = DefineLabel();
                    
                    LoadValue(val);
                    LoadValue(maxJumps);
                    Emit(OpCodes.Div);
#if DEBUG_COMPILE
                Debug.WriteLine(OpCodes.Switch.ToString());
#endif
                    _il.Emit(OpCodes.Switch, blockLabels);
                    Branch(endOfSwitch, false);

                    Label[] innerLabels = new Label[maxJumps];
                    for (int blockIndex = 0; blockIndex < blockCount; blockIndex++)
                    {
                        _il.MarkLabel(blockLabels[blockIndex]);

                        int itemsThisBlock = Math.Min(maxJumps, count);
                        count -= itemsThisBlock;
                        if (innerLabels.Length != itemsThisBlock) innerLabels = new Label[itemsThisBlock];

                        int subtract = offset;
                        for (int j = 0; j < itemsThisBlock; j++)
                        {
                            innerLabels[j] = jumpTable[offset++].Value;
                        }
                        LoadValue(val);
                        if (subtract != 0) // switches are always zero-based
                        {
                            LoadValue(subtract);
                            Emit(OpCodes.Sub);
                        }
#if DEBUG_COMPILE
                        Debug.WriteLine(OpCodes.Switch.ToString());
#endif
                        _il.Emit(OpCodes.Switch, innerLabels);
                        if (count != 0)
                        { // force default to the very bottom
                            Branch(endOfSwitch, false);
                        }
                    }
                    Debug.Assert(count == 0, "Should use exactly all switch items");
                    MarkLabel(endOfSwitch);
                }
            }
        }

        internal void EndFinally()
        {
            _il.EndExceptionBlock();
#if DEBUG_COMPILE
            Debug.WriteLine("EndExceptionBlock");
#endif
        }

        internal void BeginFinally()
        {
            _il.BeginFinallyBlock();
#if DEBUG_COMPILE
            Debug.WriteLine("BeginFinallyBlock");
#endif
        }

        internal void EndTry(CodeLabel label, bool @short)
        {
            OpCode code = @short ? OpCodes.Leave_S : OpCodes.Leave;
            _il.Emit(code, label.Value);
#if DEBUG_COMPILE
            Debug.WriteLine(code + ": " + label.Index);
#endif
        }

        internal CodeLabel BeginTry()
        {
            CodeLabel label = new CodeLabel(_il.BeginExceptionBlock(), _nextLabel++);
#if DEBUG_COMPILE
            Debug.WriteLine("BeginExceptionBlock: " + label.Index);
#endif
            return label;
        }
#if !FX11
        internal void Constrain(Type type)
        {
            _il.Emit(OpCodes.Constrained, type);
#if DEBUG_COMPILE
            Debug.WriteLine(OpCodes.Constrained + ": " + type);
#endif
        }
#endif

        internal void TryCast(Type type)
        {
            _il.Emit(OpCodes.Isinst, type);
#if DEBUG_COMPILE
            Debug.WriteLine(OpCodes.Isinst + ": " + type);
#endif
        }

        internal void Cast(Type type)
        {
            _il.Emit(OpCodes.Castclass, type);
#if DEBUG_COMPILE
            Debug.WriteLine(OpCodes.Castclass + ": " + type);
#endif
        }
        public IDisposable Using(Local local)
        {
            return new UsingBlock(this, local);
        }
        private sealed class UsingBlock : IDisposable{
            private Local _local;
            CompilerContext _ctx;
            CodeLabel _label;
            /// <summary>
            /// Creates a new "using" block (equivalent) around a variable;
            /// the variable must exist, and note that (unlike in C#) it is
            /// the variables *final* value that gets disposed. If you need
            /// *original* disposal, copy your variable first.
            /// 
            /// It is the callers responsibility to ensure that the variable's
            /// scope fully-encapsulates the "using"; if not, the variable
            /// may be re-used (and thus re-assigned) unexpectedly.
            /// </summary>
            public UsingBlock(CompilerContext ctx, Local local)
            {
                if (ctx == null) throw new ArgumentNullException(nameof(ctx));
                if (local == null) throw new ArgumentNullException(nameof(local));

                Type type = local.Type;
                // check if **never** disposable
                if ((type.IsValueType || type.IsSealed) &&
                    !ctx.MapType(typeof(IDisposable)).IsAssignableFrom(type))
                {
                    return; // nothing to do! easiest "using" block ever
                    // (note that C# wouldn't allow this as a "using" block,
                    // but we'll be generous and simply not do anything)
                }
                _local = local;
                _ctx = ctx;
                _label = ctx.BeginTry();
                
            }
            public void Dispose()
            {
                if (_local == null || _ctx == null) return;

                _ctx.EndTry(_label, false);
                _ctx.BeginFinally();
                Type disposableType = _ctx.MapType(typeof (IDisposable));
                MethodInfo dispose = disposableType.GetMethod("Dispose");
                Type type = _local.Type;
                // remember that we've already (in the .ctor) excluded the case
                // where it *cannot* be disposable
                if (type.IsValueType)
                {
                    _ctx.LoadAddress(_local, type);

                    _ctx.Constrain(type);
                    
                    _ctx.EmitCall(dispose);                    
                }
                else
                {
                    CodeLabel @null = _ctx.DefineLabel();
                    if (disposableType.IsAssignableFrom(type))
                    {   // *known* to be IDisposable; just needs a null-check                            
                        _ctx.LoadValue(_local);
                        _ctx.BranchIfFalse(@null, true);
                        _ctx.LoadAddress(_local, type);
                    }
                    else
                    {   // *could* be IDisposable; test via "as"
                        using (Local disp = new Local(_ctx, disposableType))
                        {
                            _ctx.LoadValue(_local);
                            _ctx.TryCast(disposableType);
                            _ctx.CopyValue();
                            _ctx.StoreValue(disp);
                            _ctx.BranchIfFalse(@null, true);
                            _ctx.LoadAddress(disp, disposableType);
                        }
                    }
                    _ctx.EmitCall(dispose);
                    _ctx.MarkLabel(@null);
                }
                _ctx.EndFinally();
                _local = null;
                _ctx = null;
                _label = new CodeLabel(); // default
            }
        }

        public void Add()
        {
            Emit(OpCodes.Add);
        }

        public void LoadLength(Local arr, bool zeroIfNull)
        {
            Debug.Assert(arr.Type.IsArray && arr.Type.GetArrayRank() == 1);

            if (zeroIfNull)
            {
                CodeLabel notNull = DefineLabel(), done = DefineLabel();
                LoadValue(arr);
                CopyValue(); // optimised for non-null case
                BranchIfTrue(notNull, true);
                DiscardValue();
                LoadValue(0);
                Branch(done, true);
                MarkLabel(notNull);
                Emit(OpCodes.Ldlen);
                Emit(OpCodes.Conv_I4);
                MarkLabel(done);
            }
            else
            {
                LoadValue(arr);
                Emit(OpCodes.Ldlen);
                Emit(OpCodes.Conv_I4);
            }
        }

        public void CreateArray(Type elementType, Local length)
        {
            LoadValue(length);
            _il.Emit(OpCodes.Newarr, elementType);
#if DEBUG_COMPILE
            Debug.WriteLine(OpCodes.Newarr + ": " + elementType);
#endif

        }

        public void LoadArrayValue(Local arr, Local i)
        {
            Type type = arr.Type;
            Debug.Assert(type.IsArray && arr.Type.GetArrayRank() == 1);
            type = type.GetElementType();
            Debug.Assert(type != null, "Not an array: " + arr.Type.FullName);
            LoadValue(arr);
            LoadValue(i);
            string fullName = type.FullName;
            if (!type.IsPrimitive) fullName = string.Empty;
            switch(fullName) {
                case "System.SByte": Emit(OpCodes.Ldelem_I1); break;
                case "System.Int16": Emit(OpCodes.Ldelem_I2); break;
                case "System.Int32": Emit(OpCodes.Ldelem_I4); break;
                case "System.Int64": Emit(OpCodes.Ldelem_I8); break;

                case "System.Byte": Emit(OpCodes.Ldelem_U1); break;
                case "System.UInt16": Emit(OpCodes.Ldelem_U2); break;
                case "System.UInt32": Emit(OpCodes.Ldelem_U4); break;
                case "System.UInt64": Emit(OpCodes.Ldelem_I8); break; // odd, but this is what C# does...
                    
                case "System.Single": Emit(OpCodes.Ldelem_R4); break;
                case "System.Double": Emit(OpCodes.Ldelem_R8); break;
                default:
                    if (type.IsValueType)
                    {
                        _il.Emit(OpCodes.Ldelema, type);
                        _il.Emit(OpCodes.Ldobj, type);
#if DEBUG_COMPILE
                        Debug.WriteLine(OpCodes.Ldelema + ": " + type);
                        Debug.WriteLine(OpCodes.Ldobj + ": " + type);
#endif
                    }
                    else
                    {
                        Emit(OpCodes.Ldelem_Ref);
                    }
             
                    break;
            }
            
        }


        public void LoadValue(Type type)
        {
            _il.Emit(OpCodes.Ldtoken, type);
#if DEBUG_COMPILE
            Debug.WriteLine(OpCodes.Ldtoken + ": " + type);
#endif
            EmitCall(MapType(typeof(System.Type)).GetMethod("GetTypeFromHandle"));
        }

        public void ConvertToInt32(TypeCode typeCode, bool uint32Overflow)
        {
            switch (typeCode)
            {
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.Int16:
                case TypeCode.UInt16:
                    Emit(OpCodes.Conv_I4);
                    break;
                case TypeCode.Int32:
                    break;
                case TypeCode.Int64:
                    Emit(OpCodes.Conv_Ovf_I4);
                    break;
                case TypeCode.UInt32:
                    Emit(uint32Overflow ? OpCodes.Conv_Ovf_I4_Un : OpCodes.Conv_Ovf_I4);
                    break;
                case TypeCode.UInt64:
                    Emit(OpCodes.Conv_Ovf_I4_Un);
                    break;
                default:
                    throw new InvalidOperationException("ConvertToInt32 not implemented for: " + typeCode.ToString());
            }
        }

        public void ConvertFromInt32(TypeCode typeCode, bool uint32Overflow)
        {
            switch (typeCode)
            {
                case TypeCode.SByte: Emit(OpCodes.Conv_Ovf_I1); break;
                case TypeCode.Byte: Emit(OpCodes.Conv_Ovf_U1); break;
                case TypeCode.Int16: Emit(OpCodes.Conv_Ovf_I2); break;
                case TypeCode.UInt16: Emit(OpCodes.Conv_Ovf_U2); break;
                case TypeCode.Int32: break;
                case TypeCode.UInt32: Emit(uint32Overflow ? OpCodes.Conv_Ovf_U4 : OpCodes.Conv_U4); break;
                case TypeCode.Int64: Emit(OpCodes.Conv_I8); break;
                case TypeCode.UInt64: Emit(OpCodes.Conv_U8); break;
                default: throw new InvalidOperationException();
            }
        }

        public void LoadValue(decimal value)
        {
            if (value == 0M)
            {
                LoadValue(typeof(decimal).GetField("Zero"));
            }
            else
            {
                int[] bits = decimal.GetBits(value);
                LoadValue(bits[0]); // lo
                LoadValue(bits[1]); // mid
                LoadValue(bits[2]); // hi
                LoadValue((int)(((uint)bits[3]) >> 31)); // isNegative (bool, but int for CLI purposes)
                LoadValue((bits[3] >> 16) & 0xFF); // scale (byte, but int for CLI purposes)

                EmitCtor(MapType(typeof(decimal)), MapType(typeof(int)), MapType(typeof(int)), MapType(typeof(int)), MapType(typeof(bool)), MapType(typeof(byte)));
            }
        }

        public void LoadValue(Guid value)
        {
            if (value == Guid.Empty)
            {
                LoadValue(typeof(Guid).GetField("Empty"));
            }
            else
            { // note we're adding lots of shorts/bytes here - but at the IL level they are I4, not I1/I2 (which barely exist)
                byte[] bytes = value.ToByteArray();
                int i = (bytes[0]) | (bytes[1] << 8) | (bytes[2] << 16) | (bytes[3] << 24);
                LoadValue(i);
                short s = (short)((bytes[4]) | (bytes[5] << 8));
                LoadValue(s);
                s = (short)((bytes[6]) | (bytes[7] << 8));
                LoadValue(s);
                for (i = 8; i <= 15; i++)
                {
                    LoadValue(bytes[i]);
                }
                EmitCtor(MapType(typeof(Guid)), MapType(typeof(int)), MapType(typeof(short)), MapType(typeof(short)), MapType(typeof(byte)), MapType(typeof(byte)), MapType(typeof(byte)), MapType(typeof(byte)), MapType(typeof(byte)), MapType(typeof(byte)), MapType(typeof(byte)), MapType(typeof(byte)));
            }
        }
        readonly Universe _universe;
        public Type MapType(System.Type type)
        {
            Type result = _universe.GetType(type.AssemblyQualifiedName);

            if (result == null)
            {
                // things also tend to move around... *a lot* - especially in WinRT; search all as a fallback strategy
                foreach (Assembly a in _universe.GetAssemblies())
                {
                    result = a.GetType(type.FullName);
                    if (result != null) break;
                }
            }
            return result;
        }
        
        internal bool AllowInternal(PropertyInfo property)
        {
            return NonPublic || InternalsVisible(property.DeclaringType.Assembly);
        }
    }
}