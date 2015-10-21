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
        Dictionary<Type, ITypeInfoProvider> providers = new Dictionary<Type, ITypeInfoProvider>();
        Dictionary<Type, WeakReference> cache = new Dictionary<Type, WeakReference>();

        public TypeInfo(ITypeMapper typeMapper)
        {
            TypeMapper = typeMapper;
        }

        public ITypeMapper TypeMapper { get; private set; }

        class CacheEntry
        {
            Type t;
            IMemberInfo[] constructors, fields, properties, events, methods;
            static string nullStr = "$NULL";
            string defaultMember = nullStr;

            TypeInfo _;

            public CacheEntry(Type t, TypeInfo owner)
            {
                this._ = owner;
                this.t = t;

                if (t.GetType() != typeof(object).GetType())
                {
                    // not a runtime type, TypeInfoProvider missing - return nothing
                    constructors = fields = properties = events = methods = empty;
                    defaultMember = null;
                }
            }

            ~CacheEntry()
            {
                lock (_.cache)
                {
                    WeakReference wr;
                    if (_.cache.TryGetValue(t, out wr) && (wr.Target == this || wr.Target == null))
                        _.cache.Remove(t);
                }
            }

            static IMemberInfo[] empty = { };

            public IMemberInfo[] Constructors
            {
                get
                {
                    if (constructors == null)
                    {
                        ConstructorInfo[] ctors = t.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly | BindingFlags.Instance);
                        constructors = Array.ConvertAll<ConstructorInfo, IMemberInfo>(ctors, delegate(ConstructorInfo ci) { return new StdMethodInfo(ci, _); });
                    }
                    return constructors;
                }
            }

            public IMemberInfo[] Fields
            {
                get
                {
                    if (fields == null)
                    {
                        FieldInfo[] fis = t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static);
                        fields = Array.ConvertAll<FieldInfo, IMemberInfo>(fis, delegate(FieldInfo fi) { return new StdFieldInfo(fi); });
                    }
                    return fields;
                }
            }

            public IMemberInfo[] Properties
            {
                get
                {
                    if (properties == null)
                    {
                        PropertyInfo[] pis = t.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static);
                        properties = Array.ConvertAll<PropertyInfo, IMemberInfo>(pis, delegate(PropertyInfo pi) { return new StdPropertyInfo(pi); });
                    }
                    return properties;
                }
            }

            public IMemberInfo[] Events
            {
                get
                {
                    if (events == null)
                    {
                        EventInfo[] eis = t.GetEvents(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static);
                        events = Array.ConvertAll<EventInfo, IMemberInfo>(eis, delegate(EventInfo ei) { return new StdEventInfo(ei); });
                    }
                    return events;
                }
            }

            public IMemberInfo[] Methods
            {
                get
                {
                    if (methods == null)
                    {
                        MethodInfo[] mis = t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static);
                        methods = Array.ConvertAll<MethodInfo, IMemberInfo>(mis, delegate(MethodInfo mi) { return new StdMethodInfo(mi, _); });
                    }
                    return methods;
                }
            }

            public string DefaultMember
            {
                get
                {
                    if (defaultMember == nullStr)
                    {
                        foreach (var dma in Helpers.GetCustomAttributes(t, typeof(DefaultMemberAttribute), true))
                        { 
#if FEAT_IKVM
                            return defaultMember = dma.ConstructorArguments[0].Value as string;
#else
                            return defaultMember = ((DefaultMemberAttribute)dma).MemberName;
#endif
                        }
                        defaultMember = null;
                    }
                    return defaultMember;
                }
            }
        }

        public void RegisterProvider(Type t, ITypeInfoProvider prov)
        {
            providers[t] = prov;
        }

        public void UnregisterProvider(Type t)
        {
            providers.Remove(t);
        }

        CacheEntry GetCacheEntry(Type t)
        {
            if (t is TypeBuilder)
                t = t.UnderlyingSystemType;

            lock (cache)
            {
                CacheEntry ce;
                WeakReference wr;

                if (cache.TryGetValue(t, out wr))
                {
                    ce = wr.Target as CacheEntry;
                    if (ce != null)
                        return ce;
                }

                ce = new CacheEntry(t, this);
                cache[t] = new WeakReference(ce);
                return ce;
            }
        }

        public IEnumerable<IMemberInfo> GetConstructors(Type t)
        {
            ITypeInfoProvider prov;

            if (providers.TryGetValue(t, out prov))
                return prov.GetConstructors();

            return GetCacheEntry(t).Constructors;
        }

        public IEnumerable<IMemberInfo> GetFields(Type t)
        {
            ITypeInfoProvider prov;

            if (providers.TryGetValue(t, out prov))
                return prov.GetFields();

            return GetCacheEntry(t).Fields;
        }

        public IEnumerable<IMemberInfo> GetProperties(Type t)
        {
            ITypeInfoProvider prov;

            if (providers.TryGetValue(t, out prov))
                return prov.GetProperties();

            return GetCacheEntry(t).Properties;
        }

        public IEnumerable<IMemberInfo> GetEvents(Type t)
        {
            ITypeInfoProvider prov;

            if (providers.TryGetValue(t, out prov))
                return prov.GetEvents();

            return GetCacheEntry(t).Events;
        }

        public IEnumerable<IMemberInfo> GetMethods(Type t)
        {
            ITypeInfoProvider prov;

            if (providers.TryGetValue(t, out prov))
                return prov.GetMethods();

            return GetCacheEntry(t).Methods;
        }

        public string GetDefaultMember(Type t)
        {
            ITypeInfoProvider prov;

            if (providers.TryGetValue(t, out prov))
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
            MethodBase mb;
            MethodInfo mi;
            string name;
            Type returnType;
            Type[] parameterTypes;
            bool hasVar;
            TypeInfo _;

            public StdMethodInfo(MethodInfo mi, TypeInfo owner)
                : this((MethodBase)mi, owner)
            {
                this.mi = mi;
            }

            public StdMethodInfo(ConstructorInfo ci, TypeInfo owner)
                : this((MethodBase)ci, owner)
            {
                this.returnType =  owner.TypeMapper.MapType(typeof(void));
            }

            public StdMethodInfo(MethodBase mb, TypeInfo owner)
            {
                this.mb = mb;
                _ = owner;
            }

            void RequireParameters()
            {
                if (parameterTypes == null)
                {
                    ParameterInfo[] pis = mb.GetParameters();
                    parameterTypes = ArrayUtils.GetTypes(pis);

                    hasVar = pis.Length > 0 &&
                        Helpers.GetCustomAttributes(pis[pis.Length - 1], typeof(ParamArrayAttribute), false).Count> 0;
                }
            }

            public MemberInfo Member { get { return mb; } }
            public string Name
            {
                get
                {
                    if (name == null)
                        name = mb.Name;
                    return name;
                }
            }
            public Type ReturnType
            {
                get
                {
                    if (returnType == null)
                        returnType = mi.ReturnType;
                    return returnType;
                }
            }
            public Type[] ParameterTypes
            {
                get
                {
                    RequireParameters();
                    return parameterTypes;
                }
            }
            public bool IsParameterArray
            {
                get
                {
                    RequireParameters();
                    return hasVar;
                }
            }
            public bool IsStatic { get { return mb.IsStatic; } }
            public bool IsOverride { get { return Utils.IsOverride(mb.Attributes); } }

            public override string ToString()
            {
                return mb.ToString();
            }
        }

        class StdPropertyInfo : IMemberInfo
        {
            PropertyInfo pi;
            string name;
            MethodInfo mi;
            Type returnType;
            Type[] parameterTypes;
            bool hasVar;

            public StdPropertyInfo(PropertyInfo pi)
            {
                this.pi = pi;
                this.mi = pi.GetGetMethod();
                if (mi == null)
                    mi = pi.GetSetMethod();
                // mi will remain null for abstract properties
            }

            void RequireParameters()
            {
                if (parameterTypes == null)
                {
                    ParameterInfo[] pis = pi.GetIndexParameters();
                    parameterTypes = ArrayUtils.GetTypes(pis);

                    hasVar = pis.Length > 0 &&
                        Helpers.GetCustomAttributes(pis[pis.Length - 1], typeof(ParamArrayAttribute), false).Count > 0;
                }
            }

            public MemberInfo Member { get { return pi; } }
            public string Name
            {
                get
                {
                    if (name == null)
                        name = pi.Name;
                    return name;
                }
            }
            public Type ReturnType
            {
                get
                {
                    if (returnType == null)
                        returnType = pi.PropertyType;
                    return returnType;
                }
            }
            public Type[] ParameterTypes
            {
                get
                {
                    RequireParameters();
                    return parameterTypes;
                }
            }
            public bool IsParameterArray
            {
                get
                {
                    RequireParameters();
                    return hasVar;
                }
            }
            public bool IsOverride { get { return mi == null ? false : Utils.IsOverride(mi.Attributes); } }
            public bool IsStatic { get { return mi == null ? false : mi.IsStatic; } }

            public override string ToString()
            {
                return pi.ToString();
            }
        }

        class StdEventInfo : IMemberInfo
        {
            EventInfo ei;
            string name;
            MethodInfo mi;

            public StdEventInfo(EventInfo ei)
            {
                this.ei = ei;
                this.name = ei.Name;

                this.mi = ei.GetAddMethod();
                if (mi == null)
                    mi = ei.GetRemoveMethod();
                // mi will remain null for abstract properties
            }

            public MemberInfo Member { get { return ei; } }
            public string Name { get { return name; } }
            public Type ReturnType { get { return ei.EventHandlerType; } }
            public Type[] ParameterTypes { get { return Type.EmptyTypes; } }
            public bool IsParameterArray { get { return false; } }
            public bool IsOverride { get { return mi == null ? false : Utils.IsOverride(mi.Attributes); } }
            public bool IsStatic { get { return mi == null ? false : mi.IsStatic; } }

            public override string ToString()
            {
                return ei.ToString();
            }
        }

        class StdFieldInfo : IMemberInfo
        {
            FieldInfo fi;
            string name;

            public StdFieldInfo(FieldInfo fi)
            {
                this.fi = fi;
                this.name = fi.Name;
            }

            public MemberInfo Member { get { return fi; } }
            public string Name { get { return name; } }
            public Type ReturnType { get { return fi.FieldType; } }
            public Type[] ParameterTypes { get { return Type.EmptyTypes; } }
            public bool IsParameterArray { get { return false; } }
            public bool IsOverride { get { return false; } }
            public bool IsStatic { get { return fi.IsStatic; } }

            public override string ToString()
            {
                return fi.ToString();
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
