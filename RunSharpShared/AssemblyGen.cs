/*
Copyright(c) 2009, Stefan Simek
Copyright(c) 2015, Vladyslav Taranov

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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.IO;
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
using Universe = System.AppDomain;
#endif

namespace TriAxis.RunSharp
{
    public class AssemblyGen : ICodeGenBasicContext
	{
        readonly List<TypeGen> _types = new List<TypeGen>();
		List<AttributeGen> _assemblyAttributes;
		List<AttributeGen> _moduleAttributes;
		string _ns;
        
		internal AssemblyBuilder AssemblyBuilder { get; set; }
        internal ModuleBuilder ModuleBuilder { get; set; }
        public ExpressionFactory ExpressionFactory { get; private set; }
        public StaticFactory StaticFactory { get; private set; }

        internal void AddType(TypeGen tg)
		{
			_types.Add(tg);
		}

		class NamespaceContext : IDisposable
		{
		    readonly AssemblyGen _ag;
		    readonly string _oldNs;

			public NamespaceContext(AssemblyGen ag)
			{
				_ag = ag;
				_oldNs = ag._ns;
			}

			public void Dispose()
			{
				_ag._ns = _oldNs;
			}
		}

		public IDisposable Namespace(string name)
		{
			NamespaceContext nc = new NamespaceContext(this);
			_ns = Qualify(name);
			return nc;
		}

		string Qualify(string name)
		{
			if (_ns == null)
				return name;
			else
				return _ns + "." + name;
		}

		#region Modifiers
		TypeAttributes _attrs;

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public AssemblyGen Public { get { _attrs |= TypeAttributes.Public; return this; } }
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public AssemblyGen Private { get { _attrs |= TypeAttributes.NotPublic; return this; } }
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public AssemblyGen Sealed { get { _attrs |= TypeAttributes.Sealed; return this; } }
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public AssemblyGen Abstract { get { _attrs |= TypeAttributes.Abstract; return this; } }
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public AssemblyGen NoBeforeFieldInit { get { _attrs |= TypeAttributes.BeforeFieldInit; return this; } }
		#endregion

		#region Custom Attributes

		public AssemblyGen Attribute(AttributeType type)
		{
			BeginAttribute(type);
			return this;
		}

		public AssemblyGen Attribute(AttributeType type, params object[] args)
		{
			BeginAttribute(type, args);
			return this;
		}

		public AttributeGen<AssemblyGen> BeginAttribute(AttributeType type)
		{
			return BeginAttribute(type, EmptyArray<object>.Instance);
		}

		public AttributeGen<AssemblyGen> BeginAttribute(AttributeType type, params object[] args)
		{
			return AttributeGen<AssemblyGen>.CreateAndAdd(this, ref _assemblyAttributes, AttributeTargets.Assembly, type, args, TypeMapper);
		}

		public AssemblyGen ModuleAttribute(AttributeType type)
		{
			BeginModuleAttribute(type);
			return this;
		}

		public AssemblyGen ModuleAttribute(AttributeType type, params object[] args)
		{
			BeginModuleAttribute(type, args);
			return this;
		}

		public AttributeGen<AssemblyGen> BeginModuleAttribute(AttributeType type)
		{
			return BeginModuleAttribute(type, EmptyArray<object>.Instance);
		}

		public AttributeGen<AssemblyGen> BeginModuleAttribute(AttributeType type, params object[] args)
		{
			return AttributeGen<AssemblyGen>.CreateAndAdd(this, ref _moduleAttributes, AttributeTargets.Module, type, args, TypeMapper);
		}

		#endregion

		#region Types
		public TypeGen Class(string name)
		{
			return Class(name, TypeMapper.MapType(typeof(object)));
		}

#if FEAT_IKVM

        public TypeGen Class(System.Type baseType, string name)
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
			TypeGen tg = new TypeGen(this, Qualify(name), (_attrs | TypeAttributes.Class) ^ TypeAttributes.BeforeFieldInit, baseType, interfaces, TypeMapper);
			_attrs = 0;
			return tg;
		}

		public TypeGen Struct(string name)
		{
			return Struct(name, Type.EmptyTypes);
		}

		public TypeGen Struct(string name, params Type[] interfaces)
		{
			TypeGen tg = new TypeGen(this, Qualify(name), (_attrs | TypeAttributes.Sealed | TypeAttributes.SequentialLayout) ^ TypeAttributes.BeforeFieldInit, TypeMapper.MapType(typeof(ValueType)), interfaces, TypeMapper);
			_attrs = 0;
			return tg;
		}

		public TypeGen Interface(string name)
		{
			return Interface(name, Type.EmptyTypes);
		}

		public TypeGen Interface(string name, params Type[] interfaces)
		{
			TypeGen tg = new TypeGen(this, Qualify(name), (_attrs | TypeAttributes.Interface | TypeAttributes.Abstract) & ~(TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit), null, interfaces, TypeMapper);
			_attrs = 0;
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
			return new DelegateGen(this, Qualify(name), returnType, (_attrs | TypeAttributes.Sealed) & ~(TypeAttributes.Abstract | TypeAttributes.BeforeFieldInit));
		}
        #endregion

        #region Construction

        AssemblyBuilderAccess _access;
        public ITypeMapper TypeMapper { get; private set; }
        public Universe Universe { get; private set; }

#if !FEAT_IKVM

        public AssemblyGen(string name, CompilerOptions options, ITypeMapper typeMapper = null)
        {
            Initialize(AppDomain.CurrentDomain, name, !Helpers.IsNullOrEmpty(options.OutputPath) ?AssemblyBuilderAccess.RunAndSave : AssemblyBuilderAccess.Run, options, typeMapper);

        }

        public AssemblyGen(AppDomain domain, string name, CompilerOptions options, ITypeMapper typeMapper = null)
        {
            Initialize(domain, name, AssemblyBuilderAccess.Run, options, typeMapper);
        }

#else
        public AssemblyGen(string assemblyName, CompilerOptions options)
            : this(new Universe(), assemblyName, options)
        {
        }

        public AssemblyGen(Universe universe, string assemblyName, CompilerOptions options, ITypeMapper typeMapper = null)
	    {
	        Initialize(universe, assemblyName, AssemblyBuilderAccess.Save, options, typeMapper);
	    }

        private byte[] FromHex(string value)
        {
            if (Helpers.IsNullOrEmpty(value)) throw new ArgumentNullException("value");
            int len = value.Length / 2;
            byte[] result = new byte[len];
            for (int i = 0; i < len; i++)
            {
                result[i] = Convert.ToByte(value.Substring(i * 2, 2), 16);
            }
            return result;
        }
#endif
        string _fileName;

        void Initialize(Universe universe, string assemblyName, AssemblyBuilderAccess access, CompilerOptions options, ITypeMapper typeMapper = null)
        {
            if (universe == null) throw new ArgumentNullException(nameof(universe));
            if (options == null) throw new ArgumentNullException(nameof(options));
            _compilerOptions = options;
            if (typeMapper == null)
#if FEAT_IKVM
                typeMapper = new TypeMapper(universe);
#else
                typeMapper = new TypeMapper();
#endif
            ExpressionFactory = new ExpressionFactory(typeMapper);
            StaticFactory = new StaticFactory(typeMapper);

            bool save = (access & AssemblyBuilderAccess.Save) != 0;
            string path = options.OutputPath;
            if (path == null && save) throw new ArgumentNullException("options.OutputPath");

            Universe = universe;

            TypeMapper = typeMapper;
            _access = access;

            if (Helpers.IsNullOrEmpty(assemblyName))
            {
                if (save) throw new ArgumentNullException(nameof(assemblyName));
                assemblyName = Guid.NewGuid().ToString();
            }
            
            string moduleName = path == null ? assemblyName : assemblyName + Path.GetExtension(path);

            _fileName = path;

            AssemblyName an = new AssemblyName();
            an.Name = assemblyName;

            AssemblyBuilder = path != null
                                  ? Universe.DefineDynamicAssembly(an, access, Path.GetDirectoryName(path))
                                  : Universe.DefineDynamicAssembly(an, access);
#if FEAT_IKVM
            if (!Helpers.IsNullOrEmpty(options.KeyFile))
            {
               AssemblyBuilder.__SetAssemblyKeyPair(new StrongNameKeyPair(File.OpenRead(options.KeyFile)));
            }
            else if (!Helpers.IsNullOrEmpty(options.KeyContainer))
            {
                AssemblyBuilder.__SetAssemblyKeyPair(new StrongNameKeyPair(options.KeyContainer));
            }
            else if (!Helpers.IsNullOrEmpty(options.PublicKey))
            {
                AssemblyBuilder.__SetAssemblyPublicKey(FromHex(options.PublicKey));
            }
            if (!Helpers.IsNullOrEmpty(options.ImageRuntimeVersion) && options.MetaDataVersion != 0)
            {
                AssemblyBuilder.__SetImageRuntimeVersion(options.ImageRuntimeVersion, options.MetaDataVersion);
            }
            ModuleBuilder = AssemblyBuilder.DefineDynamicModule(moduleName, path, options.SymbolInfo);
#else
            ModuleBuilder = save ? AssemblyBuilder.DefineDynamicModule(moduleName, Path.GetFileName(path)) : AssemblyBuilder.DefineDynamicModule(moduleName);
#endif
        }


        public void Save()
		{
			Complete();

			if ((_access & AssemblyBuilderAccess.Save) != 0)
#if FEAT_IKVM
                AssemblyBuilder.Save(_fileName);
#else
                AssemblyBuilder.Save(Path.GetFileName(_fileName));
#endif
        }

        public Assembly GetAssembly()
		{
			Complete();
			return AssemblyBuilder;
		}


        private void WriteAssemblyAttributes(CompilerOptions options, string assemblyName, AssemblyBuilder asm)
        {
            if (!Helpers.IsNullOrEmpty(options.TargetFrameworkName))
            {
                // get [TargetFramework] from mscorlib/equivalent and burn into the new assembly
                Type versionAttribType = null;
                try
                { // this is best-endeavours only
                    versionAttribType = TypeMapper.GetType("System.Runtime.Versioning.TargetFrameworkAttribute", TypeMapper.MapType(typeof(string)).Assembly);
                }
                catch { /* don't stress */ }
                if (versionAttribType != null)
                {
                    PropertyInfo[] props;
                    object[] propValues;
                    if (Helpers.IsNullOrEmpty(options.TargetFrameworkDisplayName))
                    {
                        props = new PropertyInfo[0];
                        propValues = new object[0];
                    }
                    else
                    {
                        props = new PropertyInfo[1] { versionAttribType.GetProperty("FrameworkDisplayName") };
                        propValues = new object[1] { options.TargetFrameworkDisplayName };
                    }
                    CustomAttributeBuilder builder = new CustomAttributeBuilder(
                        versionAttribType.GetConstructor(new Type[] { TypeMapper.MapType(typeof(string)) }),
                        new object[] { options.TargetFrameworkName },
                        props,
                        propValues);
                    asm.SetCustomAttribute(builder);
                }
            }

            // copy assembly:InternalsVisibleTo
            Type internalsVisibleToAttribType = null;
