//     This file is part of PapyrusDotNet.
// 
//     PapyrusDotNet is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General Public License as published by
//     the Free Software Foundation, either version 3 of the License, or
//     (at your option) any later version.
// 
//     PapyrusDotNet is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU General Public License for more details.
// 
//     You should have received a copy of the GNU General Public License
//     along with PapyrusDotNet.  If not, see <http://www.gnu.org/licenses/>.
//  
//     Copyright 2016, Karl Patrik Johansson, zerratar@gmail.com

#region

using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;
using PapyrusDotNet.Common;
using PapyrusDotNet.Common.Interfaces;
using PapyrusDotNet.Common.Utilities;
using PapyrusDotNet.Converters.Clr2Papyrus.Enums;
using PapyrusDotNet.Converters.Clr2Papyrus.Exceptions;
using PapyrusDotNet.Converters.Clr2Papyrus.Interfaces;
using PapyrusDotNet.PapyrusAssembly;
using PapyrusDotNet.PapyrusAssembly.Extensions;

#endregion

namespace PapyrusDotNet.Converters.Clr2Papyrus.Implementations.Processors
{
	public interface ICallProcessor : ISubInstructionProcessor
	{
		PapyrusInstruction CreatePapyrusCallInstruction(
			IClrInstructionProcessor mainInstructionProcessor,
			PapyrusOpCodes callOpCode, MethodReference methodRef,
			string callerLocation, string destinationVariable, List<object> parameters,
			out List<PapyrusInstruction> structGets, string methodName = null);
	}

	public class CallProcessor : ICallProcessor
	{
		private readonly IValueTypeConverter valueTypeConverter;
		private Instruction currentInstruction;
		private IReadOnlyCollection<PapyrusAssemblyDefinition> papyrusAssemblyCollection;

		/// <summary>
		/// Initializes a new instance of the <see cref="CallProcessor" /> class.
		/// </summary>
		/// <param name="valueTypeConverter">The value type converter.</param>
		public CallProcessor(IValueTypeConverter valueTypeConverter)
		{
			this.valueTypeConverter = valueTypeConverter ?? new PapyrusValueTypeConverter();
		}

