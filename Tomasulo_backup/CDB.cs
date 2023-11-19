namespace Tomasulo_backup;
using System;
using System.Collections.Generic;
using System.Linq;

public class CDB
{
    private List<FU> funcUnitList;
    private InstructionBuffer instrBuff;
    private RAT rat;

    public CDB(List<FU> funcUnitList, InstructionBuffer instrBuff, RAT rat)
    {
        this.funcUnitList = funcUnitList;
        this.instrBuff = instrBuff;
        this.rat = rat;
    }

    public List<Instruction> AppendFinishedComputations(int cycle)
    {
        List<Instruction> results = new List<Instruction>();
        foreach (var unit in funcUnitList)
        {
            results.AddRange(unit.OutBuff);
        }
        results.Sort((x, y) => x.ComputationCompletion != y.ComputationCompletion ? x.ComputationCompletion.CompareTo(y.ComputationCompletion) : x.Cycle.CompareTo(y.Cycle));
        return results;
    }

    public void SendDataOverCdb(int cycle, List<Instruction> results)
    {
        foreach (var instr in results)
        {
            instr.FunctionalUnit.OutBuff.Remove(instr);
            if (WriteBack(instr.Dest, instr.Result))
            {
                instr.PipelineTimings.WriteBack = cycle;
                break;
            }
        }
    }

    public bool WriteBack(object dest, object result)
    {
        if (dest.Equals(SKIP_TAG))
        {
            return false;
        }
        if (dest.Equals(NULL_TAG))
        {
            return true;
        }
        ROBEntry robEntry = rat.SetRobVal(dest, result);
        foreach (var instr in instrBuff.History)
        {
            int index = instr.Operands.IndexOf(robEntry);
            if (index == -1)
            {
                continue;
            }
            instr.Operands[index] = robEntry.Value;
        }
        return true;
    }

    public void StepExec(int cycle)
    {
        var compResults = AppendFinishedComputations(cycle);
        SendDataOverCdb(cycle, compResults);
    }
}