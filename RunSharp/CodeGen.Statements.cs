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
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;

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
			if (cg == null)
				throw new InvalidOperationException(Properties.Messages.ErrConstructorOnlyCall);
			if (chainCalled)
				throw new InvalidOperationException(Properties.Messages.ErrConstructorAlreadyChained);

			ApplicableFunction other = TypeInfo.FindConstructor(cg.Type, args);

			il.Emit(OpCodes.Ldarg_0);
			other.EmitArgs(this, args);
			il.Emit(OpCodes.Call, (ConstructorInfo)other.Method.Member);
			chainCalled = true;
		}

		public void InvokeBase(params Operand[] args)
		{
			if (cg == null)
				throw new InvalidOperationException(Properties.Messages.ErrConstructorOnlyCall);
			if (chainCalled)
				throw new InvalidOperationException(Properties.Messages.ErrConstructorAlreadyChained);
			if (cg.Type.TypeBuilder.IsValueType)
				throw new InvalidOperationException(Properties.Messages.ErrStructNoBaseCtor);

			ApplicableFunction other = TypeInfo.FindConstructor(cg.Type.BaseType, args);

			if (other == null)
				throw new InvalidOperationException(Properties.Messages.ErrMissingConstructor);

			il.Emit(OpCodes.Ldarg_0);
			other.EmitArgs(this, args);
			il.Emit(OpCodes.Call, (ConstructorInfo)other.Method.Member);
			chainCalled = true;

			// when the chain continues to base, we also need to call the common constructor
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Call, cg.Type.CommonConstructor().GetMethodBuilder());
		}
		#endregion

		void BeforeStatement()
		{
			if (!reachable)
				throw new InvalidOperationException(Properties.Messages.ErrCodeNotReachable);

			if (cg != null && !chainCalled && !cg.Type.TypeBuilder.IsValueType)
				InvokeBase();
		}

		void DoInvoke(Operand invocation)
		{
			BeforeStatement();

			invocation.EmitGet(this);
			if (invocation.Type != typeof(void))
				il.Emit(OpCodes.Pop);
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

		public void WriteLine(params Operand[] args)
		{
			Invoke(typeof(Console), "WriteLine", args);
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
			il.Emit(OpCodes.Initobj, target.Type);
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

			foreach (Block blk in blocks)
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
					il.Emit(useLeave ? OpCodes.Leave : OpCodes.Br, brkBlock.GetBreakTarget());
					reachable = false;
					return;
				}
			}

			throw new InvalidOperationException(Properties.Messages.ErrInvalidBreak);
		}

		public void Continue()
		{
			BeforeStatement();

			bool useLeave = false;

			foreach (Block blk in blocks)
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
					il.Emit(useLeave ? OpCodes.Leave : OpCodes.Br, cntBlock.GetContinueTarget());
					reachable = false;
					return;
				}
			}

			throw new InvalidOperationException(Properties.Messages.ErrInvalidContinue);
		}

		public void Return()
		{
			if (context.ReturnType != null && context.ReturnType != typeof(void))
				throw new InvalidOperationException(Properties.Messages.ErrMethodMustReturnValue);

			BeforeStatement();

			ExceptionBlock xb = GetAnyTryBlock();

			if (xb == null)
			{
				il.Emit(OpCodes.Ret);
			}
			else if (xb.IsFinally)
			{
				throw new InvalidOperationException(Properties.Messages.ErrInvalidFinallyBranch);
			}
			else
			{
				EnsureReturnVariable();
				il.Emit(OpCodes.Leave, retLabel);
			}

			reachable = false;
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters", Justification = "'Operand' required as type to use provided implicit conversions")]
		public void Return(Operand value)
		{
			if (context.ReturnType == null || context.ReturnType == typeof(void))
				throw new InvalidOperationException(Properties.Messages.ErrVoidMethodReturningValue);

			BeforeStatement();

			EmitGetHelper(value, context.ReturnType, false);

			ExceptionBlock xb = GetAnyTryBlock();

			if (xb == null)
			{
				il.Emit(OpCodes.Ret);
			}
			else if (xb.IsFinally)
			{
				throw new InvalidOperationException(Properties.Messages.ErrInvalidFinallyBranch);
			}
			else
			{
				EnsureReturnVariable();
				il.Emit(OpCodes.Stloc, retVar);
				il.Emit(OpCodes.Leave, retLabel);
			}
			reachable = false;
		}

		public void Throw()
		{
			BeforeStatement();

			il.Emit(OpCodes.Rethrow);
			reachable = false;
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters", Justification = "'Operand' required as type to use provided implicit conversions")]
		public void Throw(Operand exception)
		{
			BeforeStatement();

			EmitGetHelper(exception, typeof(Exception), false);
			il.Emit(OpCodes.Throw);
			reachable = false;
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
			ForeachBlock fb = new ForeachBlock(elementType, expression);
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

			blocks.Pop();
			Begin(new ElseBlock(ifBlk));
		}

		public void Try()
		{
			Begin(new ExceptionBlock());
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
			foreach (Block blk in blocks)
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
			Begin(new SwitchBlock(expression));
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
			if (blocks.Count == 0)
				return null;

			return blocks.Peek();
		}

		Block GetBlockForVariable()
		{
			if (blocks.Count == 0)
				return null;

			Block b = blocks.Peek();
			b.EnsureScope();
			return b;
		}

		void Begin(Block b)
		{
			blocks.Push(b);
			b.g = this;
			b.Begin();
		}

		public void End()
		{
			if (blocks.Count == 0)
				throw new InvalidOperationException(Properties.Messages.ErrNoOpenBlocks);

			blocks.Peek().End();
			blocks.Pop();
		}

		abstract class Block
		{
			bool hasScope;
			internal CodeGen g;

			public void EnsureScope()
			{
				if (!hasScope)
				{
					if (g.context.SupportsScopes)
						g.il.BeginScope();
					hasScope = true;
				}
			}

			protected void EndScope()
			{
				if (hasScope)
				{
					if (g.context.SupportsScopes)
						g.il.EndScope();
					hasScope = false;
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
			Operand condition;

			public IfBlock(Operand condition)
			{
				if (condition.Type != typeof(bool))
					this.condition = condition.IsTrue();
				else
					this.condition = condition;
			}

			Label lbSkip;

			protected override void BeginImpl()
			{
				g.BeforeStatement();

				lbSkip = g.il.DefineLabel();
				condition.EmitBranch(g, BranchSet.Inverse, lbSkip);
			}

			protected override void EndImpl()
			{
				g.il.MarkLabel(lbSkip);
				g.reachable = true;
			}
		}

		class ElseBlock : Block
		{
			IfBlock ifBlk;
			Label lbSkip;
			bool canSkip;

			public ElseBlock(IfBlock ifBlk)
			{
				this.ifBlk = ifBlk;
			}

			protected override void BeginImpl()
			{
				if (canSkip = g.reachable)
				{
					lbSkip = g.il.DefineLabel();
					g.il.Emit(OpCodes.Br, lbSkip);
				}
				ifBlk.End();
			}

			protected override void EndImpl()
			{
				if (canSkip)
				{
					g.il.MarkLabel(lbSkip);
					g.reachable = true;
				}
			}
		}

		class LoopBlock : Block, IBreakable, IContinuable
		{
			IStatement init;
			Operand test;
			IStatement iter;

			public LoopBlock(IStatement init, Operand test, IStatement iter)
			{
				this.init = init;
				this.test = test;
				this.iter = iter;

				if (test.Type != typeof(bool))
					test = test.IsTrue();
			}

			Label lbLoop, lbTest, lbEnd, lbIter;
			bool endUsed = false, iterUsed = false;

			protected override void BeginImpl()
			{
				g.BeforeStatement();

				lbLoop = g.il.DefineLabel();
				lbTest = g.il.DefineLabel();
				if (init != null)
					init.Emit(g);
				g.il.Emit(OpCodes.Br, lbTest);
				g.il.MarkLabel(lbLoop);
			}

			protected override void EndImpl()
			{
				if (iter != null)
				{
					if (iterUsed)
						g.il.MarkLabel(lbIter);
				
					iter.Emit(g);
				}

				g.il.MarkLabel(lbTest);
				test.EmitBranch(g, BranchSet.Normal, lbLoop);

				if (endUsed)
					g.il.MarkLabel(lbEnd);

				g.reachable = true;
			}

			public Label GetBreakTarget()
			{
				if (!endUsed)
				{
					lbEnd = g.il.DefineLabel();
					endUsed = true;
				}
				return lbEnd;
			}

			public Label GetContinueTarget()
			{
				if (iter == null)
					return lbTest;

				if (!iterUsed)
				{
					lbIter = g.il.DefineLabel();
					iterUsed = true;
				}
				return lbIter;
			}
		}

		// TODO: proper implementation, including dispose
		class ForeachBlock : Block, IBreakable, IContinuable
		{
			Type elementType;
			Operand collection;

			public ForeachBlock(Type elementType, Operand collection)
			{
				this.elementType = elementType;
				this.collection = collection;
			}

			Operand enumerator, element;
			Label lbLoop, lbTest, lbEnd;
			bool endUsed = false;

			public Operand Element { get { return element; } }

			protected override void BeginImpl()
			{
				g.BeforeStatement();

				enumerator = g.Local();
				lbLoop = g.il.DefineLabel();
				lbTest = g.il.DefineLabel();

				if (typeof(IEnumerable).IsAssignableFrom(collection.Type))
					collection = collection.Cast(typeof(IEnumerable));

				g.Assign(enumerator, collection.Invoke("GetEnumerator"));
				g.il.Emit(OpCodes.Br, lbTest);
				g.il.MarkLabel(lbLoop);
				element = g.Local(elementType);
				g.Assign(element, enumerator.Property("Current"), true);
			}

			protected override void EndImpl()
			{
				g.il.MarkLabel(lbTest);
				enumerator.Invoke("MoveNext").EmitGet(g);

				g.il.Emit(OpCodes.Brtrue, lbLoop);

				if (endUsed)
					g.il.MarkLabel(lbEnd); 
				
				g.reachable = true;
			}

			public Label GetBreakTarget()
			{
				if (!endUsed)
				{
					lbEnd = g.il.DefineLabel();
					endUsed = true;
				}
				return lbEnd;
			}

			public Label GetContinueTarget()
			{
				return lbTest;
			}
		}

		class ExceptionBlock : Block
		{
			bool endReachable = false;
			bool isFinally = false;

			protected override void BeginImpl()
			{
				g.il.BeginExceptionBlock();
			}

			public void BeginCatchAll()
			{
				EndScope();

				if (g.reachable)
					endReachable = true;
				g.il.BeginCatchBlock(typeof(object));
				g.il.Emit(OpCodes.Pop);
				g.reachable = true;
			}

			public Operand BeginCatch(Type t)
			{
				EndScope();

				if (g.reachable)
					endReachable = true;

				g.il.BeginCatchBlock(t);
				LocalBuilder lb = g.il.DeclareLocal(t);
				g.il.Emit(OpCodes.Stloc, lb);
				g.reachable = true;

				return new _Local(g, lb);
			}

			public void BeginFault()
			{
				EndScope();

				g.il.BeginFaultBlock();
				g.reachable = true;
				isFinally = true;
			}

			public void BeginFinally()
			{
				EndScope();

				g.il.BeginFinallyBlock();
				g.reachable = true;
				isFinally = true;
			}

			protected override void EndImpl()
			{
				g.il.EndExceptionBlock();
				g.reachable = endReachable;
			}

			public bool IsFinally { get { return isFinally; } }
		}

		class SwitchBlock : Block, IBreakable
		{
			static Type[] validTypes = { 
				typeof(sbyte), typeof(byte), typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(char), typeof(string)
			};
			static MethodInfo strCmp = typeof(string).GetMethod("Equals", BindingFlags.Public | BindingFlags.Static,
				null, new Type[] { typeof(string), typeof(string) }, null);

			Operand expression;
			Conversion conv;
			Type govType;
			Label lbDecision;
			Label lbEnd;
			Label lbDefault;
			LocalBuilder exp;
			bool defaultExists = false;
			bool endReachable = false;
			SortedList<IComparable, Label> cases = new SortedList<IComparable, Label>();

			public SwitchBlock(Operand expression)
			{
				this.expression = expression;

				Type exprType = expression.Type;
				if (Array.IndexOf(validTypes, exprType) != -1)
					govType = exprType;
				else if (exprType.IsEnum)
					govType = Enum.GetUnderlyingType(exprType);
				else
				{
					// if a single implicit coversion from expression to one of the valid types exists, it's ok
					foreach (Type t in validTypes)
					{
						Conversion tmp = Conversion.GetImplicit(expression, t, false);
						if (tmp.IsValid)
						{
							if (conv == null)
							{
								conv = tmp;
								govType = t;
							}
							else
								throw new AmbiguousMatchException(Properties.Messages.ErrAmbiguousSwitchExpression);
						}
					}
				}
			}

			protected override void BeginImpl()
			{
				lbDecision = g.il.DefineLabel();
				lbDefault = lbEnd = g.il.DefineLabel();

				expression.EmitGet(g);
				if (conv != null)
					conv.Emit(g, expression.Type, govType);
				exp = g.il.DeclareLocal(govType);
				g.il.Emit(OpCodes.Stloc, exp);
				g.il.Emit(OpCodes.Br, lbDecision);
				g.reachable = false;
			}

			public void Case(IConvertible value)
			{
				bool duplicate;

				// make sure the value is of the governing type
				IComparable val = value == null ? null : (IComparable)value.ToType(govType, System.Globalization.CultureInfo.InvariantCulture);

				if (value == null)
					duplicate = defaultExists;
				else
					duplicate = cases.ContainsKey(val);

				if (duplicate)
					throw new InvalidOperationException(Properties.Messages.ErrDuplicateCase);

				if (g.reachable)
					g.il.Emit(OpCodes.Br, lbEnd);

				EndScope();
				Label lb = g.il.DefineLabel();
				g.il.MarkLabel(lb);
				if (value == null)
				{
					defaultExists = true;
					lbDefault = lb;
				}
				else
				{
					cases[val] = lb;
				}
				g.reachable = true;
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
						g.il.Emit(OpCodes.Beq, labels[0]);
						break;
					default:
						g.il.Emit(OpCodes.Sub);
						g.il.Emit(OpCodes.Switch, labels.ToArray());
						break;
				}
			}

			void EmitValue(IConvertible val)
			{
				switch (val.GetTypeCode())
				{
					case TypeCode.UInt64:
						g.EmitI8Helper(unchecked((long)val.ToUInt64(null)), false);
						break;
					case TypeCode.Int64:
						g.EmitI8Helper(val.ToInt64(null), true);
						break;
					case TypeCode.UInt32:
						g.EmitI4Helper(unchecked((int)val.ToUInt64(null)));
						break;
					default:
						g.EmitI4Helper(val.ToInt32(null));
						break;
				}
			}

			protected override void EndImpl()
			{
				if (g.reachable)
				{
					g.il.Emit(OpCodes.Br, lbEnd);
					endReachable = true;
				}

				EndScope();
				g.il.MarkLabel(lbDecision);

				if (govType == typeof(string))
				{
					foreach (KeyValuePair<IComparable, Label> kvp in cases)
					{
						g.il.Emit(OpCodes.Ldloc, exp);
						g.il.Emit(OpCodes.Ldstr, kvp.Key.ToString());
						g.il.Emit(OpCodes.Call, strCmp);
						g.il.Emit(OpCodes.Brtrue, kvp.Value);
					}
				}
				else
				{
					bool first = true;
					IConvertible prev = null;
					List<Label> labels = new List<Label>();

					foreach (KeyValuePair<IComparable, Label> kvp in cases)
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
									labels.Add(lbDefault);
						}

						if (first)
						{
							g.il.Emit(OpCodes.Ldloc, exp);
							EmitValue(val);
							first = false;
						}

						labels.Add(kvp.Value);
						prev = val;
					}

					Finish(labels);
				}
				if (lbDefault != lbEnd)
					g.il.Emit(OpCodes.Br, lbDefault);
				g.il.MarkLabel(lbEnd);
				g.reachable = endReachable;
			}

			public Label GetBreakTarget()
			{
				endReachable = true;
				return lbEnd;
			}
		}
	}
}
