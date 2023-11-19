namespace Tomasulo_backup;
using System;

public class TestedOutcome
{
    public bool Result { get; set; }
    public string Key { get; set; }
    public object CheckValue { get; set; }
    public object Value { get; set; }

    public override string ToString()
    {
        string formattedValue = $"{Value}".PadRight(9);
        string resultString = Result ? "==" : "!=";
        string status = Result ? "Passed!" : "Failed!";
        return $"{Key}: {CheckValue} {resultString} {formattedValue}".PadRight(31) + $" - {status}";
    }
}