		/// <summary>
		/// Parses the instruction.
		/// </summary>
		/// <param name="mainProcessor">The main instruction processor.</param>
		/// <param name="asmCollection">The papyrus assembly collection.</param>
		/// <param name="instruction">The instruction.</param>
		/// <param name="targetMethod">The target method.</param>
		/// <param name="targetType">Type of the target.</param>
		/// <returns></returns>
		/// <exception cref="StackUnderflowException"></exception>
		public IEnumerable<PapyrusInstruction> Process(
			IClrInstructionProcessor mainProcessor,
			IReadOnlyCollection<PapyrusAssemblyDefinition> asmCollection,
			Instruction instruction,
			MethodDefinition targetMethod,
			TypeDefinition targetType)
		{
			bool isStructAccess;
			var structGets = new List<PapyrusInstruction>();
			this.papyrusAssemblyCollection = asmCollection;
			var processInstruction = new List<PapyrusInstruction>();
			var stack = mainProcessor.EvaluationStack;
			currentInstruction = instruction;
			var methodRef = instruction.Operand as MethodReference;
			if (methodRef != null)
			{
				if (methodRef.FullName.ToLower().Contains("system.void") &&
					methodRef.FullName.ToLower().Contains(".ctor"))
				{
					return new PapyrusInstruction[0];
				}

				// What we need:
				// 1. Call Location (The name of the type that has this method)
				// 2. Method Name (The name of the method, duh)
				// 3. Method Parameters (The parameters that we need to pass to the method)
				// 4. Destination Variable (The variable needed to store the return value of the method)
				//      - Destination Variable must always exist, the difference between a function returning a object
				//        and a void, is that we "assign" the destination to a ::nonevar temp variable of type None (void)

				var name = methodRef.FullName;
				var itemsToPop = instruction.OpCode.StackBehaviourPop == StackBehaviour.Varpop
					? methodRef.Parameters.Count
					: Utility.GetStackPopCount(instruction.OpCode.StackBehaviourPop);

				if (stack.Count < itemsToPop)
				{
					if (mainProcessor.PapyrusCompilerOptions == PapyrusCompilerOptions.Strict)
					{
						throw new StackUnderflowException(targetMethod, instruction);
					}
					return processInstruction;
				}

				// Check if the current invoked Method is inside the same instance of this type
				// by checking if it is using "this.Method(params);" << "this."...
				var isThisCall = false;
				var isStaticCall = false;
				var callerLocation = "";
				var parameters = new List<object>();

				if (methodRef.HasThis)
				{
					callerLocation = "self";
					isThisCall = true;
				}

				for (var paramIndex = 0; paramIndex < itemsToPop; paramIndex++)
				{
					var parameter = stack.Pop();
					if (parameter.IsThis && stack.Count > methodRef.Parameters.Count
						|| methodRef.CallingConvention == MethodCallingConvention.ThisCall)
					{
						isThisCall = true;
						callerLocation = "self"; // Location: 'self' is the same as 'this'
					}
					parameters.Insert(0, parameter);
				}

				var methodDefinition = mainProcessor.TryResolveMethodReference(methodRef);
				if (methodDefinition != null)
				{
					isStaticCall = methodDefinition.IsStatic;
				}

				if (methodDefinition != null && methodDefinition.IsConstructor) return processInstruction;

				if (methodDefinition == null)
				{
					isStaticCall = name.Contains("::");
				}

				if (isStaticCall)
				{
					callerLocation = name.Split("::")[0];
				}

				if (callerLocation.Contains("."))
				{
					callerLocation = callerLocation.Split('.').LastOrDefault();
				}

				if (methodRef.Name.ToLower().Contains("concat"))
				{
					processInstruction.AddRange(mainProcessor.ProcessStringConcat(instruction, methodRef,
						parameters));
				}
				else if (methodRef.Name.ToLower().Contains("op_equal") ||
						 methodRef.Name.ToLower().Contains("op_inequal"))
				{
					// TODO: Add Equality comparison

					mainProcessor.InvertedBranch = methodRef.Name.ToLower().Contains("op_inequal");

					if (!InstructionHelper.IsStore(instruction.Next.OpCode.Code))
					{
						mainProcessor.SkipToOffset = instruction.Next.Offset;
						return processInstruction;
					}
					// EvaluationStack.Push(new EvaluationStackItem { IsMethodCall = true, Value = methodRef, TypeName = methodRef.ReturnType.FullName });

					processInstruction.AddRange(mainProcessor.ProcessConditionalInstruction(instruction,
						Code.Ceq));
				}
				else
				{
					if (methodRef.Name.ToLower().StartsWith("get_") || methodRef.Name.ToLower().StartsWith("set_"))
					{
						processInstruction.AddRange(ProcessPropertyAccess(mainProcessor, instruction, methodRef, methodDefinition,
							parameters));

						return processInstruction;
					}


					if (isStaticCall)
					{
						var destinationVariable = mainProcessor.GetTargetVariable(instruction, methodRef,
							out isStructAccess);
						{
							processInstruction.Add(CreatePapyrusCallInstruction(mainProcessor, PapyrusOpCodes.Callstatic, methodRef,
								callerLocation,
								destinationVariable, parameters, out structGets));
							processInstruction.InsertRange(processInstruction.Count - 1, structGets);
							return processInstruction;
						}
					}
					if (isThisCall)
					{
						var isDelegateInvoke = false;
						if (stack.Count > 0)
						{
							var next = stack.Peek().Value;
							var varRef = next as PapyrusVariableReference;
							if (varRef != null && varRef.IsDelegateReference)
							{
								isDelegateInvoke = true;
							}
						}

						var destinationVariable = mainProcessor.GetTargetVariable(instruction, methodRef,
							out isStructAccess);

						if (isDelegateInvoke)
						{
							var targetDelegate = stack.Pop().Value as PapyrusVariableReference;
							var targetDelegateMethodName = targetDelegate.DelegateInvokeReference;

							// In case we are invoking a delegate from inside a delegate then we need to find the 
							// reference to the previous delegate method and use that one instead.
							var target = InstructionHelper.FindPreviousInstruction(instruction,
								InstructionHelper.IsLoadMethodRef);
							if (target != null && target.Operand is MethodReference)
							{
								methodRef = target.Operand as MethodReference;
								targetDelegateMethodName = methodRef.Name;
							}

							processInstruction.Add(CreatePapyrusCallInstruction(mainProcessor, PapyrusOpCodes.Callmethod, methodRef,
								callerLocation, destinationVariable, parameters, out structGets,
								targetDelegateMethodName));
							processInstruction.InsertRange(processInstruction.Count - 1, structGets);
						}
						else
						{
							if (isStructAccess)
							{
								var structRef =
									stack.Pop().Value as PapyrusStructFieldReference;
								if (structRef != null)
								{
									// (Call and Assign Temp then do StructSet using Temp)

									// Call and Assign return value to temp
									processInstruction.Add(CreatePapyrusCallInstruction(mainProcessor, PapyrusOpCodes.Callmethod,
										methodRef,
										callerLocation,
										destinationVariable, parameters, out structGets));

									// StructSet
									processInstruction.Add(
										mainProcessor.CreatePapyrusInstruction(PapyrusOpCodes.StructSet,
											structRef.StructSource, structRef.StructVariable,
											mainProcessor.CreateVariableReferenceFromName(destinationVariable)));

									// Skip next instruction as it should be stfld and we already store the field here.
									mainProcessor.SkipNextInstruction = true;
								}
							}
							else
							{
								processInstruction.Add(CreatePapyrusCallInstruction(mainProcessor, PapyrusOpCodes.Callmethod, methodRef,
									callerLocation,
									destinationVariable, parameters, out structGets));
								processInstruction.InsertRange(processInstruction.Count - 1, structGets);
							}
						}
						return processInstruction;
					}
				}
			}
			return processInstruction;
		}

