namespace Tomasulo_backup;
using System;
using System.Collections.Generic;

public class IntAdder : FU
{
    private static readonly HashSet<InstrTypes> instructionSet = new HashSet<InstrTypes>
    {
        InstrTypes.ADD,
        InstrTypes.SUB,
        InstrTypes.ADDI,
        InstrTypes.SUBI
    };

    private static readonly HashSet<InstrTypes> requireRob = instructionSet;

    public Instruction Decode(Instr instruction)
    {
        instruction.Operands[1] = this.Rat.GetVal(instruction.Operands[1]);

        if (instruction.Type == InstrTypes.ADDI || instruction.Type == InstrTypes.SUBI)
        {
            instruction.Operands[2] = Convert.ToInt32(instruction.Operands[2]);
        }

        if (instruction.Type == InstrTypes.ADD || instruction.Type == InstrTypes.SUB)
        {
            instruction.Operands[2] = this.Rat.GetVal(instruction.Operands[2]);
        }

        instruction.Operands[0] = this.Rat.ReserveRob(instruction.Operands[0]);
        instruction.Dest = instruction.Operands[0];
        instruction.PipelineTimings.Mem = SkipTag; // Assuming SkipTag is a predefined constant

        return instruction;
    }

    public void PerformAction(int cycle, Instruction instr)
    {
        if (instr.Type == InstrTypes.ADD || instr.Type == InstrTypes.ADDI)
        {
            instr.Result = Convert.ToInt32(instr.Operands[1]) + Convert.ToInt32(instr.Operands[2]);
        }

        if (instr.Type == InstrTypes.SUB || instr.Type == InstrTypes.SUBI)
        {
            instr.Result = Convert.ToInt32(instr.Operands[1]) - Convert.ToInt32(instr.Operands[2]);
        }
    }
}