#if !FX11
            try
            {
                internalsVisibleToAttribType = TypeMapper.MapType(typeof(System.Runtime.CompilerServices.InternalsVisibleToAttribute));
            }
            catch { /* best endeavors only */ }
#endif
            if (internalsVisibleToAttribType != null)
            {
                List<string> internalAssemblies = new List<string>();
                List<Assembly> consideredAssemblies = new List<Assembly>();
                foreach (Type type in _types)
                {
                    Assembly assembly = type.Assembly;
                    if (consideredAssemblies.IndexOf(assembly) >= 0) continue;
                    consideredAssemblies.Add(assembly);

                    AttributeMap[] assemblyAttribsMap = AttributeMap.Create(TypeMapper, assembly);
                    for (int i = 0; i < assemblyAttribsMap.Length; i++)
                    {

                        if (assemblyAttribsMap[i].AttributeType != internalsVisibleToAttribType) continue;

                        object privelegedAssemblyObj;
                        assemblyAttribsMap[i].TryGet("AssemblyName", out privelegedAssemblyObj);
                        string privelegedAssemblyName = (string)privelegedAssemblyObj;
                        if (privelegedAssemblyName == assemblyName || Helpers.IsNullOrEmpty(privelegedAssemblyName)) continue; // ignore

                        if (internalAssemblies.IndexOf(privelegedAssemblyName) >= 0) continue; // seen it before
                        internalAssemblies.Add(privelegedAssemblyName);

                        CustomAttributeBuilder builder = new CustomAttributeBuilder(
                            internalsVisibleToAttribType.GetConstructor(new Type[] { TypeMapper.MapType(typeof(string)) }),
                            new object[] { privelegedAssemblyName });
                        asm.SetCustomAttribute(builder);
                    }
                }
            }
        }

        CompilerOptions _compilerOptions;

        public void Complete()
		{
			foreach (TypeGen tg in _types)
				tg.Complete();

			AttributeGen.ApplyList(ref _assemblyAttributes, AssemblyBuilder.SetCustomAttribute);
			AttributeGen.ApplyList(ref _moduleAttributes, ModuleBuilder.SetCustomAttribute);
            WriteAssemblyAttributes(_compilerOptions, AssemblyBuilder.GetName().Name, AssemblyBuilder);
		}
#endregion
	}
}
