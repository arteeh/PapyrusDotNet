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
using PapyrusDotNet.Converters.Clr2Papyrus.Interfaces;

#endregion

namespace PapyrusDotNet.Converters.Clr2Papyrus.Implementations
{
	public class DelegatePairDefinition : IDelegatePairDefinition
	{
		public Dictionary<MethodDefinition, List<FieldDefinition>> DelegateMethodFieldPair { get; set; } =
			new Dictionary<MethodDefinition, List<FieldDefinition>>();

		public Dictionary<MethodDefinition, List<VariableReference>> DelegateMethodLocalPair { get; set; } =
			new Dictionary<MethodDefinition, List<VariableReference>>();

		public List<MethodDefinition> DelegateMethodDefinitions { get; set; } = new List<MethodDefinition>();
		public List<FieldDefinition> DelegateFields { get; set; } = new List<FieldDefinition>();
		public List<TypeDefinition> DelegateTypeDefinitions { get; set; } = new List<TypeDefinition>();
	}
}