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
using System.Reflection;
using System.Reflection.Emit;

namespace TriAxis.RunSharp
{
	enum Better : byte
	{
		Undefined, Left, Right, Neither
	}

	class ApplicableFunction
	{
		IMemberInfo method;
		Type[] methodSignature, appliedSignature, paramsSignature;
		Conversion[] conversions;

		internal ApplicableFunction(IMemberInfo method, Type[] methodSignature,
			Type[] appliedSignature, Type[] paramsSignature,
			Conversion[] conversions)
		{
			this.method = method;
			this.methodSignature = methodSignature;
			this.appliedSignature = appliedSignature;
			this.paramsSignature = paramsSignature;
			this.conversions = conversions;
		}
		public IMemberInfo Method { get { return method; } }
		public bool IsExpanded { get { return methodSignature != appliedSignature; } }

		public bool SignatureEquals(ApplicableFunction other)
		{
			return ArrayUtils.Equals(appliedSignature, other.appliedSignature);
		}

		public void EmitArgs(CodeGen g, Operand[] args)
		{
			if (args.Length != appliedSignature.Length)
				throw new InvalidOperationException();

			if (IsExpanded)
			{
				int fixedCount = methodSignature.Length - 1;
				Type expType = methodSignature[fixedCount].GetElementType();

				for (int i = 0; i < fixedCount; i++)
					EmitArg(g, i, args[i]);

				int arrayLen = args.Length - methodSignature.Length - 1;
				g.EmitI4Helper(arrayLen);
				g.IL.Emit(OpCodes.Newarr, expType);
				OpCode stelemCode = CodeGen.GetStelemOpCode(expType);
				for (int i = 0; i < arrayLen; i++)
				{
					g.IL.Emit(OpCodes.Dup);
					g.EmitI4Helper(i);
					if (stelemCode == OpCodes.Stobj)
						g.IL.Emit(OpCodes.Ldelema, expType);
					EmitArg(g, fixedCount + i, args[fixedCount + i]);
					if (stelemCode == OpCodes.Stobj)
						g.IL.Emit(OpCodes.Stobj, expType);
					else
						g.IL.Emit(stelemCode);
				}
			}
			else
			{
				for (int i = 0; i < args.Length; i++)
					EmitArg(g, i, args[i]);
			}
		}

		void EmitArg(CodeGen g, int index, Operand arg)
		{
			if (appliedSignature[index].IsByRef)
			{
				arg.EmitAddressOf(g);
				return;
			}

			if ((object)arg == null)
				g.IL.Emit(OpCodes.Ldnull);
			else
				arg.EmitGet(g);

			conversions[index].Emit(g, paramsSignature[index], appliedSignature[index]);
		}

		public static Better GetBetterCandidate(ApplicableFunction left, ApplicableFunction right)
		{
			if (!ArrayUtils.Equals(left.paramsSignature, right.paramsSignature))
				throw new InvalidOperationException();

			int leftBetter = 0, rightBetter = 0;

			for (int i = 0; i < left.appliedSignature.Length; i++)
			{
				Better better = GetBetterConversion(left.paramsSignature[i], left.appliedSignature[i], right.appliedSignature[i]);
				if (better == Better.Left)
					leftBetter++;
				else if (better == Better.Right)
					rightBetter++;

				if (leftBetter != 0 && rightBetter != 0)
					return 0;
			}

			if (leftBetter > 0)
				return Better.Left;
			if (rightBetter > 0)
				return Better.Right;
			return Better.Neither;
		}

		static Better GetBetterConversion(Type from, Type left, Type right)
		{
			if (left == right)
				return Better.Neither;
			if (from == left)
				return Better.Left;
			if (from == right)
				return Better.Right;

			Conversion lrConv = Conversion.GetImplicit(left, right, false);
			Conversion rlConv = Conversion.GetImplicit(right, left, false);

			if (lrConv.IsValid && !rlConv.IsValid)
				return Better.Left;
			if (rlConv.IsValid && !lrConv.IsValid)
				return Better.Right;

			if (BetterSign(left, right))
				return Better.Left;
			if (BetterSign(right, left))
				return Better.Right;

			return Better.Neither;
		}

		static bool BetterSign(Type better, Type than)
		{
			if (better == typeof(sbyte))
				return than == typeof(byte) || than == typeof(ushort) || than == typeof(uint) || than == typeof(ulong);
			if (better == typeof(short))
				return than == typeof(ushort) || than == typeof(uint) || than == typeof(ulong);
			if (better == typeof(int))
				return than == typeof(uint) || than == typeof(ulong);
			if (better == typeof(long))
				return than == typeof(ulong);

			return false;
		}
	}

