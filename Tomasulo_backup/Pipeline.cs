namespace Tomasulo_backup;
using System;

public class PipelineTimings
{
    public static readonly string[] Rows = { "Issue", "Execute", "Memory", "Write Back", "Commit" };

    public int? Issue { get; set; }
    public Tuple<int?, int?> Exec { get; set; }
    public Tuple<int?, int?> Mem { get; set; }
    public int? WriteBack { get; set; }
    public Tuple<int?, int?> Commit { get; set; }

    public PipelineTimings()
    {
        Issue = null;
        Exec = null;
        Mem = null;
        WriteBack = null;
        Commit = null;
    }

    public string[] PrintStr()
    {
        string[] z = new string[5];
        var values = new object[] { Issue, Exec, Mem, WriteBack, Commit };

        for (int i = 0; i < values.Length; i++)
        {
            var v = values[i];
            if (v is Tuple<int?, int?> tuple)
            {
                if (tuple.Item1 == tuple.Item2)
                {
                    z[i] = tuple.Item1.ToString();
                }
                else
                {
                    z[i] = $"{tuple.Item1} - {tuple.Item2}";
                }
            }
            else if (v == null)
            {
                z[i] = "null";
            }
            else
            {
                z[i] = v.ToString();
            }
        }

        return z;
    }

    public override string ToString()
    {
        return $"{Issue}, {Exec}, {Mem}, {WriteBack}, {Commit}";
    }
}
