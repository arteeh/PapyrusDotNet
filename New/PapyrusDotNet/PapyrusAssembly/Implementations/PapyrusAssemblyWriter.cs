﻿//     This file is part of PapyrusDotNet.
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

using PapyrusDotNet.PapyrusAssembly.Interfaces;
using PapyrusDotNet.PapyrusAssembly.IO;

#endregion

namespace PapyrusDotNet.PapyrusAssembly.Implementations
{
	internal class PapyrusAssemblyWriter : IPapyrusAssemblyWriter, IDisposable
	{
		private readonly MemoryStream outputStream;
		private readonly PexWriter pexWriter;
		private bool isDisposed;

		public PapyrusAssemblyWriter(PapyrusAssemblyDefinition assembly)
		{
			if (assembly == null) throw new ArgumentNullException(nameof(assembly));

			Assembly = assembly;
			outputStream = new MemoryStream();
			pexWriter = new PexWriter(assembly, outputStream);
		}

		public PapyrusAssemblyDefinition Assembly { get; set; }

		public void Dispose()
		{
			Dispose(true);
		}

		public void Write(string outputFile)
		{
			if (outputFile == null) throw new ArgumentNullException(nameof(outputFile));

			WriteHeader();

			WriteStringTable();

			pexWriter.Write(Assembly.HasDebugInfo);

			if (Assembly.HasDebugInfo)
			{
				WriteDebugInfo();
			}

			WriteHeaderUserflags();

			WriteTypeDefinitions();

			var dir = Path.GetDirectoryName(outputFile);
			if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);

			File.WriteAllBytes(outputFile, outputStream.ToArray());
		}

		private void WriteTypeDefinitions()
		{
			var asm = Assembly;
			var types = asm.Types;
			pexWriter.Write((short)types.Count);
			foreach (var t in types)
			{
				WriteTypeInfo(t);

				if (Assembly.VersionTarget == PapyrusVersionTargets.Fallout4)
				{
					WriteStructs(t);
				}

				WriteFields(t);

				WriteProperties(t);

				WriteStates(t);
			}
		}

		private void WriteStates(PapyrusTypeDefinition papyrusTypeDefinition)
		{
			var states = papyrusTypeDefinition.States;
			pexWriter.Write((short)states.Count);
			foreach (var state in states)
				WriteState(state);
		}

		private void WriteState(PapyrusStateDefinition state)
		{
			pexWriter.Write(state.Name);
			pexWriter.Write((short)state.Methods.Count);
			foreach (var method in state.Methods)
			{
				pexWriter.Write(method.Name);
				WriteMethod(method);
			}
		}

		private void WriteMethod(PapyrusMethodDefinition method)
		{
			pexWriter.Write(method.ReturnTypeName);
			pexWriter.Write(method.Documentation);
			pexWriter.Write(method.UserFlags);
			pexWriter.Write(method.Flags);

			pexWriter.Write((short)method.Parameters.Count);
			foreach (var param in method.Parameters)
				WriteParameter(param);

			pexWriter.Write((short)method.Body.Variables.Count);
			foreach (var variable in method.Body.Variables)
				WriteVariable(variable);

			pexWriter.Write((short)method.Body.Instructions.ToArray().Count(i => !i.TemporarilyInstruction));
			foreach (var instruction in method.Body.Instructions)
			{
				if (!instruction.TemporarilyInstruction)
					WriteInstruction(instruction);
			}
		}

		private void WriteInstruction(PapyrusInstruction instruction)
		{
			pexWriter.Write((byte)instruction.OpCode);
			var desc = PapyrusInstructionOpCodeDescription.FromOpCode(instruction.OpCode);
			foreach (var arg in instruction.Arguments)
				WriteValueReference(arg);
			if (desc.HasOperandArguments)
			{
				WriteValueReference(new PapyrusVariableReference
				{
					Value = instruction.OperandArguments.Count,
					Type = PapyrusPrimitiveType.Integer
				});
				foreach (var varg in instruction.OperandArguments)
					WriteValueReference(varg);
			}
		}

