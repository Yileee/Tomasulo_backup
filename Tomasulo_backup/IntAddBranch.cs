namespace Tomasulo_backup;
using System;
using System.Collections.Generic;

public class IntAdderwBranch : FU
{
    private static readonly List<InstrTypes> instructionSetInt = new List<InstrTypes>
    {
        InstrTypes.ADD,
        InstrTypes.SUB,
        InstrTypes.ADDI,
        InstrTypes.SUBI
    };
    private static readonly List<InstrTypes> requireRobInt = instructionSetInt;

    private Branch branchUnit;

    public IntAdderwBranch(RAT rat, int execTime, int rsLen, int outBuffLen, bool pipelined = false, int btbLimit = 8)
        : base(rat, execTime, rsLen, outBuffLen, pipelined)
    {
        this.branchUnit = new Branch(function: this, btbLimit: btbLimit);
        this.InstructionSet = new HashSet<InstrTypes>(instructionSetInt);
        this.RequireRob = new HashSet<InstrTypes>(requireRobInt);

        foreach (var instrType in this.branchUnit.InstructionSet)
        {
            this.InstructionSet.Add(instrType);
        }

        foreach (var robType in this.branchUnit.RequireRob)
        {
            this.RequireRob.Add(robType);
        }
    }
    
    
    public bool PerformAction(int cycle, Instruction instr)
    {
        if (instructionSetInt.Contains(instr.Type))
        {
            return PerformActionInt(cycle, instr); // Assuming PerformActionInt is a method defined in this class or base class
        }
        if (branchUnit.InstructionSet.Contains(instr.Type))
        {
            return branchUnit.PerformAction(cycle, instr); // Assuming the PerformAction method is defined in the Branch class
        }
        return false;
    }
    
    public void AssignExecCycles(int cycle, Instruction instr)
    {
        if (instructionSetInt.Contains(instr.Type))
        {
            base.AssignExecCycles(cycle, instr);
        }
        else
        {
            if (DetermineOperandVal(instr, 0) && DetermineOperandVal(instr, 1))
            {
                instr.PipelineTimings.Exec = new Tuple<int, int>(cycle, cycle + this.execTime - 1);
            }
        }
    }

    public Instruction DecodeInt(Instruction instruction)
    {
        instruction.Operands[1] = this.rat.GetVal(instruction.Operands[1]);
        if (instruction.Type == InstrTypes.ADDI || instruction.Type == InstrTypes.SUBI)
        {
            instruction.Operands[2] = Convert.ToInt32(instruction.Operands[2]);
        }
        if (instruction.Type == InstrTypes.ADD || instruction.Type == InstrTypes.SUB)
        {
            instruction.Operands[2] = this.rat.GetVal(instruction.Operands[2]);
        }
        instruction.Operands[0] = this.rat.ReserveRob(instruction.Operands[0]);
        instruction.Dest = instruction.Operands[0];
        instruction.PipelineTimings.Mem = SkipTag; // Replace SkipTag with the appropriate constant or value
        return instruction;
    }
    
    public void PerformActionInt(int cycle, Instruction instr)
    {
        if (instr.Type == InstrTypes.ADD || instr.Type == InstrTypes.ADDI)
        {
            instr.Result = Convert.ToInt32(instr.Operands[1] + instr.Operands[2]);
        }
        else if (instr.Type == InstrTypes.SUB || instr.Type == InstrTypes.SUBI)
        {
            instr.Result = Convert.ToInt32(instr.Operands[1] - instr.Operands[2]);
        }
    }
}
