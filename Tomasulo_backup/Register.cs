using System;
using System.Collections.Generic;

public class Register<G>
{
    public List<G> data;
    public int length;
    // private readonly List<G> data;
    // private readonly int length;

    public Register(int length, G initialValue = default(G))
    {
        if (length < 0)
        {
            throw new ArgumentException($"A register cannot have a length less than 0!: length = {length}");
        }

        this.length = length;
        this.data = new List<G>(new G[length + 1]);

        for (int i = 0; i <= length; i++)
        {
            this.data[i] = initialValue;
        }
    }

    public G this[int key]
    {
        get
        {
            if (key > this.length)
            {
                throw new IndexOutOfRangeException($"Attempted to read value in register outside of bounds!: {key} > {this.length}");
            }

            return this.data[key];
        }
        set
        {
            if (key >= this.data.Count)
            {
                throw new IndexOutOfRangeException($"Attempted to write value to register outside of bounds!: {key} >= {this.length}");
            }

            if (key != 0)
            {
                this.data[key] = value;
            }
        }
    }

    public G Get(int idx)
    {
        return this[idx];
    }
    
    public int Length
    {
        get => this.length;
    }

    public virtual string PrintInfo()
    {
        string output = "";
        for (int i = 0; i < this.data.Count; i++)
        {
            if (i == 0)
            {
                continue;
            }

            string idx = i.ToString().PadLeft((int)Math.Ceiling(Math.Log10(this.length)));
            string val = this.data[i].ToString();
            output += $"{idx}: {val}".PadRight(15);

            if (i % 8 == 0)
            {
                output += Environment.NewLine;
            }
        }

        if (output[output.Length - 1] != '\n')
        {
            output += Environment.NewLine;
        }

        return output;
    }
}