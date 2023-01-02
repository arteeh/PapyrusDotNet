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


#endregion

namespace PapyrusDotNet.PapyrusAssembly
{
	public class PapyrusTypeDebugInfo
	{
		public PapyrusTypeDebugInfo()
		{
			MethodDescriptions = new List<PapyrusMethodDecription>();
			PropertyDescriptions = new List<PapyrusStatePropertyDescriptions>();
			StructDescriptions = new List<PapyrusStructDescription>();
		}

		public bool HasMethodDescriptions => MethodDescriptions != null && MethodDescriptions.Count > 0;
		public List<PapyrusMethodDecription> MethodDescriptions { get; set; }
		public bool HasPropertyDescriptions => PropertyDescriptions != null && PropertyDescriptions.Count > 0;
		public List<PapyrusStatePropertyDescriptions> PropertyDescriptions { get; set; }
		public bool HasStructDescriptions => StructDescriptions != null && StructDescriptions.Count > 0;
		public List<PapyrusStructDescription> StructDescriptions { get; set; }
		public long DebugTime { get; set; }
	}
}