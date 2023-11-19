namespace Tomasulo_backup;
using System;
using System.Collections.Generic;

public class Branch
{
    private static readonly HashSet<InstrTypes> instructionSet = new HashSet<InstrTypes>
    {
        InstrTypes.BEQ,
        InstrTypes.BNE
    };

    private static readonly List<InstrTypes> requireRob = new List<InstrTypes>();
    public FU Parent { get; private set; }
    public int BtbLimit { get; private set; }
    public Dictionary<int, BTBEntry> BtbTable { get; private set; }

    public Branch(FU function, int btbLimit = 8)
    {
        Parent = function;
        if (Math.Log(btbLimit, 2) != (int)Math.Log(btbLimit, 2))
        {
            throw new ArgumentException("Attempted to initialize the BTB to have a number of entries that is not a power of 2!");
        }

        BtbLimit = btbLimit;
        BtbTable = new Dictionary<int, BTBEntry>();

        for (int i = 0; i < BtbLimit; i++)
        {
            BtbTable.Add(i, new BTBEntry());
        }
    }
    
    public Instruction Decode(Instruction instruction)
    {
        instruction.Operands[0] = this.Parent.Rat.GetVal(instruction.Operands[0]);
        instruction.Operands[1] = this.Parent.Rat.GetVal(instruction.Operands[1]);
        instruction.Operands[2] = Convert.ToInt32(instruction.Operands[2]);
        instruction.BranchData["branch_prediction_accurate"] = null;
        instruction.Dest = SkipTag; // Assuming SkipTag is a predefined constant
        instruction.PipelineTimings.Mem = SkipTag;
        instruction.PipelineTimings.WriteBack = SkipTag;

        return instruction;
    }

    public void PerformAction(int cycle, Instruction instruction)
    {
        bool equality = instruction.Operands[0] == instruction.Operands[1];
        instruction.Result = instruction.Idx + 1;

        if (instruction.Type == InstrTypes.BEQ && equality)
        {
            instruction.Result += instruction.Operands[2];
        }

        if (instruction.Type == InstrTypes.BNE && !equality)
        {
            instruction.Result += instruction.Operands[2];
        }

        CheckPrediction(instruction, equality);
    }

    private void CheckPrediction(Instruction instruction, bool equality)
    {
        // Implementation of check_prediction method
    }

    public bool IsBusy()
    {
        return this.Parent.ReservationStation.Count > this.Parent.ReservationStationLen;
    }
    
    private void CheckPrediction(Instruction instruction, bool equality)
    {
        instruction.BranchData["branch_prediction_accurate"] = true; // Assume correct prediction initially
        int index = instruction.Idx % BtbLimit;

        if (instruction.Type == InstrTypes.BEQ)
        {
            HandleBeqInstruction(instruction, equality, index);
        }
        else if (instruction.Type == InstrTypes.BNE)
        {
            HandleBneInstruction(instruction, equality, index);
        }
    }

    private void HandleBeqInstruction(Instruction instruction, bool equality, int index)
    {
        if (equality) // Should have taken the branch
        {
            if (instruction.BranchData["branch_prediction"])
                return;

            UpdateBtbEntry(instruction, index, true);
        }
        else // Should not have taken the branch
        {
            if (!instruction.BranchData["branch_prediction"])
                return;

            ClearBtbEntry(instruction, index);
        }
    }

    private void HandleBneInstruction(Instruction instruction, bool equality, int index)
    {
        if (!equality) // Should have taken the branch
        {
            if (instruction.BranchData["branch_prediction"])
                return;

            UpdateBtbEntry(instruction, index, true);
        }
        else // Should not have taken the branch
        {
            if (!instruction.BranchData["branch_prediction"])
                return;

            ClearBtbEntry(instruction, index);
        }
    }

    private void UpdateBtbEntry(Instruction instruction, int index, bool value)
    {
        BtbTable[index].Value = value;
        BtbTable[index].Target = instruction.Result;
        BtbTable[index].Lookup = instruction.Idx;
        instruction.BranchData["branch_prediction_accurate"] = false;
        instruction.BranchData["branch_correction"] = true;
    }

    private void ClearBtbEntry(Instruction instruction, int index)
    {
        BtbTable[index].Value = false;
        BtbTable[index].Target = null;
        BtbTable[index].Lookup = null;
        instruction.BranchData["branch_prediction_accurate"] = false;
        instruction.BranchData["branch_correction"] = true;
    }
    
    public int Predict(Instruction instruction)
    {
        if (instruction.Type != InstrTypes.BEQ && instruction.Type != InstrTypes.BNE)
        {
            throw new InvalidOperationException("Instruction type is not a branch");
        }

        int numInt = instruction.Idx % BtbLimit;
        instruction.BranchData["branch_prediction"] = BtbTable[numInt].Value;

        if (instruction.BranchData["branch_prediction"])
        {
            return Convert.ToInt32(instruction.Operands[2]);
        }

        return 0;
    }
}
