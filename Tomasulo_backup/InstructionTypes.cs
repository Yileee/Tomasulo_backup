namespace Tomasulo_backup;
using System;

public enum InstrTypes
{
    Nop,
    Ld,
    Sd,
    Beq,
    Bne,
    Add,
    Sub,
    Addi,
    Subi,
    Addd,
    Subd,
    Multd,
    Divd
}

public static class InstrTypesExtensions
{
    public static string GetStringValue(this InstrTypes instrType)
    {
        switch (instrType)
        {
            case InstrTypes.Nop:
                return "NOP";
            case InstrTypes.Ld:
                return "L.D";
            case InstrTypes.Sd:
                return "S.D";
            case InstrTypes.Beq:
                return "BEQ";
            case InstrTypes.Bne:
                return "BNE";
            case InstrTypes.Add:
                return "ADD";
            case InstrTypes.Sub:
                return "SUB";
            case InstrTypes.Addi:
                return "ADDI";
            case InstrTypes.Subi:
                return "SUBI";
            case InstrTypes.Addd:
                return "FADD.D";
            case InstrTypes.Subd:
                return "FSUB.D";
            case InstrTypes.Multd:
                return "FMULT.D";
            case InstrTypes.Divd:
                return "FDIV.D";
            default:
                throw new ArgumentOutOfRangeException(nameof(instrType), instrType, null);
        }
    }
}