﻿/*
    This file is part of PapyrusDotNet.

    PapyrusDotNet is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    PapyrusDotNet is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with PapyrusDotNet.  If not, see <http://www.gnu.org/licenses/>.
	
	Copyright 2014, Karl Patrik Johansson, zerratar@gmail.com
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PapyrusDotNet.Launcher
{
	public class SkyrimInterface : MarshalByRefObject
	{
		public void IsInstalled(Int32 InClientPID)
		{
			Console.WriteLine("PapyrusDotNet.Bridge has been installed in target {0}.\r\n", InClientPID);
		}

		public void IntensiveThingHere(string val)
		{
			Console.WriteLine(val);
		}

		public void ReportException(Exception ExtInfo)
		{
			throw new NotImplementedException();
		}
	}
}