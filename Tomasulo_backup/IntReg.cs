using System;

public class IntReg : Register<int>
{
    public IntReg(int length, int initialValue = 0) : base(length, initialValue)
    {
    }

    public new int this[int key]
    {
        get => base[key];
        set => base[key] = Convert.ToInt32(value);
    }

    public override string PrintInfo()
    {
        string output = "";
        for (int i = 0; i < this.Length; i++)
        {
            string idx = $"R{i.ToString().PadLeft((int)Math.Ceiling(Math.Log10(this.Length)))}";
            string val = this.Get(i).ToString();
            output += $"{idx}: {val}".PadRight(15);

            if (i % 8 == 7)
            {
                output += Environment.NewLine;
            }
        }

        if (string.IsNullOrEmpty(output) || output[output.Length - 1] != '\n')
        {
            output += Environment.NewLine;
        }

        return output;
    }
}