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
using System.Text;
using TriAxis.RunSharp;
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

    public class ExpressionFactory
    {
        readonly ITypeMapper _typeMapper;

        public ExpressionFactory(ITypeMapper typeMapper)
        {
            if (typeMapper == null) throw new ArgumentNullException(nameof(typeMapper));
            _typeMapper = typeMapper;
        }

        #region Construction expressions

#if FEAT_IKVM

        public ContextualOperand New(System.Type type)
        {
            return New(_typeMapper.MapType(type));
        }
#endif


        public ContextualOperand New<T>()
        {
            return New(_typeMapper.MapType(typeof(T)));
        }

        public ContextualOperand New(Type type)
        {
            return New(type, Operand.EmptyArray);
        }

#if FEAT_IKVM

        public ContextualOperand New(System.Type type, params Operand[] args)
        {
            return New(_typeMapper.MapType(type), args);
        }
#endif


        public ContextualOperand New(Type type, params Operand[] args)
        {
            ApplicableFunction ctor = OverloadResolver.Resolve(_typeMapper.TypeInfo.GetConstructors(type), _typeMapper, args);

            if (ctor == null)
                throw new MissingMethodException(Properties.Messages.ErrMissingConstructor);

            return OperandExtensions.SetLeakedState(new ContextualOperand(new NewObject(ctor, args), _typeMapper), true);
        }

#if FEAT_IKVM

        public ContextualOperand NewArray(System.Type type, params Operand[] indexes)
        {
            return NewArray(_typeMapper.MapType(type), indexes);
        }
        
#endif

        public ContextualOperand NewArray(Type type, params Operand[] indexes)
        {
            return OperandExtensions.SetLeakedState(new ContextualOperand(new NewArray(type, indexes), _typeMapper), true);
        }

#if FEAT_IKVM

        public ContextualOperand NewInitializedArray(System.Type type, params Operand[] elements)
        {
            return NewInitializedArray(_typeMapper.MapType(type), elements);
        }
#endif


        public ContextualOperand NewInitializedArray(Type type, params Operand[] elements)
        {
            return OperandExtensions.SetLeakedState(new ContextualOperand(new InitializedArray(type, elements), _typeMapper), true);
        }

#if FEAT_IKVM

        public ContextualOperand NewDelegate(System.Type type, Type target, string method)
        {
            return NewDelegate(_typeMapper.MapType(type), target, method);
        }
#endif


        public ContextualOperand NewDelegate(Type delegateType, Type target, string method)
        {
            return OperandExtensions.SetLeakedState(new ContextualOperand(new NewDelegate(delegateType, target, method, _typeMapper), _typeMapper), true);
        }

#if FEAT_IKVM

        public ContextualOperand NewDelegate(System.Type type, Operand target, string method)
        {
            return NewDelegate(_typeMapper.MapType(type), target, method);
        }
#endif


        public ContextualOperand NewDelegate(Type delegateType, Operand target, string method)
        {
            return OperandExtensions.SetLeakedState(new ContextualOperand(new NewDelegate(delegateType, target, method, _typeMapper), _typeMapper), true);
        }

        #endregion
    }
}