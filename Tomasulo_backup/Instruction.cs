namespace Tomasulo_backup;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

public class Instrction<T>
{
    public string OrigInstrStr { get; private set; }
    public int Idx { get; private set; }
    public int Cycle { get; set; } = -1;
    public InstrTypes Type { get; private set; }
    public List<string> Operands { get; private set; }
    public PipelineTimings PipelineTimings { get; private set; } = new PipelineTimings();
    public bool Exec { get; set; } = true;
    public int? ComputationCompletion { get; set; } = null;
    public object FunctionalUnit { get; set; } = null; // Replace with actual type
    public T Result { get; set; } = default(T);
    public object Dest { get; set; } = null; // Replace with actual type
    public int? MemAddr { get; set; } = null;
    public Instrction<T> Prev { get; set; } = null;
    public Dictionary<string, object> BranchData { get; private set; } = new Dictionary<string, object>();

    public Instrction(string instrStr, int idx)
    {
        if (Regex.IsMatch(instrStr, "#"))
        {
            instrStr = Regex.Match(instrStr, ".*(?=#)").Value;
        }
        instrStr = instrStr.Trim();
        string instrType = instrStr.Split(' ')[0].Replace(".", "").ToUpper();
        List<string> operands = Regex.Split(instrStr, " |\\(|\\)|,").Where(i => i.Trim().Length > 0).ToList();
        Operands = operands.Select(i => i.Trim()).ToList();

        if (!Enum.TryParse(instrType, out InstrTypes parsedType))
        {
            throw new ArgumentException($"Invalid instruction type: \"{instrType}\"");
        }
        OrigInstrStr = instrStr;
        Idx = idx;
        Type = parsedType;
    }

    public override string ToString()
    {
        string result = $"{Idx.ToString().PadLeft(3)} {Cycle.ToString().PadLeft(3)} {OrigInstrStr}";
        result += $", unit={FunctionalUnit?.GetType().Name}";
        result += $", execution={Exec}";
        result += $", result={Result}";
        result += $", event=[{PipelineTimings}]";
        if (ComputationCompletion.HasValue)
        {
            result += $", completion: {ComputationCompletion.Value}";
        }
        result += $", destination={Dest}";
        string operandsStr = string.Join(", ", Operands.Select(x => x));
        result += $", operands=[{operandsStr}]";
        if (Type == InstrTypes.Beq || Type == InstrTypes.Bne)
        {
            if (BranchData.ContainsKey("branch_prediction_accurate") && BranchData.ContainsKey("branch_prediction"))
            {
                string prediction = (bool)BranchData["branch_prediction"] ? "branch" : "continue";
                result += $", prediction ({BranchData["branch_prediction_accurate"]}): {prediction}";
            }
        }
        if (Type == InstrTypes.Ld || Type == InstrTypes.Sd)
        {
            result += $", address: {MemAddr}";
        }
        if (!Exec)
        {
            result += ", mispredicted";
        }
        return result;
    }
}