		private void WriteVariable(PapyrusVariableReference variable)
		{
			pexWriter.Write(variable.Name);
			pexWriter.Write(variable.TypeName);
		}

		private void WriteParameter(PapyrusParameterDefinition papyrusParameterDefinition)
		{
			pexWriter.Write(papyrusParameterDefinition.Name);
			pexWriter.Write(papyrusParameterDefinition.TypeName);
		}

		private void WriteProperties(PapyrusTypeDefinition papyrusTypeDefinition)
		{
			var props = papyrusTypeDefinition.Properties;
			pexWriter.Write((short)props.Count);
			foreach (var prop in props)
				WritePropertyDefinition(prop);
		}

		private void WritePropertyDefinition(PapyrusPropertyDefinition prop)
		{
			pexWriter.Write(prop.Name);
			pexWriter.Write(prop.TypeName);
			pexWriter.Write(prop.Documentation);
			pexWriter.Write(prop.Userflags);
			pexWriter.Write(prop.Flags);
			if (prop.IsAuto)
				pexWriter.Write(prop.AutoName);
			else
			{
				if (prop.HasGetter)
					WriteMethod(prop.GetMethod);
				if (prop.HasSetter)
					WriteMethod(prop.SetMethod);
			}
		}

		private void WriteFields(PapyrusTypeDefinition papyrusTypeDefinition)
		{
			var fields = papyrusTypeDefinition.Fields;
			pexWriter.Write((short)fields.Count);
			foreach (var field in fields)
				WriteFieldDefinition(field);
		}

		private void WriteStructs(PapyrusTypeDefinition papyrusTypeDefinition)
		{
			var structs = papyrusTypeDefinition.NestedTypes;
			pexWriter.Write((short)structs.Count);
			foreach (var structDef in structs)
			{
				pexWriter.Write(structDef.Name);
				pexWriter.Write((short)structDef.Fields.Count);
				foreach (var field in structDef.Fields)
					WriteDocumentedField(field);
			}
		}

		private void WriteDocumentedField(PapyrusFieldDefinition field)
		{
			WriteFieldDefinition(field);
			pexWriter.Write(field.Documentation);
		}

		private void WriteFieldDefinition(PapyrusFieldDefinition field)
		{
			pexWriter.Write(field.Name);
			pexWriter.Write(field.TypeName);
			pexWriter.Write(field.UserFlags);

			WriteValueReference(field.DefaultValue);

			pexWriter.Write(field.Flags);
		}

		private void WriteValueReference(PapyrusVariableReference fieldVariable)
		{
			pexWriter.Write((byte)fieldVariable.Type);
			switch (fieldVariable.Type)
			{
				case PapyrusPrimitiveType.Reference:
					pexWriter.Write((string)fieldVariable.Value);
					break;
				case PapyrusPrimitiveType.String:
					pexWriter.Write((string)fieldVariable.Value);
					break;
				case PapyrusPrimitiveType.Boolean:
					{
						if (fieldVariable.Value is bool)
						{
							// I think this is Acceptable :p
							var value = (byte)((bool)fieldVariable.Value ? 1 : 0);
							pexWriter.Write(value);
						}
						else
						{
							var val = fieldVariable.Value.ToString().ToLower();
							pexWriter.Write((byte)(val == "true" || val == "1" ? 1 : 0));
						}
					;
					}
					break;
				case PapyrusPrimitiveType.Float:
					if (fieldVariable.Value is double)
						pexWriter.Write((float)(double)fieldVariable.Value);
					else
						pexWriter.Write(float.Parse(fieldVariable.Value.ToString()));
					break;
				case PapyrusPrimitiveType.Integer:
#warning TODO: The value to write should NEVER need to be parsed.
					pexWriter.Write(int.Parse(fieldVariable.Value.ToString()));
					break;

				default:
				case PapyrusPrimitiveType.None:
					break;
			}
		}

