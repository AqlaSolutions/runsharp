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
using System.Reflection;

namespace TriAxis.RunSharp
{
	public class ParameterGen
	{
		ParameterGenCollection owner;
		int position;
		Type parameterType;
		ParameterAttributes attributes = ParameterAttributes.None;
		string name;
		bool va;

		internal ParameterGen(ParameterGenCollection owner, int position, Type parameterType, ParameterAttributes attributes, string name, bool va)
		{
			this.owner = owner;
			this.position = position;
			this.parameterType = parameterType;
			this.attributes = attributes;
			this.name = name;
			this.va = va;
		}

		public int Position { get { return position; } }
		public Type Type { get { return parameterType; } }
		public string Name { get { return name; } }
		public ParameterAttributes ParameterAttributes { get { return attributes; } }
		internal bool IsParameterArray { get { return va; } }

		internal void Complete(ISignatureGen sig)
		{
			sig.DefineParameter(position, attributes, name);
		}
	}

	public class ParameterGen<TOuterContext> : ParameterGen
	{
		TOuterContext context;

		internal ParameterGen(TOuterContext context, ParameterGenCollection owner, int position, Type parameterType, ParameterAttributes attributes, string name, bool va)
			: base(owner, position, parameterType, attributes, name, va)
		{
			this.context = context;
		}

		public TOuterContext End()
		{
			return context;
		}
	}

	class ParameterGenCollection : IList<ParameterGen>
	{
		ParameterGen[] array = EmptyArray<ParameterGen>.Instance;
		Type[] typeArray = Type.EmptyTypes;
		int count = 0;
		bool locked = false;

		internal void Lock()
		{
			locked = true;
		}

		internal void LockCheck()
		{
			if (locked)
				throw new InvalidOperationException();
		}

		public Type[] TypeArray
		{
			get
			{
				if (typeArray != null)
					return typeArray;

				Type[] arr = new Type[count];
				for (int i = 0; i < arr.Length; i++)
					arr[i] = array[i].Type;

				if (locked)
					typeArray = arr;

				return arr;
			}
		}

		#region IList<ParameterGen> Members

		public int IndexOf(ParameterGen item)
		{
			if (item == null)
				return -1;
			return Array.IndexOf(array, item, 0, count);
		}

		public void Insert(int index, ParameterGen item)
		{
			throw new NotSupportedException();
		}

		public void RemoveAt(int index)
		{
			throw new NotSupportedException();
		}

		public ParameterGen this[int index]
		{
			get
			{
				if (index < 0 || index >= count)
					throw new IndexOutOfRangeException();
				return array[index];
			}
			set
			{
				throw new NotSupportedException();
			}
		}

		#endregion

		#region ICollection<ParameterGen> Members

		public void Add(ParameterGen item)
		{
			LockCheck();

			if (item.IsParameterArray && count > 0 && array[count - 1].IsParameterArray)
				throw new InvalidOperationException(Properties.Messages.ErrParamArrayMustBeLast);

			AddUnchecked(item);
		}

		internal void AddUnchecked(ParameterGen item)
		{
			if (array.Length <= count)
				Array.Resize(ref array, count + 16);
			array[count++] = item;
			typeArray = null;
		}

		public void Clear()
		{
			throw new NotSupportedException();
		}

		public bool Contains(ParameterGen item)
		{
			return IndexOf(item) != -1;
		}

		public void CopyTo(ParameterGen[] array, int arrayIndex)
		{
			for (int i = 0; i < count; i++)
				array[arrayIndex++] = this.array[i];
		}

		public int Count
		{
			get { return count; }
		}

		public bool IsReadOnly
		{
			get { return locked; }
		}

		public bool Remove(ParameterGen item)
		{
			throw new NotSupportedException();
		}

		#endregion

		#region IEnumerable<ParameterGen> Members

		public IEnumerator<ParameterGen> GetEnumerator()
		{
			for (int i = 0; i < count; i++)
				yield return array[i];
		}

		#endregion

		#region IEnumerable Members

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		#endregion
	}
}
