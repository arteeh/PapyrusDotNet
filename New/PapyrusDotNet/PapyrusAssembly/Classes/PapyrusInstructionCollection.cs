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

using System.Collections;

#endregion

namespace PapyrusDotNet.PapyrusAssembly
{
	public class PapyrusInstructionCollection : IEnumerable<PapyrusInstruction>
	{
		public PapyrusInstructionCollection(PapyrusMethodDefinition method)
		{
			this.method = method;
		}

		private readonly List<PapyrusInstruction> items = new List<PapyrusInstruction>();

		private PapyrusMethodDefinition method;

		public int Count => items.Count;

		public PapyrusInstruction this[int index] => items[index];

		public IEnumerator<PapyrusInstruction> GetEnumerator()
		{
			return items.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public void Insert(int index, PapyrusInstruction item)
		{
			if (item.Method == null) item.Method = method;
			if (index >= items.Count)
				items.Add(item);
			else
				items.Insert(index, item);
		}

		public void Add(PapyrusInstruction item)
		{
			if (item.Method == null) item.Method = method;
			items.Add(item);
		}

		public void AddRange(IEnumerable<PapyrusInstruction> i)
		{
			var inst = i.ToList();
			inst.ForEach(item =>
		   {
			   if (item.Method == null) item.Method = method;
		   });
			items.AddRange(inst);
		}

		public void Remove(PapyrusInstruction item)
		{
			items.Remove(item);
		}

		public void RemoveAt(int index)
		{
			items.RemoveAt(index);
		}

		public void RecalculateOffsets()
		{
			// TODO: Call RecalculateOffset(); whenever the Method has been finalized.
			for (var offset = 0; offset < items.Count; offset++)
			{
				items[offset].Offset = offset;
			}
			// TODO: Update any instructions with operand of another instruction
			// now that the instructions have new offsets, the Parameters needs to be updated.
			// -- JUMP: First Parameter needs to be updated
			// -- JUMPF or JUMPT: Second Parameter needs to be updated

			for (var offset = 0; offset < items.Count; offset++)
			{
				var instruction = items[offset].Operand as PapyrusInstruction;
				if (instruction != null)
				{
					if (items[offset].OpCode == PapyrusOpCodes.Jmp)
					{
						items[offset].Arguments[0].Value = instruction.Offset - offset;
						items[offset].Arguments[0].Type = PapyrusPrimitiveType.Integer;
					}
					else if (items[offset].OpCode == PapyrusOpCodes.Jmpt || items[offset].OpCode == PapyrusOpCodes.Jmpf)
					{
						items[offset].Arguments[1].Value = instruction.Offset - offset;
						items[offset].Arguments[1].Type = PapyrusPrimitiveType.Integer;
					}
				}
			}
		}

		public void ForEach(Action<PapyrusInstruction> action)
		{
			foreach (var item in items)
			{
				action(item);
			}
		}
	}
}