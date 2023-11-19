namespace Tomasulo_backup;
using System;
using System.Collections.Generic;

public class LoadStoreUnit
{
    private static readonly InstrTypes[] InstructionSet = { InstrTypes.LD, InstrTypes.SD };
    private static readonly InstrTypes[] RequireRob = { InstrTypes.LD };

    private RAT rat;
    private int execTime;
    private Dictionary<string, string> cycleLog;
    private RAM ram;
    private int loadStoreQueueLen;
    private List<Instruction> loadStoreQueue;
    private int outBuffLen;
    private List<Instruction> outBuff;

    public LoadStoreUnit(RAT rat, int execTime, int ramLen, int ramLatency, int queueLen, int outBuffLen)
    {
        this.rat = rat;
        this.execTime = execTime;
        this.cycleLog = new Dictionary<string, string>();
        this.ram = new RAM(ramLen, ramLatency);
        this.loadStoreQueueLen = queueLen;
        this.loadStoreQueue = new List<Instruction>();
        this.outBuffLen = outBuffLen;
        this.outBuff = new List<Instruction>();
    }
    

    public bool IsFull()
    {
        return loadStoreQueue.Count >= loadStoreQueueLen;
    }

    public bool IsEmpty()
    {
        // Return true regardless of the actual queue status, as per your Python code.
        return true;
    }

    public void Remove(Instruction instr)
    {
        loadStoreQueue.Remove(instr);
        outBuff.Remove(instr);
    }

    public void Remove(List<Instruction> instrList)
    {
        foreach (var instr in instrList)
        {
            Remove(instr); // Reuse the single item removal method
        }
    }

    public void Issue(Instruction instr)
    {
        if (IsFull())
        {
            throw new InvalidOperationException("Load Store Queue is still full!");
        }

        if (!InstructionSet.Contains(instr.Type))
        {
            throw new ArgumentException($"Invalid Instruction type: {instr.Type}");
        }

        loadStoreQueue.Add(instr);
    }

    public Instruction Decode(Instruction instr)
    {
        instr.Operands[1] = Convert.ToInt32(instr.Operands[1]);
        instr.Operands[2] = rat.GetVal(instr.Operands[2]);

        if (instr.Type == InstrTypes.LD)
        {
            instr.Operands[0] = rat.ReserveRob(instr.Operands[0]);
            instr.Dest = instr.Operands[0];
        }
        else if (instr.Type == InstrTypes.SD)
        {
            instr.Operands[0] = rat.GetVal(instr.Operands[0]);
            instr.Result = NullTag; // Assuming NullTag and SkipTag are predefined
            instr.Dest = SkipTag;
            instr.PipelineTimings.WriteBack = SkipTag;
        }

        return instr;
    }
    
    public void AssignExecCycles(int cycle, Instruction instr)
    {
        if (DetermineOperandVal(instr, 1) && DetermineOperandVal(instr, 2))
        {
            instr.PipelineTimings.Exec = new Tuple<int, int>(cycle, cycle + execTime - 1);
        }
    }

    private bool DetermineOperandVal(Instruction instr, int opIdx)
    {
        if (instr.Operands[opIdx] == null)
        {
            throw new InvalidOperationException("Error Indexing Operand! Index is out of bounds or operator is not present!");
        }

        if (!(instr.Operands[opIdx] is ROBEntry))
        {
            return true;
        }

        var robEntry = (ROBEntry)instr.Operands[opIdx];
        if (robEntry.Finished)
        {
            instr.Operands[opIdx] = robEntry.Value;
            return true;
        }

        return false;
    }
    
    public void StepMemory(int cycle)
    {
        foreach (var instr in loadStoreQueue)
        {
            if (instr.PipelineTimings.Mem == null)
            {
                continue;
            }

            if (instr.PipelineTimings.Mem.Item1 <= cycle && cycle < instr.PipelineTimings.Mem.Item2)
            {
                return;
            }

            if (instr.PipelineTimings.Mem.Item2 == cycle)
            {
                PerformActionMem(cycle, instr);
                return;
            }
        }

        foreach (var instr in loadStoreQueue)
        {
            if (instr.PipelineTimings.Exec == null || instr.PipelineTimings.Exec.Item2 >= cycle)
            {
                continue;
            }

            if (instr.PipelineTimings.Mem == null && instr.MemAddr != null)
            {
                if (AssignMemCycles(cycle, instr))
                {
                    if (instr.PipelineTimings.Mem.Item2 == cycle)
                    {
                        PerformActionMem(cycle, instr);
                        return;
                    }
                }
            }
        }
    }

