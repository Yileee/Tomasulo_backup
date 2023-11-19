namespace Tomasulo_backup;
using System;
using System.Collections.Generic;

public class FPMultiplier : FU
{
    private static readonly HashSet<InstrTypes> instructionSet = new HashSet<InstrTypes>
    {
        InstrTypes.MULTD,
        InstrTypes.DIVD
    };

    private static readonly HashSet<InstrTypes> requireRob = instructionSet;

    public Instruction Decode(Instruction instr)
    {
        instr.Operands[1] = this.Rat.GetVal(instr.Operands[1]);
        instr.Operands[2] = this.Rat.GetVal(instr.Operands[2]);
        instr.Operands[0] = this.Rat.ReserveRob(instr.Operands[0]);
        instr.Dest = instr.Operands[0];
        instr.PipelineTimings.Mem = SkipTag; // Assuming SkipTag is a predefined constant

        return instr;
    }

    public void PerformAction(int cycle, Instruction instr)
    {
        if (instr.Type == InstrTypes.MULTD)
        {
            instr.Result = instr.Operands[1] * instr.Operands[2];
        }
        else if (instr.Type == InstrTypes.DIVD)
        {
            instr.Result = instr.Operands[1] / instr.Operands[2];
        }
    }
}
