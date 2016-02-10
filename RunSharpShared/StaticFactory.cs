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
    
	public class StaticFactory
	{
	    readonly ITypeMapper _typeMapper;

	    public StaticFactory(ITypeMapper typeMapper)
	    {
	        if (typeMapper == null) throw new ArgumentNullException(nameof(typeMapper));
	        _typeMapper = typeMapper;
	    }

#if FEAT_IKVM
        public ContextualOperand Field(System.Type type, string name)
	    {
	        return Field(_typeMapper.MapType(type), name);
	    }
#endif

	    public ContextualOperand Field<T>(string name)
	    {
	        return Field(_typeMapper.MapType(typeof(T)), name);
	    }

	    public ContextualOperand Field(Type type, string name)
		{
			return new ContextualOperand(new Field((FieldInfo)_typeMapper.TypeInfo.FindField(type, name, true).Member, null), _typeMapper).SetLeakedState(true);
		}

#if FEAT_IKVM
        public ContextualOperand Property(System.Type type, string name)
	    {
	        return Property(_typeMapper.MapType(type), name);
	    }
#endif
        public ContextualOperand Property(Type type, string name)
		{
			return Property(type, name, Operand.EmptyArray);
		}

#if FEAT_IKVM

        public ContextualOperand Property(System.Type type, string name, params Operand[] indexes)
	    {
	        return Property(_typeMapper.MapType(type), name, indexes);
	    }
#endif


	    public ContextualOperand Property(Type type, string name, params Operand[] indexes)
		{
			return new ContextualOperand(new Property(_typeMapper.TypeInfo.FindProperty(type, name, indexes, true), null, indexes), _typeMapper).SetLeakedState(true);
		}
        
	    public ContextualOperand Property<T>(string name, params Operand[] indexes)
	    {
	        return Property(typeof(T), name, indexes);
	    }
        
#if FEAT_IKVM
        public ContextualOperand Invoke(System.Type type, string name)
	    {
	        return Invoke(_typeMapper.MapType(type), name);
	    }
        
#endif

	    public ContextualOperand Invoke(Type type, string name)
		{
			return Invoke(type, name, Operand.EmptyArray);
		}

#if FEAT_IKVM
        public ContextualOperand Invoke(System.Type type, string name, params Operand[] args)
	    {
	        return Invoke(_typeMapper.MapType(type), name, args);
	    }
#endif

	    public ContextualOperand Invoke<T>(string name, params Operand[] args)
	    {
	        return Invoke(_typeMapper.MapType(typeof(T)), name, args);
	    }

	    public ContextualOperand Invoke(Type type, string name, params Operand[] args)
		{
			return new ContextualOperand(new Invocation(_typeMapper.TypeInfo.FindMethod(type, name, args, true), null, args), _typeMapper).SetLeakedState(true);
		}

        public ContextualOperand Invoke(MethodInfo method, params Operand[] args)
		{
            return new ContextualOperand(new Invocation(_typeMapper.TypeInfo.FindMethod(method), null, args), _typeMapper).SetLeakedState(true);
		}

	    public ContextualOperand InvokeReferenceEquals(Operand a, Operand b)
	    {
	        return Invoke(typeof(object), "ReferenceEquals", a, b);
	    }

	    public ContextualOperand InvokeEquals(Operand a, Operand b)
	    {
	        return Invoke(typeof(object), "Equals", a, b);
	    }
	}
}