		private IEnumerable<PapyrusInstruction> ProcessPropertyAccess(IClrInstructionProcessor mainInstructionProcessor, Instruction instruction, MethodReference methodRef,
			MethodDefinition methodDefinition,
			List<object> parameters)
		{
			bool isStructAccess;
			var instructions = new List<PapyrusInstruction>();

			if (methodRef is MethodDefinition)
			{
				methodDefinition = methodRef as MethodDefinition;
			}

			// If the property access is from outside the same class (Property exists outside the calling class)
			if (methodDefinition == null && methodRef != null)
			{
				var type = methodRef.DeclaringType;
				var methodType = methodRef.Name.Remove(3);
				var targetPropertyName = methodRef.Name.Substring(4);

				if (methodType == "set")
				{
					var param = parameters;
					var stack = mainInstructionProcessor.EvaluationStack;
					var locationVariable = stack.Pop().Value;
					instructions.Add(mainInstructionProcessor.CreatePapyrusInstruction(PapyrusOpCodes.PropSet,
						mainInstructionProcessor.CreateVariableReferenceFromName(targetPropertyName),
						locationVariable,
						param.First()
						));
				}
				else if (methodType == "get")
				{
					var stack = mainInstructionProcessor.EvaluationStack;
					var locationVariable = stack.Pop().Value;
					var destinationVariable = mainInstructionProcessor.GetTargetVariable(instruction, methodRef,
						out isStructAccess);

					if (isStructAccess)
					{
						var structRef =
							mainInstructionProcessor.EvaluationStack.Pop().Value as PapyrusStructFieldReference;
						if (structRef != null)
						{
							// (Get Property Value and Assign Temp then do StructSet using Temp)
							instructions.Add(mainInstructionProcessor.CreatePapyrusInstruction(PapyrusOpCodes.PropGet,
								mainInstructionProcessor.CreateVariableReferenceFromName(targetPropertyName),
								locationVariable,
								mainInstructionProcessor.CreateVariableReferenceFromName(destinationVariable)
								));

							// StructSet
							instructions.Add(mainInstructionProcessor.CreatePapyrusInstruction(PapyrusOpCodes.StructSet,
								structRef.StructSource, structRef.StructVariable,
								mainInstructionProcessor.CreateVariableReferenceFromName(destinationVariable)));

							// Skip next instruction as it should be stfld and we already store the field here.
							mainInstructionProcessor.SkipNextInstruction = true;
						}
					}
					else
					{
						instructions.Add(mainInstructionProcessor.CreatePapyrusInstruction(PapyrusOpCodes.PropGet,
							mainInstructionProcessor.CreateVariableReferenceFromName(targetPropertyName),
							locationVariable,
							mainInstructionProcessor.CreateVariableReferenceFromName(destinationVariable)
							));
					}
				}
				else
				{
					throw new InvalidPropertyAccessException();
				}
				return instructions;
			}

			// Property Access within same class (Property exists within the same class)
			if (methodDefinition != null)
			{
				var targetPropertyName = methodRef.Name.Substring(4);

				var matchingProperty = mainInstructionProcessor.PapyrusType.Properties.FirstOrDefault(
					p => (p.SetMethod != null && p.SetMethod.Name.Value.ToLower().Equals(methodRef.Name.ToLower())) ||
						 (p.GetMethod != null && p.GetMethod.Name.Value.ToLower().Equals(methodRef.Name.ToLower())) ||
						 p.Name.Value.ToLower() == targetPropertyName.ToLower());
				if (matchingProperty != null)
				{
					if (methodDefinition.IsSetter)
					{
						var param = parameters;
						var firstParam = param.First();
						PapyrusStructFieldReference structRef = null;
						var eva1 = firstParam as PapyrusStructFieldReference;
						var eval = firstParam as EvaluationStackItem;
						if (eval != null)
						{
							structRef = eval.Value as PapyrusStructFieldReference;
						}
						if (eva1 != null)
							structRef = eva1;

						if (structRef != null)
						{
							// Create Temp Var

							// StructGet -> TempVar
							// PropSet TempVar

							var structSource = structRef.StructSource as PapyrusFieldDefinition;
							var structField = structRef.StructVariable;

							var fieldType = GetStructFieldType(papyrusAssemblyCollection, structSource, structField);

							// 1. Create Temp Var
							var tempVar = mainInstructionProcessor.GetTargetVariable(instruction, null,
								out isStructAccess,
								fieldType, true);

							// 2. StructGet -> tempVar
							// 3. Assign var <- tempVar
							instructions.Add(mainInstructionProcessor.CreatePapyrusInstruction(
								PapyrusOpCodes.StructGet,
								mainInstructionProcessor.CreateVariableReferenceFromName(tempVar), structSource,
								structField));

							instructions.Add(mainInstructionProcessor.CreatePapyrusInstruction(PapyrusOpCodes.PropSet,
								mainInstructionProcessor.CreateVariableReferenceFromName(matchingProperty.Name.Value),
								mainInstructionProcessor.CreateVariableReferenceFromName("self"),
								mainInstructionProcessor.CreateVariableReferenceFromName(tempVar)
								));
						}
						else
						{
							instructions.Add(mainInstructionProcessor.CreatePapyrusInstruction(PapyrusOpCodes.PropSet,
								mainInstructionProcessor.CreateVariableReferenceFromName(matchingProperty.Name.Value),
								mainInstructionProcessor.CreateVariableReferenceFromName("self"),
								firstParam
								));
						}
					}
					else if (methodDefinition.IsGetter)
					{
						var destinationVariable = mainInstructionProcessor.GetTargetVariable(instruction, methodRef,
							out isStructAccess);
						var locationVariable = mainInstructionProcessor.CreateVariableReferenceFromName("self");
						var targetProperty =
							mainInstructionProcessor.CreateVariableReferenceFromName(matchingProperty.Name.Value);
						if (isStructAccess)
						{
							var structRef =
								mainInstructionProcessor.EvaluationStack.Pop().Value as PapyrusStructFieldReference;
							if (structRef != null)
							{
								// (Get Property Value and Assign Temp then do StructSet using Temp)
								instructions.Add(
									mainInstructionProcessor.CreatePapyrusInstruction(PapyrusOpCodes.PropGet,
										targetProperty,
										locationVariable,
										mainInstructionProcessor.CreateVariableReferenceFromName(destinationVariable)
										));

								// StructSet
								instructions.Add(
									mainInstructionProcessor.CreatePapyrusInstruction(PapyrusOpCodes.StructSet,
										structRef.StructSource, structRef.StructVariable,
										mainInstructionProcessor.CreateVariableReferenceFromName(destinationVariable)));

								// Skip next instruction as it should be stfld and we already store the field here.
								mainInstructionProcessor.SkipNextInstruction = true;
							}
						}
						else
						{
							var dest = mainInstructionProcessor.CreateVariableReferenceFromName(destinationVariable);
							instructions.Add(mainInstructionProcessor.CreatePapyrusInstruction(PapyrusOpCodes.PropGet,
								targetProperty,
								locationVariable,
								dest
								));

							if (InstructionHelper.NextInstructionIs(instruction, InstructionHelper.IsConditional))
							{
								mainInstructionProcessor.EvaluationStack.Push(new EvaluationStackItem
								{
									Value = dest,
									TypeName = Utility.GetPapyrusReturnType(methodRef.ReturnType)
								});
							}
						}
					}
				}
			}
			return instructions;
		}

