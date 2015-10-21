#if !FEAT_IKVM
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
using System.Diagnostics;
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
    public sealed class DynamicMethodGen : RoutineGen<DynamicMethodGen>, ICodeGenContext
    {
        readonly Attributes _attrs;
        DynamicMethod _dm;

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
            internal bool SkipVisibility;
            internal Type ownerType;
            internal Module OwnerModule;
            internal bool AsInstance;

            internal Attributes(Type owner, bool asInstance)
            {
                this.ownerType = owner;
                this.AsInstance = asInstance;
            }

            internal Attributes(Module owner)
            {
                this.OwnerModule = owner;
            }

            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            public Attributes NoVisibilityChecks { get { SkipVisibility = true; return this; } }

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
            this._attrs = attrs;

            if (attrs.AsInstance)
                Parameter(attrs.ownerType, "this");
        }

        protected override void CreateMember()
        {
            if (_attrs.ownerType != null)
                try
                {
                    this._dm = new DynamicMethod(_attrs.name, ReturnType, ParameterTypes, _attrs.ownerType, _attrs.SkipVisibility);
                }
                catch
                {
                    this._dm = new DynamicMethod(_attrs.name, ReturnType, ParameterTypes, _attrs.ownerType, false);
                }
            else
                try
                {
                    this._dm = new DynamicMethod(_attrs.name, ReturnType, ParameterTypes, _attrs.OwnerModule, _attrs.SkipVisibility);
                }
                catch
                {
                    this._dm = new DynamicMethod(_attrs.name, ReturnType, ParameterTypes, _attrs.OwnerModule, false);
                }
        }

        protected override void RegisterMember()
        {
            // nothing to register
        }

        public bool IsCompleted => !(SignatureComplete && GetCode().IsCompleted);

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

            return _dm;
        }

        #region RoutineGen concrete implementation

        protected override bool HasCode => true;

        protected override ILGenerator GetILGenerator()
        {
            return _dm.GetILGenerator();
        }

        protected override ParameterBuilder DefineParameter(int position, ParameterAttributes attributes, string parameterName)
        {
            return _dm.DefineParameter(position, attributes, parameterName);
        }

        protected override MemberInfo Member => _dm;

        public override string Name => _attrs.name;

        protected internal override bool IsStatic => !_attrs.AsInstance;

        protected internal override bool IsOverride => false;

        protected override AttributeTargets AttributeTarget
        {
            get { throw new InvalidOperationException(Properties.Messages.ErrDynamicMethodNoCustomAttrs); }
        }

        protected override void SetCustomAttribute(CustomAttributeBuilder cab)
        {
            throw new InvalidOperationException(Properties.Messages.ErrDynamicMethodNoCustomAttrs);
        }

        #endregion

        #region ICodeGenContext Members

        bool ICodeGenContext.SupportsScopes => false;

        #endregion
    }
}
#endif