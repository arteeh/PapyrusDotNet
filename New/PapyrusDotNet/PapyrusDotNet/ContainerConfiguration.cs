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

using PapyrusDotNet.Common;
using PapyrusDotNet.Common.Interfaces;
using PapyrusDotNet.Common.Utilities;

namespace PapyrusDotNet
{
	public class ContainerConfiguration
	{
		private readonly bool noUserInterface;
		private readonly IoCContainer ioc;

		public ContainerConfiguration(bool noUserInterface)
		{
			this.noUserInterface = noUserInterface;
			ioc = new IoCContainer();
			RegisterProcessors();
		}

		private void RegisterProcessors()
		{
			if (noUserInterface)
				ioc.Register<IUserInterface, OutputUserInterface>(); // NoopUserInterface
			else
				ioc.Register<IUserInterface, AdvancedConsoleUserInterface>();

			ioc.Register<IValueTypeConverter, PapyrusValueTypeConverter>();
			ioc.Register<ILoadProcessor, LoadProcessor>();
			ioc.Register<IStoreProcessor, StoreProcessor>();
			ioc.Register<IBranchProcessor, BranchProcessor>();
			ioc.Register<ICallProcessor, CallProcessor>();
			ioc.Register<IConditionalProcessor, ConditionalProcessor>();
			ioc.Register<IReturnProcessor, ReturnProcessor>();
			ioc.Register<IStringConcatProcessor, StringConcatProcessor>();

			ioc.Register<IClrInstructionProcessor, ClrInstructionProcessor>();
			ioc.RegisterCustom<PascalCaseNameResolverSettings>(() => new PascalCaseNameResolverSettings("wordlist-fo4.txt"));
			ioc.Register<INameConventionResolver, PascalCaseNameResolver>();
		}

		public T Resolve<T>()
		{
			return ioc.Resolve<T>();
		}
	}
}