	static class OverloadResolver
	{
		public static ApplicableFunction Resolve(IEnumerable<IMemberInfo> candidates, params Operand[] args)
		{
			List<ApplicableFunction> applicable = FindApplicable(candidates, args);

			if (applicable == null)
				return null;

			ApplicableFunction best = FindBest(applicable);

			if (best == null)
				throw new AmbiguousMatchException(Properties.Messages.ErrAmbiguousBinding);

			return best;
		}

		public static void RemoveExpanded(List<ApplicableFunction> candidates)
		{
			// remove the expanded candidates with signatures matching
			// the normal candidates
			for (int i = 0; i < candidates.Count; i++)
			{
				if (candidates[i].IsExpanded)
				{
					for (int j = 0; j < candidates.Count; j++)
					{
						if (candidates[j].IsExpanded)
							continue;

						if (candidates[j].SignatureEquals(candidates[i]))
						{
							candidates.RemoveAt(i--);
							break;
						}
					}
				}
			}
		}

		public static List<ApplicableFunction> FindApplicable(IEnumerable<IMemberInfo> candidates, params Operand[] args)
		{
			List<ApplicableFunction> valid = null;
			bool expandedCandidates = false;

			if (!FindApplicable(ref valid, ref expandedCandidates, candidates, args))
				return null;

			if (expandedCandidates)
				RemoveExpanded(valid);

			return valid;
		}

		public static bool FindApplicable(ref List<ApplicableFunction> valid, ref bool expandedCandidates, IEnumerable<IMemberInfo> candidates, params Operand[] args)
		{
			if (candidates == null)
				return false;

			bool found = false;

			foreach (IMemberInfo candidate in candidates)
			{
				ApplicableFunction vc = ValidateCandidate(candidate, args);

				if (vc != null)
				{
					found = true;

					if (vc.IsExpanded)
						expandedCandidates = true;

					if (valid == null)
						valid = new List<ApplicableFunction>();
					valid.Add(vc);
				}
			}

			return found;
		}

		public static ApplicableFunction ValidateCandidate(IMemberInfo candidate, Operand[] args)
		{
			Type[] cTypes = candidate.ParameterTypes;

			if (cTypes.Length == args.Length)
			{
				Conversion[] conversions = new Conversion[args.Length];

				for (int i = 0; i < cTypes.Length; i++)
				{
					conversions[i] = Conversion.GetImplicit(args[i], cTypes[i], false);
					if (!conversions[i].IsValid)
						return null;
				}

				return new ApplicableFunction(candidate, cTypes, cTypes, Operand.GetTypes(args), conversions);
			}

			if (candidate.IsParameterArray && args.Length >= cTypes.Length - 1)
			{
				Type[] expandedTypes = new Type[args.Length];
				Array.Copy(cTypes, expandedTypes, cTypes.Length - 1);
				Type varType = cTypes[cTypes.Length - 1].GetElementType();

				for (int i = cTypes.Length - 1; i < expandedTypes.Length; i++)
					expandedTypes[i] = varType;

				Conversion[] conversions = new Conversion[args.Length];

				for (int i = 0; i < expandedTypes.Length; i++)
				{
					conversions[i] = Conversion.GetImplicit(args[i], expandedTypes[i], false);
					if (!conversions[i].IsValid)
						return null;
				}

				return new ApplicableFunction(candidate, cTypes, expandedTypes, Operand.GetTypes(args), conversions);
			}

			return null;
		}

		static Better Invert(Better b)
		{
			switch (b)
			{
				case Better.Left: return Better.Right;
				case Better.Right: return Better.Left;
				default: return b;
			}
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1814:PreferJaggedArraysOverMultidimensional", MessageId = "Body", Justification = "Jagged array would have worse performance (many allocations)")]
		public static ApplicableFunction FindBest(List<ApplicableFunction> candidates)
		{
			if (candidates == null || candidates.Count == 0)
				return null;

			if (candidates.Count == 1)
				return candidates[0];

			ApplicableFunction best = null;
			Better[,] betterMap = new Better[candidates.Count, candidates.Count];

			for (int i = 0; i < candidates.Count; i++)
			{
				bool isBest = true;

				for (int j = 0; j < candidates.Count; j++)
				{
					if (i == j)
						continue;

					Better better = betterMap[i, j];
					if (better == Better.Undefined)
					{
						better = ApplicableFunction.GetBetterCandidate(candidates[i], candidates[j]);
						betterMap[i, j] = better;
						betterMap[j, i] = Invert(better);
					}

					if (better != Better.Left)
					{
						isBest = false;
						break;
					}
				}

				if (isBest)
				{
					if (best == null)
						best = candidates[i];
					else
						return null;
				}
			}

			return best;
		}
	}
}
