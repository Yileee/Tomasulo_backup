namespace Tomasulo_backup;
using System;
using System.Collections.Generic;

public class NOPUnit : FU
{
    private static readonly HashSet<InstrTypes> instructionSet = new HashSet<InstrTypes> { InstrTypes.NOP };
    private static readonly List<InstrTypes> requireRob = new List<InstrTypes>();

    public Instruction Decode(Instruction instr)
    {
        instr.Result = NullTag; // Assuming NullTag is a predefined constant
        instr.Dest = NullTag;   // Assuming NullTag is a predefined constant
        instr.PipelineTimings.Mem = SkipTag; // Assuming SkipTag is a predefined constant

        return instr;
    }

    public void PerformAction(int cycle, Instruction instr)
    {
        // Empty implementation as the original method returns None
    }

    public bool IsFull()
    {
        return false;
    }
}
