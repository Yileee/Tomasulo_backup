namespace Tomasulo_backup;
using System;

public class FPRegister : Register<float>
{
    public FPRegister(int length, float initialValue = 0.0f) : base(length, initialValue)
    {
    }

    public new float this[int key]
    {
        get => base[key];
        set => base[key] = Convert.ToSingle(value);
    }

    public override string PrintInfo()
    {
        string output = "";
        for (int i = 0; i < this.Length; i++)
        {
            string idx = $"F{i.ToString().PadLeft((int)Math.Ceiling(Math.Log10(this.Length)))}";
            float val = this.Get(i);
            string valString = (val == (int)val) ? val.ToString() : val.ToString("0.0000");
            output += $"{idx}: {valString}".PadRight(15);

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