		/// <summary>
		/// Creates a papyrus call instruction.
		/// </summary>
		/// <param name="mainInstructionProcessor">The main instruction processor.</param>
		/// <param name="callOpCode">The call op code.</param>
		/// <param name="methodRef">The method reference.</param>
		/// <param name="callerLocation">The caller location.</param>
		/// <param name="destinationVariable">The destination variable.</param>
		/// <param name="parameters">The parameters.</param>
		/// <param name="structGets">The structure gets.</param>
		/// <param name="methodName">Name of the method.</param>
		/// <returns></returns>
		public PapyrusInstruction CreatePapyrusCallInstruction(
			IClrInstructionProcessor mainInstructionProcessor,
			PapyrusOpCodes callOpCode, MethodReference methodRef,
			string callerLocation, string destinationVariable, List<object> parameters,
			out List<PapyrusInstruction> structGets, string methodName = null)
		{
			structGets = new List<PapyrusInstruction>();

			var inst = new PapyrusInstruction { OpCode = callOpCode };

			var param = parameters.ToArray();
			for (var index = 0; index < param.Length; index++)
			{
				var p = param[index];
				PapyrusStructFieldReference structRef = null;
				var evalItem = p as EvaluationStackItem;

				if (evalItem != null)
					structRef = evalItem.Value as PapyrusStructFieldReference;
				if (structRef == null)
					structRef = p as PapyrusStructFieldReference;
				if (structRef != null)
				{
					var structSource = structRef.StructSource as PapyrusFieldDefinition;
					var structField = structRef.StructVariable;

					var fieldType = GetStructFieldType(papyrusAssemblyCollection, structSource, structField);

					// 1. Create Temp Var
					bool isStructAccess;
					var tempVar = mainInstructionProcessor.GetTargetVariable(currentInstruction, null,
						out isStructAccess,
						fieldType, true);

					param[index] = mainInstructionProcessor.CreateVariableReferenceFromName(tempVar);

					// 2. StructGet -> tempVar
					// 3. Assign var <- tempVar
					structGets.Add(mainInstructionProcessor.CreatePapyrusInstruction(
						PapyrusOpCodes.StructGet,
						mainInstructionProcessor.CreateVariableReferenceFromName(tempVar), structSource,
						structField));
				}
			}

			methodName = methodName ?? methodRef.Name;

			if (callOpCode == PapyrusOpCodes.Callstatic)
			{
				inst.Arguments.AddRange(mainInstructionProcessor.ParsePapyrusParameters(new object[]
				{
					mainInstructionProcessor.CreateVariableReference(PapyrusPrimitiveType.Reference, callerLocation),
					mainInstructionProcessor.CreateVariableReference(PapyrusPrimitiveType.Reference, methodName),
					mainInstructionProcessor.CreateVariableReference(PapyrusPrimitiveType.Reference, destinationVariable)
				}));
			}
			else
			{
				inst.Arguments.AddRange(mainInstructionProcessor.ParsePapyrusParameters(new object[]
				{
					mainInstructionProcessor.CreateVariableReference(PapyrusPrimitiveType.Reference, methodName),
					mainInstructionProcessor.CreateVariableReference(PapyrusPrimitiveType.Reference, callerLocation),
					mainInstructionProcessor.CreateVariableReference(PapyrusPrimitiveType.Reference, destinationVariable)
				}));
			}
			inst.OperandArguments.AddRange(EnsureParameterTypes(mainInstructionProcessor, methodRef.Parameters,
				mainInstructionProcessor.ParsePapyrusParameters(param)));
			inst.Operand = methodRef;
			return inst;
		}

