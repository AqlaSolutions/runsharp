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

    interface IMemberInfo
    {
        MemberInfo Member { get; }
        string Name { get; }
        Type ReturnType { get; }
        Type[] ParameterTypes { get; }
        bool IsParameterArray { get; }
        bool IsStatic { get; }
        bool IsOverride { get; }
    }

    interface ITypeInfoProvider
    {
        IEnumerable<IMemberInfo> GetConstructors();
        IEnumerable<IMemberInfo> GetFields();
        IEnumerable<IMemberInfo> GetProperties();
        IEnumerable<IMemberInfo> GetEvents();
        IEnumerable<IMemberInfo> GetMethods();
        string DefaultMember { get; }
    }

    class TypeInfo : ITypeInfo
    {
        readonly Dictionary<Type, ITypeInfoProvider> _providers = new Dictionary<Type, ITypeInfoProvider>();
        readonly Dictionary<Type, WeakReference> _cache = new Dictionary<Type, WeakReference>();

        public TypeInfo(ITypeMapper typeMapper)
        {
            TypeMapper = typeMapper;
        }

        public ITypeMapper TypeMapper { get; private set; }

        class CacheEntry
        {
            readonly Type _t;
            IMemberInfo[] _constructors, _fields, _properties, _events, _methods;
            static readonly string _nullStr = "$NULL";
            string _defaultMember = _nullStr;

            readonly TypeInfo _;

            public CacheEntry(Type t, TypeInfo owner)
            {
                this._ = owner;
                this._t = t;

                if (t.GetType() != typeof(object).GetType())
                {
                    // not a runtime type, TypeInfoProvider missing - return nothing
                    _constructors = _fields = _properties = _events = _methods = _empty;
                    _defaultMember = null;
                }
            }

            ~CacheEntry()
            {
                lock (_._cache)
                {
                    WeakReference wr;
                    if (_._cache.TryGetValue(_t, out wr) && (wr.Target == this || wr.Target == null))
                        _._cache.Remove(_t);
                }
            }

            static readonly IMemberInfo[] _empty = { };

            public IMemberInfo[] Constructors
            {
                get
                {
                    if (_constructors == null)
                    {
                        ConstructorInfo[] ctors = _t.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly | BindingFlags.Instance);
                        _constructors = Array.ConvertAll<ConstructorInfo, IMemberInfo>(ctors, delegate(ConstructorInfo ci) { return new StdMethodInfo(ci, _); });
                    }
                    return _constructors;
                }
            }

            public IMemberInfo[] Fields
            {
                get
                {
                    if (_fields == null)
                    {
                        FieldInfo[] fis = _t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static);
                        _fields = Array.ConvertAll<FieldInfo, IMemberInfo>(fis, delegate(FieldInfo fi) { return new StdFieldInfo(fi); });
                    }
                    return _fields;
                }
            }

            public IMemberInfo[] Properties
            {
                get
                {
                    if (_properties == null)
                    {
                        PropertyInfo[] pis = _t.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static);
                        _properties = Array.ConvertAll<PropertyInfo, IMemberInfo>(pis, delegate(PropertyInfo pi) { return new StdPropertyInfo(pi); });
                    }
                    return _properties;
                }
            }

            public IMemberInfo[] Events
            {
                get
                {
                    if (_events == null)
                    {
                        EventInfo[] eis = _t.GetEvents(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static);
                        _events = Array.ConvertAll<EventInfo, IMemberInfo>(eis, delegate(EventInfo ei) { return new StdEventInfo(ei); });
                    }
                    return _events;
                }
            }

            public IMemberInfo[] Methods
            {
                get
                {
                    if (_methods == null)
                    {
                        MethodInfo[] mis = _t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static);
                        _methods = Array.ConvertAll<MethodInfo, IMemberInfo>(mis, delegate(MethodInfo mi) { return new StdMethodInfo(mi, _); });
                    }
                    return _methods;
                }
            }

            public string DefaultMember
            {
                get
                {
                    if (_defaultMember == _nullStr)
                    {
                        foreach (var dma in Helpers.GetCustomAttributes(_t, typeof(DefaultMemberAttribute), true))
                        { 
#if FEAT_IKVM
                            return _defaultMember = dma.ConstructorArguments[0].Value as string;
#else
                            return _defaultMember = ((DefaultMemberAttribute)dma).MemberName;
#endif
                        }
                        _defaultMember = null;
                    }
                    return _defaultMember;
                }
            }
        }

        public void RegisterProvider(Type t, ITypeInfoProvider prov)
        {
            _providers[t] = prov;
        }

        public void UnregisterProvider(Type t)
        {
            _providers.Remove(t);
        }

        CacheEntry GetCacheEntry(Type t)
        {
            if (t is TypeBuilder)
                t = t.UnderlyingSystemType;

            lock (_cache)
            {
                CacheEntry ce;
                WeakReference wr;

                if (_cache.TryGetValue(t, out wr))
                {
                    ce = wr.Target as CacheEntry;
                    if (ce != null)
                        return ce;
                }

                ce = new CacheEntry(t, this);
                _cache[t] = new WeakReference(ce);
                return ce;
            }
        }

        public IEnumerable<IMemberInfo> GetConstructors(Type t)
        {
            ITypeInfoProvider prov;

            if (_providers.TryGetValue(t, out prov))
                return prov.GetConstructors();

            return GetCacheEntry(t).Constructors;
        }

        public IEnumerable<IMemberInfo> GetFields(Type t)
        {
            ITypeInfoProvider prov;

            if (_providers.TryGetValue(t, out prov))
                return prov.GetFields();

            return GetCacheEntry(t).Fields;
        }

        public IEnumerable<IMemberInfo> GetProperties(Type t)
        {
            ITypeInfoProvider prov;

            if (_providers.TryGetValue(t, out prov))
                return prov.GetProperties();

            return GetCacheEntry(t).Properties;
        }

        public IEnumerable<IMemberInfo> GetEvents(Type t)
        {
            ITypeInfoProvider prov;

            if (_providers.TryGetValue(t, out prov))
                return prov.GetEvents();

            return GetCacheEntry(t).Events;
        }

        public IEnumerable<IMemberInfo> GetMethods(Type t)
        {
            ITypeInfoProvider prov;

            if (_providers.TryGetValue(t, out prov))
                return prov.GetMethods();

            return GetCacheEntry(t).Methods;
        }

        public string GetDefaultMember(Type t)
        {
            ITypeInfoProvider prov;

            if (_providers.TryGetValue(t, out prov))
                return prov.DefaultMember;

            return GetCacheEntry(t).DefaultMember;
        }

        public IEnumerable<IMemberInfo> Filter(IEnumerable<IMemberInfo> source, string name, bool ignoreCase, bool isStatic, bool allowOverrides)
        {
            foreach (IMemberInfo mi in source)
            {
                if (mi.IsStatic != isStatic)
                    continue;

                if (!allowOverrides && mi.IsOverride)
                    continue;

                if (name != null)
                {
                    if (ignoreCase)
                    {
                        if (!mi.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                            continue;
                    }
                    else
                    {
                        if (mi.Name != name)
                            continue;
                    }
                }

                yield return mi;
            }
        }

        public ApplicableFunction FindConstructor(Type t, Operand[] args)
        {
            ApplicableFunction ctor = OverloadResolver.Resolve(GetConstructors(t), args);

            if (ctor == null)
                throw new MissingMemberException(Properties.Messages.ErrMissingConstructor);

            return ctor;
        }

        public IMemberInfo FindField(Type t, string name, bool @static)
        {
            foreach (Type type in SearchableTypes(t))
            {
                foreach (IMemberInfo mi in GetFields(type))
                {
                    if (mi.Name == name && mi.IsStatic == @static)
                        return mi;
                }
            }

            //for (; t != null; t = t.BaseType)
            //{
            //    foreach (IMemberInfo mi in GetFields(t))
            //    {
            //        if (mi.Name == name && mi.IsStatic == @static)
            //            return mi;
            //    }
            //}

            throw new MissingFieldException(Properties.Messages.ErrMissingField);
        }

        public ApplicableFunction FindProperty(Type t, string name, Operand[] indexes, bool @static)
        {
            if (name == null)
                name = GetDefaultMember(t);

            foreach (Type type in SearchableTypes(t))
            {
                ApplicableFunction af = OverloadResolver.Resolve(Filter(GetProperties(type), name, false, @static, false), indexes);

                if (af != null)
                    return af;
            }
            //for (; t != null; t = t.BaseType)
            //{

            //}

            throw new MissingMemberException(Properties.Messages.ErrMissingProperty);
        }

        public IMemberInfo FindEvent(Type t, string name, bool @static)
        {
            foreach (Type type in SearchableTypes(t))
            {
                foreach (IMemberInfo mi in GetEvents(type))
                {
                    if (mi.Name == name && mi.IsStatic == @static)
                        return mi;
                }
            }

            throw new MissingMemberException(Properties.Messages.ErrMissingEvent);
        }

        private IEnumerable<Type> SearchableTypes(Type t)
        {
            if (t.IsInterface)
            {
                foreach (Type @interface in SearchInterfaces(t))
                {
                    yield return @interface;
                }
            }
            else
            {
                foreach (Type baseType in SearchBaseTypes(t))
                {
                    yield return baseType;
                }
            }
        }

        private IEnumerable<Type> SearchBaseTypes(Type t)
        {
            yield return t;
            t = t.BaseType;
            if (t != null)
            {
                foreach (Type baseType in SearchBaseTypes(t))
                {
                    yield return baseType;
                }
            }
        }

        public IEnumerable<Type> SearchInterfaces(Type t)
        {
            yield return t;
            foreach (Type @interface in t.GetInterfaces())
            {
                foreach (Type baseInterface in SearchInterfaces(@interface))
                {
                    yield return baseInterface;
                }
            }
        }

        public ApplicableFunction FindMethod(Type t, string name, Operand[] args, bool @static)
        {
            foreach (Type type in SearchableTypes(t))
            {
                ApplicableFunction af = OverloadResolver.Resolve(Filter(GetMethods(type), name, false, @static, false), args);

                if (af != null)
                    return af;
            }

            throw new MissingMethodException(Properties.Messages.ErrMissingMethod);
        }

        class StdMethodInfo : IMemberInfo
        {
            readonly MethodBase _mb;
            readonly MethodInfo _mi;
            string _name;
            Type _returnType;
            Type[] _parameterTypes;
            bool _hasVar;
            TypeInfo _;

            public StdMethodInfo(MethodInfo mi, TypeInfo owner)
                : this((MethodBase)mi, owner)
            {
                this._mi = mi;
            }

            public StdMethodInfo(ConstructorInfo ci, TypeInfo owner)
                : this((MethodBase)ci, owner)
            {
                this._returnType =  owner.TypeMapper.MapType(typeof(void));
            }

            public StdMethodInfo(MethodBase mb, TypeInfo owner)
            {
                this._mb = mb;
                _ = owner;
            }

            void RequireParameters()
            {
                if (_parameterTypes == null)
                {
                    ParameterInfo[] pis = _mb.GetParameters();
                    _parameterTypes = ArrayUtils.GetTypes(pis);

                    _hasVar = pis.Length > 0 &&
                        Helpers.GetCustomAttributes(pis[pis.Length - 1], typeof(ParamArrayAttribute), false).Count> 0;
                }
            }

            public MemberInfo Member => _mb;

            public string Name
            {
                get
                {
                    if (_name == null)
                        _name = _mb.Name;
                    return _name;
                }
            }
            public Type ReturnType
            {
                get
                {
                    if (_returnType == null)
                        _returnType = _mi.ReturnType;
                    return _returnType;
                }
            }
            public Type[] ParameterTypes
            {
                get
                {
                    RequireParameters();
                    return _parameterTypes;
                }
            }
            public bool IsParameterArray
            {
                get
                {
                    RequireParameters();
                    return _hasVar;
                }
            }
            public bool IsStatic => _mb.IsStatic;
            public bool IsOverride => Utils.IsOverride(_mb.Attributes);

            public override string ToString()
            {
                return _mb.ToString();
            }
        }

        class StdPropertyInfo : IMemberInfo
        {
            readonly PropertyInfo _pi;
            string _name;
            readonly MethodInfo _mi;
            Type _returnType;
            Type[] _parameterTypes;
            bool _hasVar;

            public StdPropertyInfo(PropertyInfo pi)
            {
                this._pi = pi;
                this._mi = pi.GetGetMethod();
                if (_mi == null)
                    _mi = pi.GetSetMethod();
                // mi will remain null for abstract properties
            }

            void RequireParameters()
            {
                if (_parameterTypes == null)
                {
                    ParameterInfo[] pis = _pi.GetIndexParameters();
                    _parameterTypes = ArrayUtils.GetTypes(pis);

                    _hasVar = pis.Length > 0 &&
                        Helpers.GetCustomAttributes(pis[pis.Length - 1], typeof(ParamArrayAttribute), false).Count > 0;
                }
            }

            public MemberInfo Member => _pi;

            public string Name
            {
                get
                {
                    if (_name == null)
                        _name = _pi.Name;
                    return _name;
                }
            }
            public Type ReturnType
            {
                get
                {
                    if (_returnType == null)
                        _returnType = _pi.PropertyType;
                    return _returnType;
                }
            }
            public Type[] ParameterTypes
            {
                get
                {
                    RequireParameters();
                    return _parameterTypes;
                }
            }
            public bool IsParameterArray
            {
                get
                {
                    RequireParameters();
                    return _hasVar;
                }
            }
            public bool IsOverride => _mi == null ? false : Utils.IsOverride(_mi.Attributes);
            public bool IsStatic => _mi == null ? false : _mi.IsStatic;

            public override string ToString()
            {
                return _pi.ToString();
            }
        }

        class StdEventInfo : IMemberInfo
        {
            readonly EventInfo _ei;
            readonly MethodInfo _mi;

            public StdEventInfo(EventInfo ei)
            {
                this._ei = ei;
                this.Name = ei.Name;

                this._mi = ei.GetAddMethod();
                if (_mi == null)
                    _mi = ei.GetRemoveMethod();
                // mi will remain null for abstract properties
            }

            public MemberInfo Member => _ei;
            public string Name { get; }
            public Type ReturnType => _ei.EventHandlerType;
            public Type[] ParameterTypes => Type.EmptyTypes;
            public bool IsParameterArray => false;
            public bool IsOverride => _mi == null ? false : Utils.IsOverride(_mi.Attributes);
            public bool IsStatic => _mi == null ? false : _mi.IsStatic;

            public override string ToString()
            {
                return _ei.ToString();
            }
        }

        class StdFieldInfo : IMemberInfo
        {
            readonly FieldInfo _fi;

            public StdFieldInfo(FieldInfo fi)
            {
                this._fi = fi;
                this.Name = fi.Name;
            }

            public MemberInfo Member => _fi;
            public string Name { get; }
            public Type ReturnType => _fi.FieldType;
            public Type[] ParameterTypes => Type.EmptyTypes;
            public bool IsParameterArray => false;
            public bool IsOverride => false;
            public bool IsStatic => _fi.IsStatic;

            public override string ToString()
            {
                return _fi.ToString();
            }
        }
    }

    /*		public Operand Invoke(string name, params Operand[] args)
            {
                Operand target = Target;

                for (Type src = this; src != null; src = src.Base)
                {
                    ApplicableFunction match = OverloadResolver.Resolve(Type.FilterMethods(src.GetMethods(), name, false, (object)target == null, false), args);

                    if (match != null)
                        return new Invocation(match, Target, args);
                }

                throw new MissingMethodException(Properties.Messages.ErrMissingMethod);
            }*/
}
