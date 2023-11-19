namespace Tomasulo_backup;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public class InstructionBuffer
{
    public List<Instruction> FullCode { get; private set; } = new List<Instruction>();
    public Dictionary<string, string> CodeParameters { get; private set; } = new Dictionary<string, string>();
    public int Counter { get; private set; } = 0;
    public int Pointer { get; private set; } = 0;
    public int PointerNext { get; private set; } = 1;
    public List<Instruction> History { get; private set; } = new List<Instruction>();

    public InstrBuffer AppendCode(string codeStr)
    {
        List<Instr> parserCode = new List<Instr>();
        string newCodeStr = "";
        string[] lines = codeStr.Split('\n');

        for (int idx = 0; idx < lines.Length; idx++)
        {
            string line = lines[idx].Trim();

            if (string.IsNullOrEmpty(line))
                continue;

            if (idx == 0)
            {
                if (Regex.IsMatch(line, "# of rs", RegexOptions.IgnoreCase))
                    continue;
                else
                    break;
            }

            // Handle different cases, e.g., "INTEGER ADDER", "FP ADDER", etc.
            // Each case follows a similar pattern to the Python code, but using C#'s Regex methods
            // For example:
            if (Regex.IsMatch(line, "INTEGER ADDER", RegexOptions.IgnoreCase))
            {
                MatchCollection matches = Regex.Matches(line.ToUpper(), @"\d+");
                if (matches.Count >= 3)
                {
                    newCodeStr += $"$ int_adder_reservation_stations = {matches[0].Value}\n";
                    newCodeStr += $"$ int_adder_exec_cycles = {matches[1].Value}\n";
                    newCodeStr += $"$ int_adder_num_units = {matches[2].Value}\n";
                }
                continue;
            }

            // ... [Handle other cases similarly]

            // Final formatting of the line
            line = Regex.Replace(line, ",\\s*", ", ");
            line = line.Replace(".", "");
            newCodeStr += $"{line}\n";
        }

        if (newCodeStr.Length > 0)
            codeStr = newCodeStr;

        // Handle the second loop for parsing instructions
        // This part is similar to the Python code, but adapted to C# syntax and conventions

        return this;
    }

    public Instruction PointerPop()
    {
        if (PointerPeak() == null)
            return null;

        Instruction instr = new Instruction(PointerPeak()); // Assuming a constructor or method to copy an Instr
        instr.Cycle = Counter;

        foreach (var i in History.ToArray().Reverse())
        {
            if (i.Exec)
            {
                instr.Prev = i;
                break;
            }
        }

        Counter++;
        Pointer = PointerNext;

        History.Add(instr);
        return instr;
    }

    public Instruction PointerPeak()
    {
        if (Pointer >= FullCode.Count)
            return null;

        return FullCode[Pointer];
    }

    public string PrintHistoryTable()
    {
        return PrintInstructions(History);
    }

    private string PrintInstructions(List<Instruction> instructions)
    {
        // Assuming `Instr` has a suitable ToString method or similar for representing an instruction
        string result = "";
        foreach (var instr in instructions)
        {
            result += instr.ToString() + Environment.NewLine;
        }
        return result;
    }
}
