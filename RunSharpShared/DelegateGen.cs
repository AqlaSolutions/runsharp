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
using System.Collections.Generic;
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
	public class DelegateGen : SignatureGen<DelegateGen>
	{
	    readonly AssemblyGen _owner;
	    readonly string _name;
	    readonly TypeAttributes _attrs;
		TypeGen _delegateType;
		List<AttributeGen> _customAttributes;
	    readonly TypeGen _owner2;

	    public ITypeMapper TypeMapper => _owner != null ? _owner.TypeMapper : _owner2.TypeMapper;

	    public DelegateGen(AssemblyGen owner, string name, Type returnType, TypeAttributes attrs)
			: base(returnType, owner.TypeMapper)
		{
			_owner = owner;
			_name = name;
			_attrs = attrs;
		}

        public DelegateGen(TypeGen typeGen, string name, Type returnType, TypeAttributes typeAttributes)
            : base(returnType, typeGen.TypeMapper)
        {
            _owner2 = typeGen;
            _name = name;
            _attrs = typeAttributes;

        }

        protected override ParameterBuilder DefineParameter(int position, ParameterAttributes attributes, string parameterName)
	    {
			return null;
		}

        TypeGen GetDelegateType()
		{
			if (_delegateType == null)
			{
				LockSignature();
				_delegateType = ImplementDelegate();
			}

			return _delegateType;
		}

#region Custom Attributes

		public override DelegateGen Attribute(AttributeType type)
		{
			BeginAttribute(type);
			return this;
		}

		public override DelegateGen Attribute(AttributeType type, params object[] args)
		{
			BeginAttribute(type, args);
			return this;
		}

		public override AttributeGen<DelegateGen> BeginAttribute(AttributeType type)
		{
			return BeginAttribute(type, EmptyArray<object>.Instance);
		}

		public override AttributeGen<DelegateGen> BeginAttribute(AttributeType type, params object[] args)
		{
			return AttributeGen<DelegateGen>.CreateAndAdd(this, ref _customAttributes, AttributeTargets.Delegate, type, args, TypeMapper);
		}

#endregion

		TypeGen ImplementDelegate()
		{
            TypeGen tg;
            if (_owner == null)
            {
                tg = new TypeGen(_owner2, _name, _attrs, _owner2.TypeMapper.MapType(typeof(MulticastDelegate)), Type.EmptyTypes, TypeMapper);
            }
            else
            {
                tg = new TypeGen(_owner, _name, _attrs, _owner.TypeMapper.MapType(typeof(MulticastDelegate)), Type.EmptyTypes, TypeMapper);
            }

			ConstructorBuilder cb = tg.Public.RuntimeImpl.Constructor()
				.Parameter(Helpers.TypeOf<object>(TypeMapper), "object")
				.Parameter(Helpers.TypeOf<IntPtr>(TypeMapper), "method")
				.GetConstructorBuilder();
			cb.SetImplementationFlags(MethodImplAttributes.Runtime | MethodImplAttributes.Managed);

			MethodBuilder mb = tg.Public.Virtual.RuntimeImpl.Method(Helpers.TypeOf<IAsyncResult>(TypeMapper), "BeginInvoke")
				.CopyParameters(Parameters)
				.UncheckedParameter(Helpers.TypeOf<AsyncCallback>(TypeMapper), "callback")
				.UncheckedParameter(Helpers.TypeOf<object>(TypeMapper), "object")
				.GetMethodBuilder();
			mb.SetImplementationFlags(MethodImplAttributes.Runtime | MethodImplAttributes.Managed);

			mb = tg.Public.Virtual.RuntimeImpl.Method(ReturnType, "EndInvoke")
				.Parameter(Helpers.TypeOf<IAsyncResult>(TypeMapper), "result")
				.GetMethodBuilder();
			mb.SetImplementationFlags(MethodImplAttributes.Runtime | MethodImplAttributes.Managed);

			mb = tg.Public.Virtual.RuntimeImpl.Method(ReturnType, "Invoke")
				.CopyParameters(Parameters)
				.GetMethodBuilder();
			mb.SetImplementationFlags(MethodImplAttributes.Runtime | MethodImplAttributes.Managed);

			AttributeGen.ApplyList(ref _customAttributes, tg.TypeBuilder.SetCustomAttribute);

			return tg;
		}

		public static implicit operator TypeGen(DelegateGen delegateGen)
		{
			return delegateGen.GetDelegateType();
		}
	}
}
