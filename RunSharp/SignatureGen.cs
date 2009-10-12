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
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;

namespace TriAxis.RunSharp
{
	interface ISignatureGen
	{
		ParameterBuilder DefineParameter(int position, ParameterAttributes attributes, string parameterName);

		ParameterGen GetParameterByName(string parameterName);
	}

	public abstract class SignatureGen<T> : ISignatureGen where T : SignatureGen<T>
	{
		ParameterGen returnParameter;
		ParameterGenCollection parameters = new ParameterGenCollection();
		bool signatureComplete;
		readonly T typedThis;

		internal SignatureGen(Type returnType)
		{
			typedThis = (T)this;
			if (returnType != null)
				this.returnParameter = new ParameterGen(parameters, 0, returnType, 0, null, false);
		}

		internal bool SignatureComplete { get { return signatureComplete; } }

		public int ParameterCount { get { return parameters.Count; } }

		public Type ReturnType { get { return returnParameter == null ? null : returnParameter.Type; } }
		public Type[] ParameterTypes { get { return parameters.TypeArray; } }

		public ParameterGen ReturnParameter { get { return returnParameter; } }
		public IList<ParameterGen> Parameters { get { return parameters; } }

		#region Parameter Definition
		enum ParamModifier { None, Ref, Out, Params };
		ParamModifier paramMod = ParamModifier.None;

		T SetModifier(ParamModifier mod)
		{
			if (paramMod != ParamModifier.None)
				throw new InvalidOperationException(Properties.Messages.ErrMultiParamModifier);
			paramMod = mod;
			return typedThis;
		}

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public T Ref { get { return SetModifier(ParamModifier.Ref); } }
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public T Out { get { return SetModifier(ParamModifier.Ref); } }
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public T Params { get { return SetModifier(ParamModifier.Params); } }

		public ParameterGen<T> BeginParameter(Type type, string name)
		{
			ParameterAttributes attrs = 0;
			bool va = false;

			switch (paramMod)
			{
				case ParamModifier.Out:
					attrs |= ParameterAttributes.Out;
					goto case ParamModifier.Ref;
				case ParamModifier.Ref:
					type = type.MakeByRefType();
					break;
				case ParamModifier.Params:
					if (!type.IsArray)
						throw new InvalidOperationException(Properties.Messages.ErrParamArrayNotArray);
					va = true;
					break;
			}

			ParameterGen<T> pgen = new ParameterGen<T>(typedThis, parameters, parameters.Count + 1, type, attrs, name, va);
			parameters.Add(pgen);
			paramMod = ParamModifier.None;
			return pgen;
		}

		internal T UncheckedParameter(Type type, string name)
		{
			parameters.AddUnchecked(new ParameterGen(parameters, parameters.Count + 1, type, 0, name, false));
			return typedThis;
		}

		public T Parameter(Type type, string name)
		{
			BeginParameter(type, name);
			return typedThis;
		}

		internal T CopyParameters(IList<ParameterGen> parameters)
		{
			for (int i = 0; i < parameters.Count; i++)
				this.parameters.Add(parameters[i]);

			return typedThis;
		}

		internal T LockSignature()
		{
			if (!signatureComplete)
			{
				signatureComplete = true;
				parameters.Lock();
				OnParametersLocked();

				if (returnParameter != null)
					returnParameter.Complete(this);

				foreach (ParameterGen pgen in parameters)
				{
					if (pgen != null)
						pgen.Complete(this);
				}
				OnParametersCompleted();
			}

			return typedThis;
		}
		#endregion

		#region Virtual methods
		protected virtual void OnParametersLocked() {}
		protected virtual void OnParametersCompleted() { }
		protected virtual ParameterBuilder DefineParameter(int position, ParameterAttributes attributes, string parameterName) { return null; }
		#endregion

		#region ICallableGen Members

		ParameterBuilder ISignatureGen.DefineParameter(int position, ParameterAttributes attributes, string parameterName)
		{
			return DefineParameter(position, attributes, parameterName);
		}

		ParameterGen ISignatureGen.GetParameterByName(string name)
		{
			foreach (ParameterGen pg in parameters)
			{
				if (pg.Name == name)
					return pg;
			}

			throw new ArgumentException(string.Format(Properties.Messages.ErrParamUnknown, name));
		}

		#endregion
	}
}