		/// <summary>
		/// Ensures the parameter types.
		/// </summary>
		/// <param name="mainInstructionProcessor">The main instruction processor.</param>
		/// <param name="parameters">The parameters.</param>
		/// <param name="papyrusParams">The papyrus parameters.</param>
		/// <returns></returns>
		public IEnumerable<PapyrusVariableReference> EnsureParameterTypes(IClrInstructionProcessor mainInstructionProcessor, Collection<ParameterDefinition> parameters,
			List<PapyrusVariableReference> papyrusParams)
		{
			var varRefs = new List<PapyrusVariableReference>();
			var i = 0;
			foreach (var p in parameters)
			{
				var papyrusReturnType = Utility.GetPapyrusReturnType(p.ParameterType);
				if (p.ParameterType.IsValueType
					&& Utility.PapyrusValueTypeToString(papyrusParams[i].Type) != papyrusReturnType
					&& papyrusParams[i].Type != PapyrusPrimitiveType.Reference)
				{
					papyrusParams[i].TypeName = papyrusReturnType.Ref(mainInstructionProcessor.PapyrusAssembly);
					papyrusParams[i].Type = Utility.GetPrimitiveTypeFromType(p.ParameterType);
					papyrusParams[i].Value = valueTypeConverter.Convert(papyrusReturnType, papyrusParams[i].Value);
					varRefs.Add(papyrusParams[i]);
				}
				else
				{
					varRefs.Add(papyrusParams[i]);
				}
				i++;
			}

			return varRefs;
		}

		private string GetStructFieldType(IReadOnlyCollection<PapyrusAssemblyDefinition> papyrusAssemblyCollection,
			PapyrusFieldDefinition structSource, PapyrusVariableReference structField)
		{
			foreach (var a in papyrusAssemblyCollection)
			{
				foreach (var t in a.Types)
				{
					foreach (var s in t.NestedTypes)
					{
						var name = structSource.TypeName.Split('#').LastOrDefault();

						if (s.Name.Value.ToLower() == name.ToLower())
						{
							var targetField = s.Fields.FirstOrDefault(f => f.Name.Value == "::" + structField.Name.Value);
							if (targetField != null)
							{
								return targetField.TypeName;
							}
						}
					}
				}
			}
			return "none";
		}
	}
}