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
	public sealed class MethodGen : RoutineGen<MethodGen>
	{
	    readonly string _name;
	    readonly MethodAttributes _attributes;
		MethodBuilder _mb;
	    readonly MethodImplAttributes _implFlags;

	    internal MethodBuilder GetMethodBuilder()
		{
			LockSignature(); 
			return _mb;
		}

		internal MethodGen(TypeGen owner, string name, MethodAttributes attributes, Type returnType, MethodImplAttributes implFlags)
			: base(owner, returnType, owner)
		{
			_name = name;
			_attributes = owner.PreprocessAttributes(this, attributes);
			_implFlags = implFlags;
		}

		protected override void CreateMember()
		{
			string methodName = _name;

			if (ImplementedInterface != null)
				methodName = ImplementedInterface + "." + _name;

			_mb = Owner.TypeBuilder.DefineMethod(methodName, _attributes | MethodAttributes.HideBySig, IsStatic ? CallingConventions.Standard : CallingConventions.HasThis, ReturnType, ParameterTypes);
            if (_implFlags != 0)
				_mb.SetImplementationFlags(_implFlags);
        }

		protected override void RegisterMember()
		{
			Owner.Register(this);
		}

		public bool IsPublic => (_attributes & MethodAttributes.Public) != 0;

	    public bool IsAbstract => (_attributes & MethodAttributes.Abstract) != 0;

	    internal Type ImplementedInterface { get; set; }

	    #region RoutineGen concrete implementation

		protected internal override bool IsStatic => (_attributes & MethodAttributes.Static) != 0;

	    protected internal override bool IsOverride => Utils.IsOverride(_attributes);

	    public override string Name => _name;

	    protected override bool HasCode => !IsAbstract && (_implFlags & MethodImplAttributes.Runtime) == 0;

	    protected override ILGenerator GetILGenerator()
		{
			return _mb.GetILGenerator();
		}

		protected override ParameterBuilder DefineParameter(int position, ParameterAttributes attributes, string parameterName)
		{
			return _mb.DefineParameter(position, attributes, parameterName);
		}

		protected override MemberInfo Member => _mb;

	    protected override AttributeTargets AttributeTarget => AttributeTargets.Constructor;

	    protected override void SetCustomAttribute(CustomAttributeBuilder cab)
		{
			_mb.SetCustomAttribute(cab);
		}

		#endregion
		
		public override string ToString()
		{
			return OwnerType.ToString() + "." + _name;
		}
	}
}