		private void WriteTypeInfo(PapyrusTypeDefinition def)
		{
			pexWriter.Write(def.Name);
			pexWriter.Write(def.Size);
			pexWriter.Write(def.BaseTypeName);
			pexWriter.Write(def.Documentation);
			if (Assembly.VersionTarget == PapyrusVersionTargets.Fallout4)
			{
				pexWriter.Write(def.Flags);
			}
			pexWriter.Write(def.UserFlags);
			pexWriter.Write(def.AutoStateName);
		}

		private void WriteHeaderUserflags()
		{
			var asm = Assembly;
			var count = asm.Header.UserflagReferenceHeader.Count;
			pexWriter.Write((short)count);
			foreach (var i in asm.Header.UserflagReferenceHeader)
			{
				pexWriter.Write(i.Key);
				pexWriter.Write(i.Value);
			}
		}

		private void WriteDebugInfo()
		{
			var asm = Assembly;
			var debug = asm.DebugInfo;
			pexWriter.Write(debug.DebugTime);
			if (asm.HasDebugInfo)
			{
				WriteDebugMethodDescriptions(debug);
			}

			if (asm.VersionTarget == PapyrusVersionTargets.Fallout4)
			{
				WriteDebugPropertyGroupDescriptions(debug);
				WriteDebugStructDescriptions(debug);
			}
			// pexWriter.Write
		}

		private void WriteDebugStructDescriptions(PapyrusTypeDebugInfo debug)
		{
			var ms = debug.StructDescriptions;
			pexWriter.Write((short)ms.Count);
			foreach (var strct in ms)
			{
				pexWriter.Write(strct.DeclaringTypeName);
				pexWriter.Write(strct.Name);
				pexWriter.Write((short)strct.FieldNames.Count);
				foreach (var l in strct.FieldNames)
					pexWriter.Write(l);
			}
		}

		private void WriteDebugPropertyGroupDescriptions(PapyrusTypeDebugInfo debug)
		{
			var ms = debug.PropertyDescriptions;
			pexWriter.Write((short)ms.Count);
			foreach (var prop in ms)
			{
				pexWriter.Write(prop.ObjectName);
				pexWriter.Write(prop.GroupName);
				pexWriter.Write(prop.GroupDocumentation);
				pexWriter.Write(prop.Userflags);
				pexWriter.Write((short)prop.PropertyNames.Count);
				foreach (var l in prop.PropertyNames)
					pexWriter.Write(l);
			}
		}

		private void WriteDebugMethodDescriptions(PapyrusTypeDebugInfo debug)
		{
			var ms = debug.MethodDescriptions;
			pexWriter.Write((short)ms.Count);
			foreach (var method in ms)
			{
				pexWriter.Write(method.DeclaringTypeName);
				pexWriter.Write(method.StateName);
				pexWriter.Write(method.Name);
				pexWriter.Write((byte)method.MethodType);
				pexWriter.Write((short)method.BodyLineNumbers.Count);
				foreach (var l in method.BodyLineNumbers)
					pexWriter.Write(l);
			}
		}

		private void WriteStringTable()
		{
			var count = Assembly.StringTable.Size;
			pexWriter.Write((short)count);
			foreach (var s in Assembly.StringTable)
			{
				pexWriter.Write(s.Identifier);
			}
			pexWriter.UseStringTable = true;
		}

		private void WriteHeader()
		{
			var header = Assembly.Header;
			var src = header.SourceHeader;

			pexWriter.Write(header.HeaderIdentifier);
			pexWriter.Write(src.MajorVersion);
			pexWriter.Write(src.MinorVersion);
			pexWriter.Write(src.GameId);
			pexWriter.Write(src.CompileTime);

			pexWriter.Write(src.Source);
			pexWriter.Write(src.User);
			pexWriter.Write(src.Computer);
		}

		~PapyrusAssemblyWriter()
		{
			Dispose(false);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (isDisposed) return;
			if (disposing)
			{
				pexWriter.Dispose();
			}
			isDisposed = true;
		}
	}
}