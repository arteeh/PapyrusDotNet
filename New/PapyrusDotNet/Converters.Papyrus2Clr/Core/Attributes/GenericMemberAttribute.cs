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


#endregion

namespace PapyrusDotNet.Converters.Papyrus2Clr.Core.Attributes
{
	public class GenericMemberAttribute : Attribute
	{
		/// <summary>
		///     Specify to PapyrusDotNet that a copy will be made
		///     of this property to match the target/used type.
		///     The name will automatically be [typename]_[membername]
		///     This will automatically be resolved for your code.
		/// </summary>
		public GenericMemberAttribute()
		{
			// if none defined, an explicit copy of the member
			// to all used types.
		}

		public GenericMemberAttribute(string targetType, string memberName)
		{
			// [GenericMember("Int", "Add")]
			// 
		}
	}
}