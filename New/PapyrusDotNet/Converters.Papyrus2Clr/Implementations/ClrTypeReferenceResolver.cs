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
//     Copyright 2015, Karl Patrik Johansson, zerratar@gmail.com

#region

using Mono.Cecil;
using PapyrusDotNet.Common.Interfaces;

#endregion

namespace PapyrusDotNet.Converters.Papyrus2Clr.Implementations
{
	public class ClrTypeReferenceResolver : ITypeReferenceResolver
	{
		private readonly INamespaceResolver namespaceResolver;
		private readonly ITypeNameResolver typeNameResolver;

		public ClrTypeReferenceResolver(INamespaceResolver namespaceResolver,
			ITypeNameResolver typeNameResolver)
		{
			this.namespaceResolver = namespaceResolver;
			this.typeNameResolver = typeNameResolver;
		}

		public TypeReference Resolve(ref IList<string> reservedNames, ref IList<TypeReference> referenceList,
			ModuleDefinition mainModule, TypeDefinition newType, string fallbackTypeName = null)
		{
			var typeName = !string.IsNullOrEmpty(fallbackTypeName) ? fallbackTypeName : newType.FullName;

			var ns = namespaceResolver.Resolve(typeName);
			var tn = typeNameResolver.Resolve(typeName);
			var isArray = tn.Contains("[]");

			if (ns == "System")
			{
				var propies =
					mainModule.TypeSystem.GetType()
						.GetProperties()
						.Where(pr => pr.PropertyType == typeof(TypeReference))
						.ToList();
				foreach (var propy in propies)
				{
					var name = propy.Name;
					if (tn.Replace("[]", "").ToLower() == name.ToLower())
					{
						var val = propy.GetValue(mainModule.TypeSystem, null) as TypeReference;
						return val != null && isArray && !val.IsArray ? new ArrayType(val) : val;
					}
				}
				// fallback
				switch (tn.ToLower())
				{
					case "none":
					case "void":
						return mainModule.TypeSystem.Void;
					case "byte":
					case "short":
					case "int":
					case "long":
					case "int8":
					case "int16":
					case "int32":
					case "int64":
						return isArray ? new ArrayType(mainModule.TypeSystem.Int32) : mainModule.TypeSystem.Int32;
					case "string":
						return isArray ? new ArrayType(mainModule.TypeSystem.String) : mainModule.TypeSystem.String;
					case "float":
					case "double":
						return isArray ? new ArrayType(mainModule.TypeSystem.Double) : mainModule.TypeSystem.Double;
					case "bool":
					case "boolean":
						return isArray ? new ArrayType(mainModule.TypeSystem.Boolean) : mainModule.TypeSystem.Boolean;
					default:
						return isArray ? new ArrayType(mainModule.TypeSystem.Object) : mainModule.TypeSystem.Object;
				}
			}
			var tnA = tn.Replace("[]", "");
			var existing =
				referenceList.FirstOrDefault(ty => ty.FullName.ToLower() == (ns + "." + tnA).ToLower());
			if (existing == null)
			{
				var hasTypeOf = mainModule.Types.FirstOrDefault(t => t.FullName.ToLower() == (ns + "." + tnA).ToLower());
				if (hasTypeOf != null)
				{
					var typeRef = new TypeReference(hasTypeOf.Namespace, hasTypeOf.Name, mainModule, mainModule)
					{
						Scope = mainModule
					};
					referenceList.Add(typeRef);
					return isArray && !typeRef.IsArray ? new ArrayType(typeRef) : typeRef;
				}
				else
				{
					if (
						reservedNames.Any(
							n => string.Equals(n, tnA, StringComparison.CurrentCultureIgnoreCase)))
					{
						tn =
							reservedNames.FirstOrDefault(
								j => string.Equals(j, tnA, StringComparison.CurrentCultureIgnoreCase));
					}
					var typeRef = new TypeReference(ns, tn, mainModule, mainModule) { Scope = mainModule };


					referenceList.Add(typeRef);
					return isArray && !typeRef.IsArray ? new ArrayType(typeRef) : typeRef;
				}
			}

			return isArray && !existing.IsArray ? new ArrayType(existing) : existing;
		}
	}
}