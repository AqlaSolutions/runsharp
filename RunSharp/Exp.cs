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
using System.Text;

namespace TriAxis.RunSharp
{
	using Operands;

	public static class Exp
	{
		#region Construction expressions

		public static Operand New(Type type)
		{
			return New(type, Operand.EmptyArray);
		}

		public static Operand New(Type type, params Operand[] args)
		{
			ApplicableFunction ctor = OverloadResolver.Resolve(TypeInfo.GetConstructors(type), args);

			if (ctor == null)
				throw new MissingMethodException(Properties.Messages.ErrMissingConstructor);

			return new NewObject(ctor, args);
		}

		public static Operand NewArray(Type type, params Operand[] indexes)
		{
			return new NewArray(type, indexes);
		}

		public static Operand NewInitializedArray(Type type, params Operand[] elements)
		{
			return new InitializedArray(type, elements);
		}

		public static Operand NewDelegate(Type delegateType, Type target, string method)
		{
			return new NewDelegate(delegateType, target, method);
		}

		public static Operand NewDelegate(Type delegateType, Operand target, string method)
		{
			return new NewDelegate(delegateType, target, method);
		}
		#endregion
	}
}
