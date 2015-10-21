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
	public class ParameterGen
	{
		ParameterGenCollection _owner;
	    readonly int _position;
	    readonly Type _parameterType;
	    readonly ParameterAttributes _attributes = ParameterAttributes.None;
	    readonly string _name;
	    readonly bool _va;
		internal List<AttributeGen> CustomAttributes;

		internal ParameterGen(ParameterGenCollection owner, int position, Type parameterType, ParameterAttributes attributes, string name, bool va)
		{
			this._owner = owner;
			this._position = position;
			this._parameterType = parameterType;
			this._attributes = attributes;
			this._name = name;
			this._va = va;
		}

		public int Position { get { return _position; } }
		public Type Type { get { return _parameterType; } }
		public string Name { get { return _name; } }
		public ParameterAttributes ParameterAttributes { get { return _attributes; } }
		internal bool IsParameterArray { get { return _va; } }

		#region Custom Attributes

		public ParameterGen Attribute(AttributeType type)
		{
			BeginAttribute(type);
			return this;
		}

		public ParameterGen Attribute(AttributeType type, params object[] args)
		{
			BeginAttribute(type, args);
			return this;
		}

		public AttributeGen<ParameterGen> BeginAttribute(AttributeType type)
		{
			return BeginAttribute(type, EmptyArray<object>.Instance);
		}

		public AttributeGen<ParameterGen> BeginAttribute(AttributeType type, params object[] args)
		{
			return AttributeGen<ParameterGen>.CreateAndAdd(this, ref CustomAttributes, _position == 0 ? AttributeTargets.ReturnValue : AttributeTargets.Parameter, type, args);
		}

		#endregion

		internal void Complete(ISignatureGen sig)
		{
			ParameterBuilder pb = sig.DefineParameter(_position, _attributes, _name);

			if (CustomAttributes != null && pb != null)
			{
				AttributeGen.ApplyList(ref CustomAttributes, pb.SetCustomAttribute);
			}
		}
	}

	public class ParameterGen<TOuterContext> : ParameterGen
	{
	    readonly TOuterContext _context;

		internal ParameterGen(TOuterContext context, ParameterGenCollection owner, int position, Type parameterType, ParameterAttributes attributes, string name, bool va)
			: base(owner, position, parameterType, attributes, name, va)
		{
			this._context = context;
		}

		#region Custom Attributes

		public new ParameterGen<TOuterContext> Attribute(AttributeType type)
		{
			BeginAttribute(type);
			return this;
		}

		public new ParameterGen<TOuterContext> Attribute(AttributeType type, params object[] args)
		{
			BeginAttribute(type, args);
			return this;
		}

		public new AttributeGen<ParameterGen<TOuterContext>> BeginAttribute(AttributeType type)
		{
			return BeginAttribute(type, EmptyArray<object>.Instance);
		}

		public new AttributeGen<ParameterGen<TOuterContext>> BeginAttribute(AttributeType type, params object[] args)
		{
			return AttributeGen<ParameterGen<TOuterContext>>.CreateAndAdd(this, ref CustomAttributes, AttributeTargets.Delegate, type, args);
		}

		#endregion

		public TOuterContext End()
		{
			return _context;
		}
	}

	class ParameterGenCollection : IList<ParameterGen>
	{
		ParameterGen[] _array = EmptyArray<ParameterGen>.Instance;
		Type[] _typeArray = Type.EmptyTypes;
		int _count = 0;
		bool _locked = false;

		internal void Lock()
		{
			_locked = true;
		}

		internal void LockCheck()
		{
			if (_locked)
				throw new InvalidOperationException();
		}

		public Type[] TypeArray
		{
			get
			{
				if (_typeArray != null)
					return _typeArray;

				Type[] arr = new Type[_count];
				for (int i = 0; i < arr.Length; i++)
					arr[i] = _array[i].Type;

				if (_locked)
					_typeArray = arr;

				return arr;
			}
		}

		#region IList<ParameterGen> Members

		public int IndexOf(ParameterGen item)
		{
			if (item == null)
				return -1;
			return Array.IndexOf(_array, item, 0, _count);
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
				if (index < 0 || index >= _count)
					throw new IndexOutOfRangeException();
				return _array[index];
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

			if (item.IsParameterArray && _count > 0 && _array[_count - 1].IsParameterArray)
				throw new InvalidOperationException(Properties.Messages.ErrParamArrayMustBeLast);

			AddUnchecked(item);
		}

		internal void AddUnchecked(ParameterGen item)
		{
			if (_array.Length <= _count)
				Array.Resize(ref _array, _count + 16);
			_array[_count++] = item;
			_typeArray = null;
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
			for (int i = 0; i < _count; i++)
				array[arrayIndex++] = this._array[i];
		}

		public int Count
		{
			get { return _count; }
		}

		public bool IsReadOnly
		{
			get { return _locked; }
		}

		public bool Remove(ParameterGen item)
		{
			throw new NotSupportedException();
		}

		#endregion

		#region IEnumerable<ParameterGen> Members

		public IEnumerator<ParameterGen> GetEnumerator()
		{
			for (int i = 0; i < _count; i++)
				yield return _array[i];
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
