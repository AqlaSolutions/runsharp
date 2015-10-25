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
	static class EmptyArray<T>
	{
		public static readonly T[] Instance = { };
	}

	static class ArrayUtils
	{
		public static bool Equals(object[] array1, object[] array2)
		{
			if (array1 == array2)
				return true;

			if (array1 == null || array2 == null || array1.Length != array2.Length)
				return false;

			for (int i = 0; i < array1.Length; i++)
			{
				if (!Equals(array1[i], array2[i]))
					return false;
			}

			return true;
		}

		public static T[] Combine<T>(T[] array, T item)
		{
			if (array == null || array.Length == 0)
				return new T[] { item };

			T[] newArray = new T[array.Length + 1];
			array.CopyTo(newArray, 0);
			newArray[array.Length] = item;
			return newArray;
		}

		public static T[] Combine<T>(T item, T[] array)
		{
			if (array == null || array.Length == 0)
				return new T[] { item };

			T[] newArray = new T[array.Length + 1];
			newArray[0] = item;
			array.CopyTo(newArray, 1);
			return newArray;
		}

		public static Type[] GetTypes(ParameterInfo[] paramInfos)
		{
			if (paramInfos == null)
				return null;

			Type[] types = new Type[paramInfos.Length];
			for (int i = 0; i < types.Length; i++)
				types[i] = paramInfos[i].ParameterType;
			return types;
		}

		public static T[] ToArray<T>(ICollection<T> collection)
		{
			if (collection == null)
				return null;

			if (collection.Count == 0)
				return EmptyArray<T>.Instance;

			T[] array = new T[collection.Count];
			collection.CopyTo(array, 0);
			return array;
		}
	}
}
