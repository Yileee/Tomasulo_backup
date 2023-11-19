namespace Tomasulo_backup;
using System;
using System.Collections.Generic;

public class ROB : Register<ROBEntry>
{
    private int head;

    public ROB(int length) : base(length, null)
    {
        head = 1;
    }

    public ROBEntry SetVal(int idx, ROBEntry val)
    {
        this[idx].SetVal(val);
        return this[idx];
    }

    public void Remove(int idx)
    {
        this[idx] = null;
    }

    public int? FindFreeIndex()
    {
        if (data.FindIndex(1, i => i == null) == -1)
        {
            return null;
        }

        while (true)
        {
            head %= data.Count;
            if (head == 0)
            {
                head++;
            }

            if (data[head] == null)
            {
                return head;
            }

            head++;
        }
    }

    public bool IsFull()
    {
        return FindFreeIndex() == null;
    }

    public (string, ROBEntry) Reserve(string regIdx)
    {
        var freeIdx = FindFreeIndex();
        if (!freeIdx.HasValue)
        {
            throw new Exception("ROB is full!");
        }

        head++;
        var entry = new ROBEntry(dest: regIdx, idx: $"ROB{freeIdx}");
        data[freeIdx.Value] = entry;
        return ($"ROB{freeIdx}", entry);
    }

    public bool IsFinished(int idx)
    {
        return data[idx].Finished;
    }

    public string PrintInfo()
    {
        int ctr = 1;
        string output = "";

        for (int i = 0; i < data.Count; i++)
        {
            var j = data[i];
            if (j != null)
            {
                string idx = i.ToString().PadLeft((int)Math.Ceiling(Math.Log10(Length)));
                string dest = j.Dest.PadRight(3);
                string valString = j.Finished ? j.Value.ToString() : "n/a";
                output += $"{idx}: {dest} -> {valString}".PadRight(16);

                ctr++;
                if (ctr % 8 == 0)
                {
                    output += Environment.NewLine;
                }
            }
        }

        if (string.IsNullOrEmpty(output) || output[output.Length - 1] != '\n')
        {
            output += Environment.NewLine;
        }

        return output;
    }
}
