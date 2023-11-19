namespace Tomasulo_backup;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public class RAT
{
    private readonly int intRegLen;
    private readonly int floatRegLen;
    private readonly int robLen;
    private readonly IntRegister intReg;
    private readonly FPRegister floatReg;
    private readonly ROB rob;
    private readonly Dictionary<int, Dictionary<string, string>> logDict;
    private readonly Dictionary<int, Dictionary<string, string>> cycleLog;
    

    public RAT(int intRegLen, int floatRegLen, int robLen)
    {
        this.intRegLen = intRegLen;
        this.floatRegLen = floatRegLen;
        this.robLen = robLen;
        intReg = new IntRegister(intRegLen);
        floatReg = new FPRegister(floatRegLen);
        rob = new ROB(robLen);
        logDict = new Dictionary<int, Dictionary<string, string>> { { -1, new Dictionary<string, string>() } };
        cycleLog = new Dictionary<int, Dictionary<string, string>>();
    }

    public Dictionary<string, string> RatTable
    {
        get => logDict[-1];
        set => logDict[-1] = value;
    }

    private (object, int) GetIdx(string idx, bool useRef = true, bool raiseError = true)
    {
        if (logDict[-1].ContainsKey(idx) && idx != logDict[-1][idx] && useRef)
        {
            return GetIdx(logDict[-1][idx], useRef, raiseError);
        }

        if (Regex.IsMatch(idx, @"R[0-9]+"))
        {
            return (intReg, int.Parse(idx.Substring(1)));
        }
        if (Regex.IsMatch(idx, @"F[0-9]+"))
        {
            return (floatReg, int.Parse(idx.Substring(1)));
        }
        if (Regex.IsMatch(idx, @"ROB[0-9]+"))
        {
            return (rob, int.Parse(idx.Substring(3)));
        }
        if (raiseError)
        {
            throw new ArgumentException($"Invalid Index: {idx}");
        }
        return (null, -1);
    }

    public object GetVal(string idx, bool raiseError = true)
    {
        var (table, tableIdx) = GetIdx(idx, raiseError);
        if (!raiseError && table == null)
        {
            return null;
        }

        if (table is Register<ROBEntry> robRegister)
        {
            var entry = robRegister[tableIdx];
            if (entry != null && entry.Finished)
            {
                return entry.Value;
            }
        }
        else if (table is Register<int> intRegister)
        {
            return intRegister[tableIdx];
        }
        else if (table is Register<float> floatRegister)
        {
            return floatRegister[tableIdx];
        }

        if (raiseError && (table == null || tableIdx == -1))
        {
            throw new ArgumentException($"Unexpected Return of None: {idx} at table index = {tableIdx}");
        }

        return null; 
    }


    public bool IsAvailable(string idx)
    {
        var (table, tableIdx) = GetIdx(idx, true, true);
        if (table is ROB)
        {
            return rob.IsFinished(tableIdx);
        }
        return true; 
    }

    public string ReserveRob(string regIdx)
    {
        if (rob.IsFull())
        {
            throw new InvalidOperationException("ROB is full");
        }
        var (robIdx, _) = rob.Reserve(regIdx);
        logDict[-1][regIdx] = robIdx;
        return robIdx;
    }

    public void SetRobVal(string robIdx, ROBEntry val) // int val
    {
        var (table, tableIdx) = GetIdx(robIdx);
        if (table != rob)
        {
            throw new InvalidOperationException($"Invalid index when attempting to set ROB!: {robIdx}");
        }

        rob.SetVal(tableIdx, val);
    }

    public Dictionary<int, Dictionary<string, string>> CommitRob(string robIdx, bool writeBack = true)
    {
        var (table, tableIdx) = GetIdx(robIdx);
        if (table != rob)
        {
            throw new InvalidOperationException($"Invalid index when attempting to commit ROB!: {robIdx}");
        }

        var robEntry = rob[tableIdx] as ROBEntry;
        if (robEntry == null)
        {
            throw new InvalidOperationException("ROB entry not found or invalid.");
        }

        foreach (var key in logDict.Keys.ToList())
        {
            if (logDict[key].ContainsKey(robEntry.Dest) && robIdx == logDict[key][robEntry.Dest])
            {
                logDict[key][robEntry.Dest] = robEntry.Dest;
            }

            if (logDict[key].ContainsKey(robEntry.Dest) && logDict[key][robEntry.Dest] == robEntry.Dest)
            {
                logDict[key].Remove(robEntry.Dest);
            }
        }

        if (writeBack)
        {
            var (reg, regIdx) = GetIdx(robEntry.Dest, false);
            if (reg is Register<int> intReg) 
            {
                intReg[regIdx] = Convert.ToInt32(robEntry.Value); 
            }
            else if (reg is Register<float> floatReg)
            {
                floatReg[regIdx] = Convert.ToSingle(robEntry.Value);
            }
        }

        rob.Remove(tableIdx); 
        return new Dictionary<int, Dictionary<string, string>>(logDict); 
    }

    public string PrintInfo()
    {
        string output = "";
        output += "--- Integer Registers ---\n";
        output += intReg.PrintInfo();
        output += "--- Float Registers ---\n";
        output += floatReg.PrintInfo(); 
        output += "--- ROB ---\n";
        output += rob.PrintInfo(); 
        output += "--- RAT ---\n";

        string ratOut = "";
        var sortedKeys = RatTable.Keys.OrderBy(k => k);
        int ctr = 0;

        foreach (var key in sortedKeys)
        {
            string reg = key.PadLeft(5);
            ratOut += $"{reg}: {RatTable[key]}".PadRight(18);
            ctr++;
            if (ctr % 8 == 0)
            {
                ratOut += "\n";
            }
        }

        if (string.IsNullOrEmpty(ratOut) || ratOut[^1] != '\n')
        {
            ratOut += "\n";
        }

        return output + ratOut;
    }


    public void BackupToIdx(int idx)
    {
        logDict[idx] = new Dictionary<string, string>(RatTable);
    }

    public void RestoreFromBackup(int idx)
    {
        RatTable = new Dictionary<string, string>(logDict[idx]);

        foreach (var entry in rob.data)
        {
            if (entry == null) continue;
            if (!RatTable.ContainsKey(entry.Dest))
            {
                rob.Remove(entry.Idx); 
            }
        }

        var keysToRemove = logDict.Keys.Where(k => k > idx).ToList();
        foreach (var key in keysToRemove)
        {
            logDict.Remove(key);
        }
    }

    public void RemoveBackup(int idx)
    {
        logDict.Remove(idx);
    }

    public void InitializeValues(Dictionary<string, string> param)
    {
        var usedKeys = new List<string>();
        foreach (var kvp in param)
        {
            var (reg, regIdx) = GetIdx(kvp.Key, false, false);
            try
            {
                if (reg is IntRegister)
                {
                    ((IntRegister)reg)[regIdx] = int.Parse(kvp.Value);
                    usedKeys.Add(kvp.Key);
                }
                else if (reg is FPRegister)
                {
                    ((FPRegister)reg)[regIdx] = float.Parse(kvp.Value);
                    usedKeys.Add(kvp.Key);
                }
            }
            catch
            {
                throw new ArgumentException($"Invalid value for {kvp.Key}: {kvp.Value}");
            }
        }

        foreach (var key in usedKeys)
        {
            param.Remove(key);
        }
    }
}
