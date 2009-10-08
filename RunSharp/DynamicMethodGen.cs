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
	public sealed class DynamicMethodGen : RoutineGen<DynamicMethodGen>
	{
		Attributes attrs;
		DynamicMethod dm;
		
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

			public DynamicMethodGen Method(Type returnType)
			{
				return new DynamicMethodGen(this, returnType);
			}
		}

		private DynamicMethodGen(Attributes attrs, Type returnType)
			: base(attrs.ownerType, returnType)
		{
			this.attrs = attrs;

			if (attrs.asInstance)
				Parameter(attrs.ownerType, "this");
		}

		protected override void CreateMember()
		{
			if (attrs.ownerType != null)
				this.dm = new DynamicMethod(attrs.name, ReturnType, ParameterTypes, attrs.ownerType, attrs.skipVisibility);
			else
				this.dm = new DynamicMethod(attrs.name, ReturnType, ParameterTypes, attrs.ownerModule, attrs.skipVisibility);
		}

		protected override void RegisterMember()
		{
			// nothing to register
		}

		public bool IsCompleted { get { return !(SignatureComplete && GetCode().IsCompleted); } }

		public void Complete() { GetCode().Complete(); }

		public DynamicMethod GetCompletedDynamicMethod()
		{
			return GetCompletedDynamicMethod(false);
		}

		public DynamicMethod GetCompletedDynamicMethod(bool completeIfNeeded)
		{
			if (completeIfNeeded)
				Complete();
			else if (!IsCompleted)
				throw new InvalidOperationException(Properties.Messages.ErrDynamicMethodNotCompleted);

			return dm;
		}

		#region RoutineGen concrete implementation

		protected override bool HasCode
		{
			get { return true; }
		}

		protected override ILGenerator GetILGenerator()
		{
			return dm.GetILGenerator();
		}

		protected override ParameterBuilder DefineParameter(int position, ParameterAttributes attributes, string parameterName)
		{
			return dm.DefineParameter(position, attributes, parameterName);
		}

		protected override MemberInfo Member
		{
			get { return dm; }
		}

		public override string Name
		{
			get { return attrs.name; }
		}

		protected internal override bool IsStatic
		{
			get { return !attrs.asInstance; }
		}

		protected internal override bool IsOverride
		{
			get { return false; }
		}

		#endregion
	}
}
