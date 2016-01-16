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
    public interface ISignatureGen
    {
        ParameterBuilder DefineParameter(int position, ParameterAttributes attributes, string parameterName);

        IParameterBasicInfo GetParameterByName(string parameterName);
    }

    public abstract class SignatureGen<T> : MemberGenBase<T>, ISignatureGen where T : SignatureGen<T>
    {
        public ITypeMapper TypeMapper { get; }
        readonly ParameterGenCollection _parameters;
        internal readonly T TypedThis;

        internal SignatureGen(Type returnType, ITypeMapper typeMapper)
            : base(typeMapper)
        {
            TypeMapper = typeMapper;
            _parameters = new ParameterGenCollection(typeMapper);
            TypedThis = (T)this;
            if (returnType != null)
                ReturnParameter = new ParameterGen(_parameters, 0, returnType, 0, null, false);
        }

        internal bool SignatureComplete { get; set; }

        public int ParameterCount => _parameters.Count;

        public Type ReturnType => ReturnParameter == null ? null : ReturnParameter.Type;
        public Type[] ParameterTypes => _parameters.TypeArray;

        public ParameterGen ReturnParameter { get; }
        public IList<ParameterGen> Parameters => _parameters;

        #region Parameter Definition
        enum ParamModifier { None, Ref, Out, Params };
        ParamModifier _paramMod = ParamModifier.None;

        T SetModifier(ParamModifier mod)
        {
            if (_paramMod != ParamModifier.None)
                throw new InvalidOperationException(Properties.Messages.ErrMultiParamModifier);
            _paramMod = mod;
            return TypedThis;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public T Ref => SetModifier(ParamModifier.Ref);

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public T Out => SetModifier(ParamModifier.Ref);

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public T Params => SetModifier(ParamModifier.Params);

        public ParameterGen<T> BeginParameter(Type type, string name)
        {
            ParameterAttributes attrs = 0;
            bool va = false;

            switch (_paramMod)
            {
                case ParamModifier.Out:
                    attrs |= ParameterAttributes.Out;
                    goto case ParamModifier.Ref;
                case ParamModifier.Ref:
                    if (!type.IsByRef)
                        type = type.MakeByRefType();
                    break;
                case ParamModifier.Params:
                    if (!type.IsArray)
                        throw new InvalidOperationException(Properties.Messages.ErrParamArrayNotArray);
                    va = true;
                    break;
            }

            ParameterGen<T> pgen = new ParameterGen<T>(TypedThis, _parameters, _parameters.Count + 1, type, attrs, name, va);
            _parameters.Add(pgen);
            _paramMod = ParamModifier.None;
            return pgen;
        }

        internal T UncheckedParameter(Type type, string name)
        {
            _parameters.AddUnchecked(new ParameterGen(_parameters, _parameters.Count + 1, type, 0, name, false));
            return TypedThis;
        }

#if FEAT_IKVM
        public T Parameter(System.Type type, string name)
        {
            return Parameter(TypeMapper.MapType(type), name);
        }
#endif
        public T Parameter(Type type, string name)
        {
            BeginParameter(type, name);
            return TypedThis;
        }

        internal T CopyParameters(IList<ParameterGen> parameters)
        {
            for (int i = 0; i < parameters.Count; i++)
                _parameters.Add(parameters[i]);

            return TypedThis;
        }

        internal T LockSignature()
        {
            if (!SignatureComplete)
            {
                SignatureComplete = true;
                _parameters.Lock();
                OnParametersLocked();

                if (ReturnParameter != null)
                    ReturnParameter.Complete(this);

                foreach (ParameterGen pgen in _parameters)
                {
                    if (pgen != null)
                        pgen.Complete(this);
                }
                OnParametersCompleted();
            }

            return TypedThis;
        }
        #endregion

        #region Virtual methods
        protected virtual void OnParametersLocked() { }
        protected virtual void OnParametersCompleted() { }
        protected virtual ParameterBuilder DefineParameter(int position, ParameterAttributes attributes, string parameterName) { return null; }
        #endregion

        #region ICallableGen Members

        ParameterBuilder ISignatureGen.DefineParameter(int position, ParameterAttributes attributes, string parameterName)
        {
            return DefineParameter(position, attributes, parameterName);
        }

        IParameterBasicInfo ISignatureGen.GetParameterByName(string name)
        {
            foreach (ParameterGen pg in _parameters)
            {
                if (pg.Name == name)
                    return pg;
            }

            throw new ArgumentException(string.Format(Properties.Messages.ErrParamUnknown, name));
        }

        #endregion
    }
}
