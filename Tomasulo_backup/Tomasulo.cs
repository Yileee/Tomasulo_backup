namespace Tomasulo_backup;
using System;
using System.Collections.Generic;
using System.Linq;

public class Tomasulo
{
    public static int Cycle { get; set; } = 1;
    public Dictionary<string, int> Param { get; private set; }
    public InstructionBuffer InstrBuff { get; private set; }
    public Dictionary<string, int> UnusedCodeParameters { get; private set; }
    public RAT Rat { get; private set; }
    public List<FU> FunctionalUnits { get; private set; }
    public NOPUnit NopUnit { get; private set; }
    public LoadStoreUnit LsUnit { get; private set; }
    public CDB Cdb { get; private set; }

    public Tomasulo(string fullCode, Dictionary<string, int> param)
    {
        Param = new Dictionary<string, int>(param);
        InstrBuff = new InstructionBuffer();
        InstrBuff.AppendCode(fullCode);
        UnusedCodeParameters = new Dictionary<string, int>(InstrBuff.CodeParameters);
        OverrideParam(UnusedCodeParameters);

        Rat = new RAT(Param["maximal_register_value"], Param["maximal_register_value"], Param["rob_length"]);
        Rat.InitializeValues(UnusedCodeParameters);

        FunctionalUnits = new List<FU>();
        NopUnit = new NOPUnit(Rat, Param["nop_exec_time"], -1, Param["output_buffer_size"], Convert.ToBoolean(Param["nop_pipelined"]));
        FunctionalUnits.Add(NopUnit);

        CreateFunctionalUnits();

        LsUnit = new LoadStoreUnit(Rat, Param["load_store_unit_exec_cycles"], Param["load_store_unit_ram_length"], Param["load_store_unit_ram_latency"], Param["load_store_unit_queue_length"], Param["output_buffer_size"]);
        LsUnit.Ram.InitializeValues(UnusedCodeParameters);
        FunctionalUnits.Add(LsUnit);

        Cdb = new CDB(FunctionalUnits, InstrBuff, Rat);

        if (UnusedCodeParameters.Count != 0)
        {
            throw new ArgumentException($"Invalid Parameters given to the Tomasulo class!: {UnusedCodeParameters}");
        }
    }

    private void CreateFunctionalUnits()
    {
        // Add functional units based on the parameters
        // Similar to the Python implementation
    }

    private void OverrideParam(Dictionary<string, int> unusedCodeParameters)
    {
        // Implementation to override parameters
    }
    
    public void OverrideParam(Dictionary<string, string> param)
    {
        var usedKeys = new List<string>();

        foreach (var item in param)
        {
            string key = item.Key;
            string val = item.Value;

            if (Param.ContainsKey(key)) // Check if the key exists in the parameters
            {
                // Parsing the value; in C#, this might need to be adjusted based on the expected type.
                // Example: int parsedValue = int.Parse(val); for integer parameters.
                // Here, a generic approach with Convert.ChangeType is used for simplicity.
                object parsedValue = null;
                try
                {
                    Type targetType = Param[key].GetType();
                    parsedValue = Convert.ChangeType(val, targetType);
                }
                catch (Exception ex)
                {
                    throw new ArgumentException($"Invalid value for '{key}': {val}", ex);
                }

                if (parsedValue.GetType() != Param[key].GetType())
                {
                    throw new ArgumentException($"Invalid type for '{key}': expected: {Param[key].GetType()}, got: {parsedValue.GetType()}");
                }

                Param[key] = parsedValue;
                usedKeys.Add(key);
            }
        }

        // Remove the used keys from the original parameter dictionary
        foreach (var key in usedKeys)
        {
            param.Remove(key);
        }
    }

