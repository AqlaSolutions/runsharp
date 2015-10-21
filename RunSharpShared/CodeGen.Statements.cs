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
using System.Collections;
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
	using Operands;

	partial class CodeGen
	{
		#region Assignment
		public void Assign(Operand target, Operand value)
		{
			Assign(target, value, false);
		}

		public void Assign(Operand target, Operand value, bool allowExplicitConversion)
		{
			if ((object)target == null)
				throw new ArgumentNullException("target");

			BeforeStatement();

			target.Assign(value, allowExplicitConversion).Emit(this);
		}

		public void AssignAdd(Operand target, Operand value)
		{
			if ((object)target == null)
				throw new ArgumentNullException("target");

			BeforeStatement();

			target.AssignAdd(value).Emit(this);
		}

		public void AssignSubtract(Operand target, Operand value)
		{
			if ((object)target == null)
				throw new ArgumentNullException("target");

			BeforeStatement();

			target.AssignSubtract(value).Emit(this);
		}

		public void AssignMultiply(Operand target, Operand value)
		{
			if ((object)target == null)
				throw new ArgumentNullException("target");

			BeforeStatement();

			target.AssignMultiply(value).Emit(this);
		}

		public void AssignDivide(Operand target, Operand value)
		{
			if ((object)target == null)
				throw new ArgumentNullException("target");

			BeforeStatement();

			target.AssignDivide(value).Emit(this);
		}

		public void AssignModulus(Operand target, Operand value)
		{
			if ((object)target == null)
				throw new ArgumentNullException("target");

			BeforeStatement();

			target.AssignModulus(value).Emit(this);
		}

		public void AssignAnd(Operand target, Operand value)
		{
			if ((object)target == null)
				throw new ArgumentNullException("target");

			BeforeStatement();

			target.AssignAnd(value).Emit(this);
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", Justification = "Checked, OK")]
		public void AssignOr(Operand target, Operand value)
		{
			if ((object)target == null)
				throw new ArgumentNullException("target");

			BeforeStatement();

			target.AssignOr(value).Emit(this);
		}

		public void AssignXor(Operand target, Operand value)
		{
			if ((object)target == null)
				throw new ArgumentNullException("target");

			BeforeStatement();

			target.AssignXor(value).Emit(this);
		}

		public void AssignLeftShift(Operand target, Operand value)
		{
			if ((object)target == null)
				throw new ArgumentNullException("target");

			BeforeStatement();

			target.AssignLeftShift(value).Emit(this);
		}

		public void AssignRightShift(Operand target, Operand value)
		{
			if ((object)target == null)
				throw new ArgumentNullException("target");

			BeforeStatement();

			target.AssignRightShift(value).Emit(this);
		}

		public void Increment(Operand target)
		{
			if ((object)target == null)
				throw new ArgumentNullException("target");

			BeforeStatement();

			target.Increment().Emit(this);
		}

		public void Decrement(Operand target)
		{
			if ((object)target == null)
				throw new ArgumentNullException("target");

			BeforeStatement();

			target.Decrement().Emit(this);
		}
		#endregion

		#region Constructor chaining
		public void InvokeThis(params Operand[] args)
		{
			if (_cg == null)
				throw new InvalidOperationException(Properties.Messages.ErrConstructorOnlyCall);
			if (_chainCalled)
				throw new InvalidOperationException(Properties.Messages.ErrConstructorAlreadyChained);

			ApplicableFunction other = TypeInfo.FindConstructor(_cg.Type, args);

			_il.Emit(OpCodes.Ldarg_0);
			other.EmitArgs(this, args);
			_il.Emit(OpCodes.Call, (ConstructorInfo)other.Method.Member);
			_chainCalled = true;
		}

		public void InvokeBase(params Operand[] args)
		{
			if (_cg == null)
				throw new InvalidOperationException(Properties.Messages.ErrConstructorOnlyCall);
			if (_chainCalled)
				throw new InvalidOperationException(Properties.Messages.ErrConstructorAlreadyChained);
			if (_cg.Type.TypeBuilder.IsValueType)
				throw new InvalidOperationException(Properties.Messages.ErrStructNoBaseCtor);

			ApplicableFunction other = TypeInfo.FindConstructor(_cg.Type.BaseType, args);

			if (other == null)
				throw new InvalidOperationException(Properties.Messages.ErrMissingConstructor);

			_il.Emit(OpCodes.Ldarg_0);
			other.EmitArgs(this, args);
			_il.Emit(OpCodes.Call, (ConstructorInfo)other.Method.Member);
			_chainCalled = true;

			// when the chain continues to base, we also need to call the common constructor
			_il.Emit(OpCodes.Ldarg_0);
			_il.Emit(OpCodes.Call, _cg.Type.CommonConstructor().GetMethodBuilder());
		}
		#endregion

		void BeforeStatement()
		{
			if (!_reachable)
				throw new InvalidOperationException(Properties.Messages.ErrCodeNotReachable);

			if (_cg != null && !_chainCalled && !_cg.Type.TypeBuilder.IsValueType)
				InvokeBase();
		}

		void DoInvoke(Operand invocation)
		{
			BeforeStatement();

			invocation.EmitGet(this);
			if (!Helpers.AreTypesEqual(invocation.Type, typeof(void), _typeMapper))
				_il.Emit(OpCodes.Pop);
		}

		#region Invocation
		public void Invoke(Type target, string method)
		{
			Invoke(target, method, Operand.EmptyArray);
		}

		public void Invoke(Type target, string method, params Operand[] args)
		{
			DoInvoke(Static.Invoke(target, method, args));
		}

		public void Invoke(Operand target, string method)
		{
			Invoke(target, method, Operand.EmptyArray);
		}

		public void Invoke(Operand target, string method, params Operand[] args)
		{
			if ((object)target == null)
				throw new ArgumentNullException("target");

			DoInvoke(target.Invoke(method, args));
		}

		public void InvokeDelegate(Operand targetDelegate)
		{
			InvokeDelegate(targetDelegate, Operand.EmptyArray);
		}

		public void InvokeDelegate(Operand targetDelegate, params Operand[] args)
		{
			if ((object)targetDelegate == null)
				throw new ArgumentNullException("targetDelegate");

			DoInvoke(targetDelegate.InvokeDelegate(args));
		}

	    ITypeMapper _typeMapper;

	    public CodeGen(ITypeMapper typeMapper)
	    {
	        this._typeMapper = typeMapper;
	    }
        
	    public void WriteLine(params Operand[] args)
		{
			Invoke(_typeMapper.MapType(typeof(Console)), "WriteLine", args);
		}
		#endregion

		#region Event subscription
		public void SubscribeEvent(Operand target, string eventName, Operand handler)
		{
			if ((object)target == null)
				throw new ArgumentNullException("target");
			if ((object)handler == null)
				throw new ArgumentNullException("handler");

			IMemberInfo evt = TypeInfo.FindEvent(target.Type, eventName, target.IsStaticTarget);
			MethodInfo mi = ((EventInfo)evt.Member).GetAddMethod();
			if (!target.IsStaticTarget)
				target.EmitGet(this);
			handler.EmitGet(this);
			EmitCallHelper(mi, target);
		}

		public void UnsubscribeEvent(Operand target, string eventName, Operand handler)
		{
			if ((object)target == null)
				throw new ArgumentNullException("target");
			if ((object)handler == null)
				throw new ArgumentNullException("handler");

			IMemberInfo evt = TypeInfo.FindEvent(target.Type, eventName, target.IsStaticTarget);
			MethodInfo mi = ((EventInfo)evt.Member).GetRemoveMethod();
			if (!target.IsStaticTarget)
				target.EmitGet(this);
			handler.EmitGet(this);
			EmitCallHelper(mi, target);
		}
		#endregion

		public void InitObj(Operand target)
		{
			if ((object)target == null)
				throw new ArgumentNullException("target");

			BeforeStatement();

			target.EmitAddressOf(this);
			_il.Emit(OpCodes.Initobj, target.Type);
		}

		#region Flow Control
		interface IBreakable
		{
			Label GetBreakTarget();
		}

		interface IContinuable
		{
			Label GetContinueTarget();
		}

		public void Break()
		{
			BeforeStatement();

			bool useLeave = false;

			foreach (Block blk in _blocks)
			{
				ExceptionBlock xb = blk as ExceptionBlock;

				if (xb != null)
				{
					if (xb.IsFinally)
						throw new InvalidOperationException(Properties.Messages.ErrInvalidFinallyBranch);

					useLeave = true;
				}

				IBreakable brkBlock = blk as IBreakable;

				if (brkBlock != null)
				{
					_il.Emit(useLeave ? OpCodes.Leave : OpCodes.Br, brkBlock.GetBreakTarget());
					_reachable = false;
					return;
				}
			}

			throw new InvalidOperationException(Properties.Messages.ErrInvalidBreak);
		}

		public void Continue()
		{
			BeforeStatement();

			bool useLeave = false;

			foreach (Block blk in _blocks)
			{
				ExceptionBlock xb = blk as ExceptionBlock;

				if (xb != null)
				{
					if (xb.IsFinally)
						throw new InvalidOperationException(Properties.Messages.ErrInvalidFinallyBranch);

					useLeave = true;
				}

				IContinuable cntBlock = blk as IContinuable;

				if (cntBlock != null)
				{
					_il.Emit(useLeave ? OpCodes.Leave : OpCodes.Br, cntBlock.GetContinueTarget());
					_reachable = false;
					return;
				}
			}

			throw new InvalidOperationException(Properties.Messages.ErrInvalidContinue);
		}

		public void Return()
		{
		    if (_context.ReturnType != null && !Helpers.AreTypesEqual(_context.ReturnType, typeof(void), _typeMapper))
				throw new InvalidOperationException(Properties.Messages.ErrMethodMustReturnValue);

			BeforeStatement();

			ExceptionBlock xb = GetAnyTryBlock();

			if (xb == null)
			{
				_il.Emit(OpCodes.Ret);
			}
			else if (xb.IsFinally)
			{
				throw new InvalidOperationException(Properties.Messages.ErrInvalidFinallyBranch);
			}
			else
			{
				EnsureReturnVariable();
				_il.Emit(OpCodes.Leave, _retLabel);
			}

			_reachable = false;
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters", Justification = "'Operand' required as type to use provided implicit conversions")]
		public void Return(Operand value)
		{
			if (_context.ReturnType == null || Helpers.AreTypesEqual(_context.ReturnType, typeof(void), _typeMapper))
				throw new InvalidOperationException(Properties.Messages.ErrVoidMethodReturningValue);

			BeforeStatement();

			EmitGetHelper(value, _context.ReturnType, false);

			ExceptionBlock xb = GetAnyTryBlock();

			if (xb == null)
			{
				_il.Emit(OpCodes.Ret);
			}
			else if (xb.IsFinally)
			{
				throw new InvalidOperationException(Properties.Messages.ErrInvalidFinallyBranch);
			}
			else
			{
				EnsureReturnVariable();
				_il.Emit(OpCodes.Stloc, _retVar);
				_il.Emit(OpCodes.Leave, _retLabel);
			}
			_reachable = false;
		}

		public void Throw()
		{
			BeforeStatement();

			_il.Emit(OpCodes.Rethrow);
			_reachable = false;
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters", Justification = "'Operand' required as type to use provided implicit conversions")]
		public void Throw(Operand exception)
		{
			BeforeStatement();

			EmitGetHelper(exception, _typeMapper.MapType(typeof(Exception)), false);
			_il.Emit(OpCodes.Throw);
			_reachable = false;
		}

		public void For(IStatement init, Operand test, IStatement iterator)
		{
			Begin(new LoopBlock(init, test, iterator));
		}

		public void While(Operand test)
		{
			Begin(new LoopBlock(null, test, null));
		}

		public Operand ForEach(Type elementType, Operand expression)
		{
			ForeachBlock fb = new ForeachBlock(elementType, expression, _typeMapper);
			Begin(fb);
			return fb.Element;
		}

		public void If(Operand condition)
		{
			Begin(new IfBlock(condition));
		}

		public void Else()
		{
			IfBlock ifBlk = GetBlock() as IfBlock;
			if (ifBlk == null)
				throw new InvalidOperationException(Properties.Messages.ErrElseWithoutIf);

			_blocks.Pop();
			Begin(new ElseBlock(ifBlk));
		}

		public void Try()
		{
			Begin(new ExceptionBlock(_typeMapper));
		}

		ExceptionBlock GetTryBlock()
		{
			ExceptionBlock tryBlk = GetBlock() as ExceptionBlock;
			if (tryBlk == null)
				throw new InvalidOperationException(Properties.Messages.ErrInvalidExceptionStatement);
			return tryBlk;
		}

		ExceptionBlock GetAnyTryBlock()
		{
			foreach (Block blk in _blocks)
			{
				ExceptionBlock tryBlk = blk as ExceptionBlock;

				if (tryBlk != null)
					return tryBlk;
			}

			return null;
		}

		public Operand Catch(Type exceptionType)
		{
			return GetTryBlock().BeginCatch(exceptionType);
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", Justification = "Intentional")]
		public void CatchAll()
		{
			GetTryBlock().BeginCatchAll();
		}

		public void Fault()
		{
			GetTryBlock().BeginFault();
		}

		public void Finally()
		{
			GetTryBlock().BeginFinally();
		}

		public void Switch(Operand expression)
		{
			Begin(new SwitchBlock(expression, _typeMapper));
		}

		SwitchBlock GetSwitchBlock()
		{
			SwitchBlock switchBlk = GetBlock() as SwitchBlock;
			if (switchBlk == null)
				throw new InvalidOperationException(Properties.Messages.ErrInvalidSwitchStatement);
			return switchBlk;
		}

		public void Case(object value)
		{
			IConvertible conv = value as IConvertible;

			if (conv == null)
				throw new ArgumentException(Properties.Messages.ErrArgMustImplementIConvertible, "value");

			GetSwitchBlock().Case(conv);
		}

		public void DefaultCase()
		{
			GetSwitchBlock().Case(null);
		}
		#endregion

		Block GetBlock()
		{
			if (_blocks.Count == 0)
				return null;

			return _blocks.Peek();
		}

		Block GetBlockForVariable()
		{
			if (_blocks.Count == 0)
				return null;

			Block b = _blocks.Peek();
			b.EnsureScope();
			return b;
		}

		void Begin(Block b)
		{
			_blocks.Push(b);
			b.G = this;
			b.Begin();
		}

		public void End()
		{
			if (_blocks.Count == 0)
				throw new InvalidOperationException(Properties.Messages.ErrNoOpenBlocks);

			_blocks.Peek().End();
			_blocks.Pop();
		}

		abstract class Block
		{
			bool _hasScope;
			internal CodeGen G;

			public void EnsureScope()
			{
				if (!_hasScope)
				{
					if (G._context.SupportsScopes)
						G._il.BeginScope();
					_hasScope = true;
				}
			}

			protected void EndScope()
			{
				if (_hasScope)
				{
					if (G._context.SupportsScopes)
						G._il.EndScope();
					_hasScope = false;
				}
			}

			public void Begin()
			{
				BeginImpl();
			}

			public void End()
			{
				EndImpl();
				EndScope();
			}

			protected abstract void BeginImpl();
			protected abstract void EndImpl();
		}

		class IfBlock : Block
		{
			Operand _condition;

			public IfBlock(Operand condition)
			{
				if (!Helpers.AreTypesEqual(condition.Type, typeof(bool)))
					this._condition = condition.IsTrue();
				else
					this._condition = condition;
			}

			Label _lbSkip;

			protected override void BeginImpl()
			{
				G.BeforeStatement();

				_lbSkip = G._il.DefineLabel();
				_condition.EmitBranch(G, BranchSet.Inverse, _lbSkip);
			}

			protected override void EndImpl()
			{
				G._il.MarkLabel(_lbSkip);
				G._reachable = true;
			}
		}

		class ElseBlock : Block
		{
			IfBlock _ifBlk;
			Label _lbSkip;
			bool _canSkip;

			public ElseBlock(IfBlock ifBlk)
			{
				this._ifBlk = ifBlk;
			}

			protected override void BeginImpl()
			{
				if (_canSkip = G._reachable)
				{
					_lbSkip = G._il.DefineLabel();
					G._il.Emit(OpCodes.Br, _lbSkip);
				}
				_ifBlk.End();
			}

			protected override void EndImpl()
			{
				if (_canSkip)
				{
					G._il.MarkLabel(_lbSkip);
					G._reachable = true;
				}
			}
		}

		class LoopBlock : Block, IBreakable, IContinuable
		{
			IStatement _init;
			Operand _test;
			IStatement _iter;

			public LoopBlock(IStatement init, Operand test, IStatement iter)
			{
				this._init = init;
				this._test = test;
				this._iter = iter;

				if (!Helpers.AreTypesEqual(test.Type, typeof(bool)))
					test = test.IsTrue();
			}

			Label _lbLoop, _lbTest, _lbEnd, _lbIter;
			bool _endUsed = false, _iterUsed = false;

			protected override void BeginImpl()
			{
				G.BeforeStatement();

				_lbLoop = G._il.DefineLabel();
				_lbTest = G._il.DefineLabel();
				if (_init != null)
					_init.Emit(G);
				G._il.Emit(OpCodes.Br, _lbTest);
				G._il.MarkLabel(_lbLoop);
			}

			protected override void EndImpl()
			{
				if (_iter != null)
				{
					if (_iterUsed)
						G._il.MarkLabel(_lbIter);
				
					_iter.Emit(G);
				}

				G._il.MarkLabel(_lbTest);
				_test.EmitBranch(G, BranchSet.Normal, _lbLoop);

				if (_endUsed)
					G._il.MarkLabel(_lbEnd);

				G._reachable = true;
			}

			public Label GetBreakTarget()
			{
				if (!_endUsed)
				{
					_lbEnd = G._il.DefineLabel();
					_endUsed = true;
				}
				return _lbEnd;
			}

			public Label GetContinueTarget()
			{
				if (_iter == null)
					return _lbTest;

				if (!_iterUsed)
				{
					_lbIter = G._il.DefineLabel();
					_iterUsed = true;
				}
				return _lbIter;
			}
		}

		// TODO: proper implementation, including dispose
		class ForeachBlock : Block, IBreakable, IContinuable
		{
			Type _elementType;
			Operand _collection;
		    ITypeMapper _typeMapper;

			public ForeachBlock(Type elementType, Operand collection, ITypeMapper typeMapper)
			{
				this._elementType = elementType;
				this._collection = collection;
			    this._typeMapper = typeMapper;
			}

			Operand _enumerator, _element;
			Label _lbLoop, _lbTest, _lbEnd;
			bool _endUsed = false;

			public Operand Element { get { return _element; } }

			protected override void BeginImpl()
			{
				G.BeforeStatement();

				_enumerator = G.Local();
				_lbLoop = G._il.DefineLabel();
				_lbTest = G._il.DefineLabel();

			    if (Helpers.IsAssignableFrom(typeof(IEnumerable), _collection.Type, _typeMapper))
			        _collection = _collection.Cast(_typeMapper.MapType(typeof(IEnumerable)));

				G.Assign(_enumerator, _collection.Invoke("GetEnumerator"));
				G._il.Emit(OpCodes.Br, _lbTest);
				G._il.MarkLabel(_lbLoop);
				_element = G.Local(_elementType);
				G.Assign(_element, _enumerator.Property("Current"), true);
			}

			protected override void EndImpl()
			{
				G._il.MarkLabel(_lbTest);
				_enumerator.Invoke("MoveNext").EmitGet(G);

				G._il.Emit(OpCodes.Brtrue, _lbLoop);

				if (_endUsed)
					G._il.MarkLabel(_lbEnd); 
				
				G._reachable = true;
			}

			public Label GetBreakTarget()
			{
				if (!_endUsed)
				{
					_lbEnd = G._il.DefineLabel();
					_endUsed = true;
				}
				return _lbEnd;
			}

			public Label GetContinueTarget()
			{
				return _lbTest;
			}
		}

		class ExceptionBlock : Block
		{
			bool _endReachable = false;
			bool _isFinally = false;

		    ITypeMapper _typeMapper;

		    public ExceptionBlock(ITypeMapper typeMapper)
		    {
		        this._typeMapper = typeMapper;
		    }

		    protected override void BeginImpl()
			{
				G._il.BeginExceptionBlock();
			}

			public void BeginCatchAll()
			{
				EndScope();

				if (G._reachable)
					_endReachable = true;
				G._il.BeginCatchBlock(_typeMapper.MapType(typeof(object)));
				G._il.Emit(OpCodes.Pop);
				G._reachable = true;
			}

			public Operand BeginCatch(Type t)
			{
				EndScope();

				if (G._reachable)
					_endReachable = true;

				G._il.BeginCatchBlock(t);
				LocalBuilder lb = G._il.DeclareLocal(t);
				G._il.Emit(OpCodes.Stloc, lb);
				G._reachable = true;

				return new _Local(G, lb);
			}

			public void BeginFault()
			{
				EndScope();

				G._il.BeginFaultBlock();
				G._reachable = true;
				_isFinally = true;
			}

			public void BeginFinally()
			{
				EndScope();

				G._il.BeginFinallyBlock();
				G._reachable = true;
				_isFinally = true;
			}

			protected override void EndImpl()
			{
				G._il.EndExceptionBlock();
				G._reachable = _endReachable;
			}

			public bool IsFinally { get { return _isFinally; } }
		}

		class SwitchBlock : Block, IBreakable
		{
			static System.Type[] _validTypes = { 
				typeof(sbyte), typeof(byte), typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(char), typeof(string)
			};
			MethodInfo _strCmp;

			Operand _expression;
			Conversion _conv;
			Type _govType;
			Label _lbDecision;
			Label _lbEnd;
			Label _lbDefault;
			LocalBuilder _exp;
			bool _defaultExists = false;
			bool _endReachable = false;
			SortedList<IComparable, Label> _cases = new SortedList<IComparable, Label>();

		    ITypeMapper _typeMapper;

			public SwitchBlock(Operand expression, ITypeMapper typeMapper)
			{
			    this._typeMapper = typeMapper;
			    _strCmp = typeMapper.MapType(typeof(string)).GetMethod(
			        "Equals",
			        BindingFlags.Public | BindingFlags.Static,
			        null,
			        new Type[] { typeMapper.MapType(typeof(string)), typeMapper.MapType(typeof(string)) },
			        null);

                this._expression = expression;

				Type exprType = expression.Type;
				if (Array.IndexOf(_validTypes, exprType) != -1)
					_govType = exprType;
				else if (exprType.IsEnum)
					_govType = Helpers.GetEnumEnderlyingType(exprType);
				else
				{
					// if a single implicit coversion from expression to one of the valid types exists, it's ok
					foreach (System.Type t in _validTypes)
					{
						Conversion tmp = Conversion.GetImplicit(expression, typeMapper.MapType(t), false);
						if (tmp.IsValid)
						{
							if (_conv == null)
							{
								_conv = tmp;
								_govType = typeMapper.MapType(t);
							}
							else
								throw new AmbiguousMatchException(Properties.Messages.ErrAmbiguousSwitchExpression);
						}
					}
				}
			}

			protected override void BeginImpl()
			{
				_lbDecision = G._il.DefineLabel();
				_lbDefault = _lbEnd = G._il.DefineLabel();

				_expression.EmitGet(G);
				if (_conv != null)
					_conv.Emit(G, _expression.Type, _govType);
				_exp = G._il.DeclareLocal(_govType);
				G._il.Emit(OpCodes.Stloc, _exp);
				G._il.Emit(OpCodes.Br, _lbDecision);
				G._reachable = false;
			}

			public void Case(IConvertible value)
			{
				bool duplicate;

				// make sure the value is of the governing type
				IComparable val = value == null ? null : (IComparable)value.ToType(System.Type.GetType(_govType.FullName, true), System.Globalization.CultureInfo.InvariantCulture);

				if (value == null)
					duplicate = _defaultExists;
				else
					duplicate = _cases.ContainsKey(val);

				if (duplicate)
					throw new InvalidOperationException(Properties.Messages.ErrDuplicateCase);

				if (G._reachable)
					G._il.Emit(OpCodes.Br, _lbEnd);

				EndScope();
				Label lb = G._il.DefineLabel();
				G._il.MarkLabel(lb);
				if (value == null)
				{
					_defaultExists = true;
					_lbDefault = lb;
				}
				else
				{
					_cases[val] = lb;
				}
				G._reachable = true;
			}

			static int Diff(IConvertible val1, IConvertible val2)
			{
				ulong diff;

				switch (val1.GetTypeCode())
				{
					case TypeCode.UInt64:
						diff = val2.ToUInt64(null) - val1.ToUInt64(null);
						break;
					case TypeCode.Int64:
						diff = (ulong)(val2.ToInt64(null) - val1.ToInt64(null));
						break;
					case TypeCode.UInt32:
						diff = val2.ToUInt32(null) - val1.ToUInt32(null);
						break;
					default:
						diff = (ulong)(val2.ToInt32(null) - val1.ToInt32(null));
						break;
				}

				if (diff >= int.MaxValue)
					return int.MaxValue;
				else
					return (int)diff;
			}

			void Finish(List<Label> labels)
			{
				switch (labels.Count)
				{
					case 0: break;
					case 1:
						G._il.Emit(OpCodes.Beq, labels[0]);
						break;
					default:
						G._il.Emit(OpCodes.Sub);
						G._il.Emit(OpCodes.Switch, labels.ToArray());
						break;
				}
			}

			void EmitValue(IConvertible val)
			{
				switch (val.GetTypeCode())
				{
					case TypeCode.UInt64:
						G.EmitI8Helper(unchecked((long)val.ToUInt64(null)), false);
						break;
					case TypeCode.Int64:
						G.EmitI8Helper(val.ToInt64(null), true);
						break;
					case TypeCode.UInt32:
						G.EmitI4Helper(unchecked((int)val.ToUInt64(null)));
						break;
					default:
						G.EmitI4Helper(val.ToInt32(null));
						break;
				}
			}

			protected override void EndImpl()
			{
				if (G._reachable)
				{
					G._il.Emit(OpCodes.Br, _lbEnd);
					_endReachable = true;
				}

				EndScope();
				G._il.MarkLabel(_lbDecision);

			    if (Helpers.AreTypesEqual(_govType, typeof(string), _typeMapper))
				{
					foreach (KeyValuePair<IComparable, Label> kvp in _cases)
					{
						G._il.Emit(OpCodes.Ldloc, _exp);
						G._il.Emit(OpCodes.Ldstr, kvp.Key.ToString());
						G._il.Emit(OpCodes.Call, _strCmp);
						G._il.Emit(OpCodes.Brtrue, kvp.Value);
					}
				}
				else
				{
					bool first = true;
					IConvertible prev = null;
					List<Label> labels = new List<Label>();

					foreach (KeyValuePair<IComparable, Label> kvp in _cases)
					{
						IConvertible val = (IConvertible)kvp.Key;
						if (prev != null)
						{
							int diff = Diff(prev, val);
							if (diff > 3)
							{
								Finish(labels);
								labels.Clear();
								prev = null;
								first = true;
							}
							else while (diff-- > 1)
									labels.Add(_lbDefault);
						}

						if (first)
						{
							G._il.Emit(OpCodes.Ldloc, _exp);
							EmitValue(val);
							first = false;
						}

						labels.Add(kvp.Value);
						prev = val;
					}

					Finish(labels);
				}
				if (_lbDefault != _lbEnd)
					G._il.Emit(OpCodes.Br, _lbDefault);
				G._il.MarkLabel(_lbEnd);
				G._reachable = _endReachable;
			}

			public Label GetBreakTarget()
			{
				_endReachable = true;
				return _lbEnd;
			}
		}
	}
}
