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
    public abstract class MemberGenBase<T>
    {
        internal MemberGenBase(ITypeMapper typeMapper)
        {
            TypeMapper = typeMapper;
        }

        public ITypeMapper TypeMapper { get; }
#if FEAT_IKVM

        public T Attribute(System.Type attributeType)
        {
            return Attribute(TypeMapper.MapType(attributeType));
        }

#endif

        public abstract T Attribute(AttributeType type);

#if FEAT_IKVM

        public T Attribute(System.Type attributeType, params object[] args)
        {
            return Attribute(TypeMapper.MapType(attributeType), args);
        }
#endif


        public abstract T Attribute(AttributeType type, params object[] args);

#if FEAT_IKVM

        public AttributeGen<T> BeginAttribute(System.Type attributeType)
        {
            return BeginAttribute(TypeMapper.MapType(attributeType));
        }
#endif


        public abstract AttributeGen<T> BeginAttribute(AttributeType type);

#if FEAT_IKVM

        public AttributeGen<T> BeginAttribute(System.Type attributeType, params object[] args)
        {
            return BeginAttribute(TypeMapper.MapType(attributeType), args);
        }

#endif

        public abstract AttributeGen<T> BeginAttribute(AttributeType type, params object[] args);

    }
}
