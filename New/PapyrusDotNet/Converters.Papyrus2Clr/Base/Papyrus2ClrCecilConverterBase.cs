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

using PapyrusDotNet.Common.Interfaces;
using PapyrusDotNet.Converters.Papyrus2Clr.Implementations;

#endregion

namespace PapyrusDotNet.Converters.Papyrus2Clr.Base
{
	public abstract class Papyrus2ClrCecilConverterBase : IPapyrusOutputConverter
	{
		protected Papyrus2ClrCecilConverterBase(INamespaceResolver namespaceResolver,
			ITypeReferenceResolver typeReferenceResolver)
		{
			NamespaceResolver = namespaceResolver;
			TypeReferenceResolver = typeReferenceResolver;
		}

		protected INamespaceResolver NamespaceResolver { get; }
		protected ITypeReferenceResolver TypeReferenceResolver { get; }

		public IAssemblyOutput Convert(IAssemblyInput input)
		{
			return ConvertAssembly(input as PapyrusAssemblyInput);
		}

		protected abstract CecilAssemblyOutput ConvertAssembly(PapyrusAssemblyInput input);
	}
}