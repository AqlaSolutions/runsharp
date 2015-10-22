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
using TryAxis.RunSharp;
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

        internal ExpressionFactory(ITypeMapper typeMapper)
        {
            if (typeMapper == null) throw new ArgumentNullException(nameof(typeMapper));
            _typeMapper = typeMapper;
        }

        #region Construction expressions

        public ContextualOperand New(Type type)
        {
            return new ContextualOperand(New(type, Operand.EmptyArray), _typeMapper);
        }

        public ContextualOperand New(Type type, params Operand[] args)
        {
            ApplicableFunction ctor = OverloadResolver.Resolve(_typeMapper.TypeInfo.GetConstructors(type), _typeMapper, args);

            if (ctor == null)
                throw new MissingMethodException(Properties.Messages.ErrMissingConstructor);

            return new ContextualOperand(new NewObject(ctor, args), _typeMapper);
        }

        public ContextualOperand NewArray(Type type, params Operand[] indexes)
        {
            return new ContextualOperand(new NewArray(type, indexes), _typeMapper);
        }

        public ContextualOperand NewInitializedArray(Type type, params Operand[] elements)
        {
            return new ContextualOperand(new InitializedArray(type, elements), _typeMapper);
        }

        public ContextualOperand NewDelegate(Type delegateType, Type target, string method)
        {
            return new ContextualOperand(new NewDelegate(delegateType, target, method, _typeMapper), _typeMapper);
        }

        public ContextualOperand NewDelegate(Type delegateType, Operand target, string method)
        {
            return new ContextualOperand(new NewDelegate(delegateType, target, method, _typeMapper), _typeMapper);
        }

        #endregion
    }
}