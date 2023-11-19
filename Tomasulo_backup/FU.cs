namespace Tomasulo_backup;
sing System.Collections.Generic;

public class FU
{
    public HashSet<InstrTypes> InstructionSet { get; set; }
    public HashSet<InstrTypes> RequireRob { get; set; }

    private RAT rat;
    private int reservationStationLen;
    private List<Instruction> reservationStation;
    private int outBuffLen;
    private List<Instruction> outBuff;
    private int execTime;
    private bool pipelined;
    private Dictionary<YourKeyType, YourValueType> cycleLog;

    public FU(RAT rat, int execTime, int rsLen, int outBuffLen, bool pipelined = false)
    {
        this.rat = rat;
        this.reservationStationLen = rsLen;
        this.reservationStation = new List<Instruction>();
        this.outBuffLen = outBuffLen;
        this.outBuff = new List<Instruction>();
        this.execTime = execTime;
        this.pipelined = pipelined;
        this.cycleLog = new Dictionary<YourKeyType, YourValueType>();
    }
    
    public bool IsEmpty()
    {
        return this.reservationStation.Count == 0;
    }

    public bool IsFull()
    {
        return this.reservationStation.Count >= this.reservationStationLen;
    }

    public void Remove(object instr) // Using object here to handle both Instr and List<Instr>
    {
        if (instr is List<Instruction> instrList)
        {
            foreach (Instruction i in instrList)
            {
                this.reservationStation.Remove(i);
                this.outBuff.Remove(i);
            }
        }
        else if (instr is Instruction singleInstr)
        {
            this.reservationStation.Remove(singleInstr);
            this.outBuff.Remove(singleInstr);
        }
    }
    
    public void Issue(Instruction instr)
    {
        if (this.IsFull())
        {
            throw new InvalidOperationException("Reservation Station is full!");
        }
        if (!this.instructionSet.Contains(instr.Type))
        {
            throw new ArgumentException($"Invalid Instruction type: {instr.Type}");
        }
        this.reservationStation.Add(instr);
    }

    public void Decode(Instruction instr)
    {
        throw new NotImplementedException();
    }
    
    
    public void StepExec(int cycle)
    {
        if (!this.pipelined)
        {
            foreach (var instr in this.reservationStation)
            {
                if (instr.PipelineTimings.Exec == null || instr.PipelineTimings.Exec.Item2 > cycle)
                {
                    return;
                }
                if (instr.PipelineTimings.Exec.Item2 == cycle)
                {
                    try
                    {
                        PerformAction(cycle, instr);
                    }
                    catch (ArithmeticException)
                    {
                        instr.Result = FaultTag; // Replace FaultTag with the appropriate constant or value
                        instr.ComputationCompletion = cycle;
                        return;
                    }
                    catch (Exception)
                    {
                        throw new NotImplementedException();
                    }
                    instr.ComputationCompletion = cycle;
                    return;
                }
            }
        }

        foreach (var instr in this.reservationStation)
        {
            if (instr.PipelineTimings.Issue > cycle)
            {
                throw new InvalidOperationException("Instruction was added to a reservation station before its issue cycle!");
            }
            if (instr.PipelineTimings.Issue == cycle)
            {
                continue;
            }
            if (instr.PipelineTimings.Exec == null)
            {
                AssignExecCycles(cycle, instr); // Implement this method as needed
                if (!this.pipelined && instr.PipelineTimings.Exec != null && instr.PipelineTimings.Exec.Item2 != cycle)
                {
                    return;
                }
            }
            if (instr.PipelineTimings.Exec != null && instr.PipelineTimings.Exec.Item2 == cycle)
            {
                try
                {
                    PerformAction(cycle, instr);
                }
                catch (ArithmeticException)
                {
                    instr.Result = FaultTag; // Replace FaultTag with the appropriate constant or value
                    instr.ComputationCompletion = cycle;
                    continue;
                }
                catch (Exception)
                {
                    throw new Exception(); // You might want to specify the type of exception or create a custom one
                }
                instr.ComputationCompletion = cycle;
                if (this.pipelined)
                {
                    continue;
                }
                else
                {
                    return;
                }
            }
        }
    }
    
    public void AssignExecCycles(int cycle, Instruction instr)
    {
        if (DetermineOperandVal(instr, 1) && DetermineOperandVal(instr, 2))
        {
            instr.PipelineTimings.Exec = new Tuple<int, int>(cycle, cycle + this.execTime - 1);
        }
    }

    public bool PerformAction(int cycle, Instruction instr)
    {
        throw new NotImplementedException();
    }

    public void StepMemory(int cycle)
    {
        // Empty method body, as 'pass' in Python does nothing.
    }

    public bool PerformActionMem(int cycle, Instruction instr)
    {
        throw new NotImplementedException();
    }
    
    public bool DetermineOperandVal(Instruction instr, int opIdx)
    {
        if (instr.Type == InstrTypes.NOP)
        {
            return true;
        }
        if (instr.Operands[opIdx] == null)
        {
            throw new InvalidOperationException("Error Indexing Operand! Index is out of bounds or operator is not present!");
        }
        if (!(instr.Operands[opIdx] is ROBEntry)) // Checking if it is not a ROBEntry
        {
            return true;
        }
        if ((instr.Operands[opIdx] as ROBEntry).Finished) // Type casting after checking the type
        {
            instr.Operands[opIdx] = (instr.Operands[opIdx] as ROBEntry).Value; // Type casting to assign the value
            return true;
        }
        return false;
    }
    
    public void StepWriteback(int cycle)
    {
        if (this.outBuff.Count >= this.outBuffLen)
        {
            return; // Exit the method if the output buffer is full
        }

        List<Instruction> compList = new List<Instruction>();
        foreach (var instr in this.reservationStation.ToList()) // ToList() to avoid modifying collection during iteration
        {
            if (instr.ComputationCompletion == null || instr.ComputationCompletion >= cycle)
            {
                continue;
            }
            if (instr.Dest == SkipTag || (instr.PipelineTimings.WriteBack != null && instr.PipelineTimings.WriteBack == SkipTag))
            {
                this.reservationStation.Remove(instr);
                continue;
            }
            compList.Add(instr);
        }

        // Sorting the completed instructions
        compList = compList.OrderBy(x => x.ComputationCompletion).ThenBy(x => x.Cycle).ToList();

        while (this.outBuff.Count < this.outBuffLen && compList.Count > 0)
        {
            var instr = compList.First();
            compList.RemoveAt(0);
            this.outBuff.Add(instr);
            this.reservationStation.Remove(instr);
        }
    }
    
    public string PrintInfo()
    {
        StringBuilder output = new StringBuilder();
        output.AppendLine($"--- {this.GetType().Name} Reservation Station Info ---");

        foreach (var (x, i) in this.reservationStation.Select((x, i) => (x, i)))
        {
            output.AppendLine($"{i + 1}: {x}"); // Assuming x.ToString() gives the desired representation
        }

        if (output.Length == 0 || output[output.Length - 1] != '\n')
        {
            output.AppendLine();
        }

        return output.ToString();
    }
}