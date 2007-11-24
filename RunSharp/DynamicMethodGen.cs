/*
 * Copyright (c) 2009, Stefan Simek
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
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;

namespace TriAxis.RunSharp
{
	public sealed class DynamicMethodGen : ICodeGenContext
	{
		Attributes attrs;
		Type returnType;
		Type[] parameterTypes;
		DynamicMethod dm;
		CodeGen code;

		public static Attributes Static(Type owner)
		{
			return new Attributes(owner, false);
		}

		public static Attributes Static(Module owner)
		{
			return new Attributes(owner);
		}

		public static Attributes Instance(Type owner)
		{
			return new Attributes(owner, false);
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible", Justification = "The type has to be public, but would be useless outside of this class")]
		public sealed class Attributes
		{
			internal string name = "";
			internal bool skipVisibility;
			internal Type ownerType;
			internal Module ownerModule;
			internal bool asInstance;

			internal Attributes(Type owner, bool asInstance)
			{
				this.ownerType = owner;
				this.asInstance = asInstance;
			}

			internal Attributes(Module owner)
			{
				this.ownerModule = owner;
			}

			[DebuggerBrowsable(DebuggerBrowsableState.Never)]
			public Attributes NoVisibilityChecks { get { skipVisibility = true; return this; } }

			public Attributes WithName(string name)
			{
				this.name = name;
				return this;
			}

			public DynamicMethodGen Method(Type returnType, params Type[] parameterTypes)
			{
				return new DynamicMethodGen(this, returnType, parameterTypes);
			}
		}
		
		private DynamicMethodGen(Attributes attrs, Type returnType, Type[] parameterTypes)
		{
			this.attrs = attrs;
			this.returnType = returnType;
			this.parameterTypes = parameterTypes;

			if (attrs.asInstance)
				parameterTypes = ArrayUtils.Combine(attrs.ownerType, parameterTypes);

			if (attrs.ownerType != null)
				this.dm = new DynamicMethod(attrs.name, returnType, parameterTypes, attrs.ownerType, attrs.skipVisibility);
			else
				this.dm = new DynamicMethod(attrs.name, returnType, parameterTypes, attrs.ownerModule, attrs.skipVisibility);

			if (attrs.asInstance)
				this.dm.DefineParameter(1, ParameterAttributes.None, "this");

			this.code = new CodeGen(this);
		}

		public CodeGen Code { get { return code; } }

		public bool IsCompleted { get { return code.IsCompleted; } }

		public void Complete() { code.Complete(); }

		public DynamicMethod GetCompletedDynamicMethod()
		{
			return GetCompletedDynamicMethod(false);
		}

		public DynamicMethod GetCompletedDynamicMethod(bool completeIfNeeded)
		{
			if (completeIfNeeded)
				code.Complete();
			else if (!code.IsCompleted)
				throw new InvalidOperationException(Properties.Messages.ErrDynamicMethodNotCompleted);

			return dm;
		}

		#region ICodeGenContext Members

		ILGenerator ICodeGenContext.GetILGenerator()
		{
			return dm.GetILGenerator();
		}

		Type ICodeGenContext.OwnerType
		{
			get { return attrs.ownerType; }
		}

		void ICodeGenContext.DefineParameterName(int index, string name)
		{
			if (attrs.asInstance)
				index++;

			dm.DefineParameter(index + 1, ParameterAttributes.None, name);
		}

		#endregion

		#region IMemberInfo Members

		MemberInfo IMemberInfo.Member
		{
			get { return dm; }
		}

		string IMemberInfo.Name
		{
			get { return attrs.name; }
		}

		Type IMemberInfo.ReturnType
		{
			get { return returnType; }
		}

		Type[] IMemberInfo.ParameterTypes
		{
			get { return parameterTypes; }
		}

		bool IMemberInfo.IsParameterArray
		{
			get { return false; }
		}

		bool IMemberInfo.IsStatic
		{
			get { return !attrs.asInstance; }
		}

		bool IMemberInfo.IsOverride
		{
			get { return false; }
		}

		#endregion
	}
}
