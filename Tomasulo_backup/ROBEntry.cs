namespace Tomasulo_backup;
using System;

public class ROBEntry
{
    public string Dest { get; set; }
    public object Value { get; private set; }
    public bool Finished { get; private set; }
    public string Idx { get; set; }

    public ROBEntry(string dest, object value = null, bool finished = false, string idx = "")
    {
        Dest = dest;
        Value = value;
        Finished = finished;
        Idx = idx;
    }

    public ROBEntry SetVal(object val)
    {
        Finished = true;
        Value = val;
        return this;
    }

    public override string ToString()
    {
        string output = $"{Dest} -> {Idx}";
        if (Finished)
        {
            return $"{output}: {Value}";
        }
        else
        {
            return $"{output}: n/a";
        }
    }
}