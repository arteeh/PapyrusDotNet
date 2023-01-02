using Mono.Cecil;
using Mono.Cecil.Cil;

namespace PapyrusDotNet.Converters.Clr2Papyrus.Interfaces
{
	public interface IPapyrusInstructionProcessor
	{
		IEnumerable<PapyrusInstruction> ParseInstruction(Instruction instruction, MethodDefinition targetMethod,
			TypeDefinition type);
	}
}