    public void Fetch()
    {
        int pc = InstrBuff.Pointer; // Assuming InstrBuff is a property of type InstrBuffer with a Pointer property

        FU branchUnit = null;
        foreach (var unit in FunctionalUnits)
        {
            if (unit.InstructionSet.Contains(InstrTypes.BEQ)) // Assuming InstructionSet is a property of type HashSet<InstrTypes>
            {
                branchUnit = unit; // Assuming each unit has a BranchUnit property or similar
                break;
            }
        }

        if (branchUnit == null)
        {
            throw new NotImplementedException("There is not functional unit for branches!");
        }

        int btbIdx = pc % branchUnit.BtbLimit; // Assuming branchUnit has a BtbLimit property

        // Assuming branchUnit has a BtbTable property which is an array or list of BTBEntry objects
        var btbEntry = branchUnit.BtbTable[btbIdx];
        if (btbEntry.Lookup != null && btbEntry.Lookup == pc && btbEntry.Value) // Assuming BTBEntry has Lookup and Value properties
        {
            InstrBuff.PointerNext = btbEntry.Target; // Assuming BTBEntry has a Target property
        }
        else
        {
            InstrBuff.PointerNext = pc + 1;
        }
    }

    
    public void Issue()
    {
        if (BranchCorrection())
        {
            return;
        }

        var currInstr = InstrBuff.PointerPeek();
        AssignFunctionalUnit();

        if (currInstr == null)
        {
            return;
        }

        if (currInstr.FunctionalUnit == null)
        {
            throw new NotImplementedException($"\"{currInstr.Type}\" instructions have not been implemented for any existing functional units!");
        }

        if (currInstr.Type.RequiresRob && Rat.Rob.IsFull()) // Assuming Type has RequiresRob property and Rob has IsFull method
        {
            return;
        }   

        if (currInstr.FunctionalUnit.IsFull())
        {
            if (LsUnit.InstructionSet.Contains(currInstr.Type)) // Assuming LsUnit has InstructionSet property
            {
                var head = LsUnit.LoadStoreQueue.FirstOrDefault(); // Assuming LoadStoreQueue is a property
                if (head != null)
                {
                    if ((head.Type == InstrTypes.SD && (head.PipelineTimings.Mem == null || head.PipelineTimings.Mem.FinishCycle >= Cycle)) ||
                        (head.Type == InstrTypes.LD && (head.PipelineTimings.WriteBack == null || head.PipelineTimings.WriteBack >= Cycle)))
                    {
                        return;
                    }
                    LsUnit.LoadStoreQueue.Dequeue(); // Assuming LoadStoreQueue is a Queue
                }
            }
            else
            {
                return;
            }
        }

        currInstr = InstrBuff.PointerPop(); // Assuming PointerPop method pops and returns the current instruction
        currInstr.PipelineTimings.Issue = Cycle; // Assuming PipelineTimings has an Issue property
        currInstr.FunctionalUnit.Decode(currInstr); // Assuming FunctionalUnit has Decode method
        currInstr.FunctionalUnit.Issue(currInstr); // Assuming FunctionalUnit has Issue method

        FU branchUnit = null;
        foreach (var unit in FunctionalUnits)
        {
            if (unit.InstructionSet.Contains(InstrTypes.BEQ)) // Assuming InstructionSet is a property
            {
                branchUnit = unit; // Assuming each unit has a BranchUnit property
                break;
            }
        }

        if (branchUnit == null)
        {
            throw new NotImplementedException("There is no functional unit for branches!");
        }

        if (branchUnit.InstructionSet.Contains(currInstr.Type))
        {
            branchUnit.Predict(currInstr); // Assuming Predict is a method
            Rat.BackupToIdx(currInstr.Cycle); // Assuming BackupToIdx is a method
        }
    }
    public void AssignFunctionalUnit()
    {
        var instr = InstrBuff.PointerPeak(); // Assuming InstrBuff is a property of type InstrBuffer with a PointerPeak method
        if (instr == null)
        {
            return;
        }

        var validUnits = new List<FU>();

        foreach (var unit in FunctionalUnits) // Assuming FunctionalUnits is a List<FunctionalUnit>
        {
            if (!unit.InstructionSet.Contains(instr.Type)) // Assuming InstructionSet is a HashSet<InstrTypes>
            {
                continue;
            }
            validUnits.Add(unit);
        }

        if (instr.Type == InstrTypes.LD || instr.Type == InstrTypes.SD)
        {
            validUnits.Sort((x, y) => x.LoadStoreQueue.Count.CompareTo(y.LoadStoreQueue.Count)); // Assuming LoadStoreQueue is a property
        }
        else
        {
            validUnits.Sort((x, y) => x.ReservationStation.Count.CompareTo(y.ReservationStation.Count)); // Assuming ReservationStation is a property
        }

        if (validUnits.Any())
        {
            instr.FunctionalUnit = validUnits.First(); // Assuming FunctionalUnit is a property of the instruction
        }
        else
        {
            // Handle the case where no valid units are found, if necessary
        }
    }
    
    
    public bool BranchCorrection()
    {
        Instruction instruction = null;

        foreach (var instr in InstrBuff.History) // Assuming InstrBuff has a History property
        {
            FU branchUnit = null;
            foreach (var unit in FunctionalUnits) // Assuming FunctionalUnits is a List<FunctionalUnit>
            {
                if (unit.InstructionSet.Contains(InstrTypes.BEQ)) // Assuming InstructionSet is a HashSet<InstrTypes>
                {
                    branchUnit = unit.BranchUnit; // Assuming BranchUnit is a property of FunctionalUnit
                    break;
                }
            }

            if (branchUnit == null)
            {
                throw new NotImplementedException("There is no functional unit for branches!");
            }

            if (instr.Type.InstructionSet.Contains(branchUnit.Type)) // Assuming Type is a property of Instruction
            {
                if (instr.BranchData.ContainsKey("branch_correction") && instr.BranchData["branch_correction"]) // Assuming BranchData is a Dictionary
                {
                    if (instr.PipelineTimings.Exec.FinishCycle < Cycle) // Assuming PipelineTimings has Exec property with FinishCycle
                    {
                        instruction = instr;
                        break;
                    }
                }
            }
        }

        if (instruction == null)
        {
            return false;
        }

        foreach (var instr in InstrBuff.History.Skip(instruction.Cycle + 1)) // Assuming History is a List<Instruction>
        {
            instr.Exec = false; // Assuming Exec is a property of Instruction
            if (LsUnit.InstructionSet.Contains(instr.Type)) // Assuming LsUnit has InstructionSet property
            {
                if (instr.FunctionalUnit.LoadStoreQueue.Contains(instr) || instr.FunctionalUnit.OutBuff.Contains(instr)) // Assuming FunctionalUnit has LoadStoreQueue and OutBuff
                {
                    instr.FunctionalUnit.Remove(instr); // Assuming FunctionalUnit has Remove method
                }
            }
            else
            {
                if (instr.FunctionalUnit.ReservationStation.Contains(instr) || instr.FunctionalUnit.OutBuff.Contains(instr))
                {
                    instr.FunctionalUnit.Remove(instr);
                }
            }
        }

        Rat.RestoreFromBackup(instruction.Cycle); // Assuming Rat has RestoreFromBackup method
        InstrBuff.Pointer = instruction.Result; // Assuming Pointer and Result are properties
        instruction.BranchData.Remove("branch_correction"); // Assuming BranchData is a Dictionary

        return true;
    }
    
    
    public void Execute()
    {
        foreach (var unit in FunctionalUnits) // Assuming FunctionalUnits is a List<FunctionalUnit>
        {
            unit.StepExec(Cycle); // Assuming each FunctionalUnit has a StepExec method
        }
    }

