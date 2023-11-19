namespace Tomasulo_backup;
using System;

public class BTBEntry
{
    public int? Lookup { get; set; } // Nullable int to represent Union[None, int]
    public int? Target { get; set; } // Nullable int to represent Union[None, int]
    public bool Value { get; set; } = false; // Initializes as false

    public BTBEntry()
    {
        Lookup = null;
        Target = null;
    }

    public void SetTarget(int idx)
    {
        Target = idx;
    }

    public override string ToString()
    {
        string takenStatus = Value ? "Taken" : "Not Taken";
        return $"Lookup PC: {Lookup}, Predicted PC: {Target} -> {takenStatus}";
    }
}
