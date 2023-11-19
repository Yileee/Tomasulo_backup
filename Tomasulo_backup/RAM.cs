namespace Tomasulo_backup;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public class RAM
{
    private int length;
    private int execTime;
    private List<float> data;
    private Dictionary<string, string> cycleLog;

    public RAM(int ramLen, int execTime)
    {
        this.length = ramLen;
        this.execTime = execTime;
        this.data = new List<float>(new float[ramLen]); // Initialize with zeroes
        this.cycleLog = new Dictionary<string, string>();
    }

    public float GetVal(int addr)
    {
        if (addr % 4 != 0)
        {
            throw new LookupError($"Invalid memory address: {addr} % 4 != 0");
        }
        addr /= 4;
        return this.data[addr];
    }

    public void SetVal(int addr, float val)
    {
        if (addr % 4 != 0)
        {
            throw new LookupError($"Invalid memory address: {addr} % 4 != 0");
        }
        addr /= 4;
        this.data[addr] = val;
    }

    public string PrintInfo()
    {
        string output = "--- Memory ---\n";
        int ctr = 1;
        int pad = (int)Math.Ceiling(Math.Log10(this.length * 4) / Math.Log10(16)) + 2;
        foreach (var v in this.data)
        {
            if (v != 0)
            {
                int idx = i * 4;
                string addr = $"{idx:#0{pad}X} [{idx.ToString().PadLeft((int)Math.Ceiling(Math.Log10(this.length * 4)), '0')}; W{i.ToString().PadLeft((int)Math.Ceiling(Math.Log10(this.length)), '0')}]";
                string val = v.ToString("0.0000");
                output += $"{addr}: {val}".PadRight(30);
                ctr++;
                if (ctr % 4 == 1)
                {
                    output += "\n";
                }
            }
        }
        if (!output.EndsWith("\n"))
        {
            output += "\n";
        }
        return output;
    }

    public void InitializeValues(Dictionary<string, string> param)
    {
        List<string> usedKeys = new List<string>();
        foreach (var kvp in param)
        {
            string key = kvp.Key;
            string val = kvp.Value;
            if (!Regex.IsMatch(key.ToUpper(), @"MEM\[\d+]"))
            {
                continue;
            }
            int addr = int.Parse(key.Substring(4, key.Length - 5));
            float value = float.Parse(val);
            if (addr % 4 != 0)
            {
                throw new LookupError($"Invalid memory address: {addr} % 4 != 0");
            }
            this.SetVal(addr, value);
            usedKeys.Add(key);
        }
        foreach (var key in usedKeys)
        {
            param.Remove(key);
        }
    }
}

public class LookupError : Exception
{
    public LookupError(string message) : base(message)
    {
    }
}