    public void Memory()
    {
        foreach (var unit in FunctionalUnits)
        {
            unit.StepMemory(Cycle); // Assuming each FunctionalUnit has a StepMemory method
        }
    }

    public void WriteBack()
    {
        foreach (var unit in FunctionalUnits)
        {
            unit.StepWriteback(Cycle); // Assuming each FunctionalUnit has a StepWriteback method
        }

        Cdb.StepExec(Cycle); // Assuming Cdb is an instance of a class with a StepExec method
    }

    public void Commit()
    {
        foreach (var instr in this.instrBuff.History)
        {
            if (!instr.Exec)
            {
                continue;
            }

            if (instr.PipelineTimings.Commit != null && instr.PipelineTimings.Commit.Item1 < this.Cycle)
            {
                continue;
            }

            if (instr.Prev != null && (instr.Prev.PipelineTimings.Commit == null || instr.Prev.PipelineTimings.Commit.Item2 >= this.Cycle))
            {
                continue;
            }

            if (instr.PipelineTimings.WriteBack == null)
            {
                return;
            }

            if (instr.PipelineTimings.WriteBack != SkipTag && instr.PipelineTimings.WriteBack >= this.Cycle)
            {
                return;
            }

            int completeCycle;
            if (instr.FunctionalUnit == this.LsUnit && instr.PipelineTimings.Mem != null)
            {
                completeCycle = instr.PipelineTimings.Mem.Item2;
            }
            else
            {
                completeCycle = instr.ComputationCompletion;
            }

            if (completeCycle == null || completeCycle >= this.Cycle)
            {
                return;
            }

            instr.PipelineTimings.Commit = new Tuple<int, int>(this.Cycle, this.Cycle);

            if (instr.Type == InstrTypes.BEQ || instr.Type == InstrTypes.BNE)
            {
                this.Rat.RemoveBackup(instr.Cycle);
            }

            if (instr.Dest != NullTag && instr.Dest != SkipTag)
            {
                this.Rat.CommitRob(instr.Dest);
            }

            return;
        }
    }

    public bool IsWorking()
    {
        if (this.InstrBuff.PointerPeak() != null)
        {
            return true;
        }

        if (this.FunctionalUnits.Any(unit => !unit.IsEmpty()))
        {
            return true;
        }

        foreach (var instr in this.InstrBuff.History.Reverse())
        {
            if (instr.Exec && (instr.PipelineTimings.Commit == null || instr.PipelineTimings.Commit.Item2 >= this.Cycle))
            {
                return true;
            }
        }

        return false;
    }


}