    public void PerformActionMem(int cycle, Instruction instr)
    {
        int index = loadStoreQueue.IndexOf(instr);
        if (index == -1) return; // Instr not found in the queue

        if (instr.Type == InstrTypes.LD)
        {
            foreach (var prevInstr in loadStoreQueue.GetRange(0, index).Reverse())
            {
                if (prevInstr.MemAddr == instr.MemAddr)
                {
                    if (prevInstr.Type == InstrTypes.LD)
                    {
                        instr.Result = prevInstr.Result;
                        instr.ComputationCompletion = cycle;
                        return;
                    }

                    if (prevInstr.Type == InstrTypes.SD)
                    {
                        instr.Result = prevInstr.Operands[0];
                        instr.ComputationCompletion = cycle;
                        return;
                    }
                }
            }

            if (instr.Result == null) // Value not found in cache, get it from RAM
            {
                instr.Result = ram.GetVal(instr.MemAddr);
                instr.ComputationCompletion = cycle;
                return;
            }
        }

        if (instr.Type == InstrTypes.SD)
        {
            ram.SetVal(instr.MemAddr, instr.Operands[0]);
            return;
        }

        instr.ComputationCompletion = cycle;
    }

    public bool AssignMemCycles(int cycle, Instruction instr)
    {
        int index = loadStoreQueue.IndexOf(instr);
        if (index == -1) return false; // Instr not found in the queue

        if (instr.Type == InstrTypes.LD)
        {
            foreach (var prevInstr in loadStoreQueue.GetRange(0, index).Reverse())
            {
                if (prevInstr.Type == InstrTypes.LD && 
                    (prevInstr.PipelineTimings.Mem == null || prevInstr.PipelineTimings.Mem.Item2 >= cycle))
                {
                    return false; // Another Load is not done
                }

                if (prevInstr.Type == InstrTypes.SD && 
                    (prevInstr.PipelineTimings.Exec == null || prevInstr.PipelineTimings.Exec.Item2 >= cycle))
                {
                    return false; // A Store has not yet calculated its address
                }

                if (prevInstr.Type == InstrTypes.SD && prevInstr.MemAddr == instr.MemAddr)
                {
                    if (prevInstr.Operands[0] is ROBEntry robEntry && !DetermineOperandVal(prevInstr, 0))
                    {
                        return false;
                    }
                    instr.PipelineTimings.Mem = new Tuple<int, int>(cycle, cycle);
                    return true;
                }
            }
            instr.PipelineTimings.Mem = new Tuple<int, int>(cycle, cycle + ram.ExecTime - 1);
            return true;
        }

        if (instr.Type == InstrTypes.SD)
        {
            if (instr.Operands[0] is ROBEntry robEntry && !robEntry.Finished)
            {
                return false; // Store's value is not ready
            }

            if (instr.Prev != null)
            {
                if (instr.Prev.PipelineTimings.Commit == null || instr.Prev.PipelineTimings.Commit.Item2 >= cycle)
                {
                    return false; // Last instruction has not committed
                }
            }
            instr.PipelineTimings.Mem = new Tuple<int, int>(cycle, cycle + ram.ExecTime - 1);
            instr.PipelineTimings.Commit = instr.PipelineTimings.Mem;
            return true;
        }

        return false;
    }
    
    
    public void StepWriteBack(int cycle)
    {
        if (outBuff.Count >= outBuffLen)
        {
            // Output buffer is full, so return early
            return;
        }

        List<Instruction> compList = new List<Instruction>();

        foreach (var instr in loadStoreQueue)
        {
            if (instr.Type == InstrTypes.SD)
            {
                continue;
            }

            if (instr.ComputationCompletion == null || instr.ComputationCompletion >= cycle)
            {
                continue;
            }

            if (instr.PipelineTimings.WriteBack != null)
            {
                continue;
            }

            compList.Add(instr);
        }

        // Sort primarily by computation completion, then by cycle
        compList = compList.OrderBy(i => i.ComputationCompletion)
            .ThenBy(i => i.Cycle)
            .ToList();

        while (outBuff.Count < outBuffLen && compList.Any())
        {
            Instruction instr = compList.First();
            compList.RemoveAt(0);
            outBuff.Add(instr);
        }
    }
    
    public string PrintInfo()
    {
        StringBuilder output = new StringBuilder();
        output.AppendLine($"--- {this.GetType().Name} Load Store Queue Info ---");

        for (int i = 0; i < loadStoreQueue.Count; i++)
        {
            output.AppendLine($"{i + 1}: {loadStoreQueue[i]}");
        }

        if (output.Length == 0 || output[output.Length - 1] != '\n')
        {
            output.AppendLine();
        }

        return output.ToString();
    }
}
