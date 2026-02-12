using System;
using UnityEngine;

[Serializable]
public class BalanceFormulaConfig
{
    public int version = 1;

    // Human-readable help don't edit this
    public string help =
        "FORMULA TYPES (FormulaType):\n" +
        "  Constant: multiplier = a\n" +
        "  Linear:   multiplier = a + b*x\n" +
        "  Power:    multiplier = a * x^p\n" +
        "  Exponential: multiplier = a * r^x\n" +
        "  LinearPlusPower: multiplier = a + b * x^p\n" +
        "\n" +
        "FIELDS:\n" +
        "  a: main coefficient (usually base multiplier)\n" +
        "  b: slope (Linear) OR coefficient for x^p (LinearPlusPower)\n" +
        "  p: exponent (Power / LinearPlusPower)\n" +
        "  r: exponential base (Exponential)\n" +
        "\n" +
        "x MEANING:\n" +
        "  For enemy formulas: x = globalWaveIndex (1..N)\n" +
        "  For upgrade formulas: x = upgradeLevel (0..N)\n" +
        "\n" +
        "NEUTRAL BASELINE:\n" +
        "  Use Constant with a=1.0 to make a formula do nothing.\n" +
        "  Example: { \"type\":\"Constant\", \"a\": 1.0 }\n";


    public EnemyGlobalFormula enemy = new EnemyGlobalFormula();
    public UpgradeCostGlobalFormula upgrades = new UpgradeCostGlobalFormula();

    [Serializable]
    public class EnemyGlobalFormula
    {
        // Global multipliers applied on top of per-enemy wave scaling (1 = no change).
        public FormulaDefinition healthMultiplier = FormulaDefinition.Constant(1f);
        public FormulaDefinition damageMultiplier = FormulaDefinition.Constant(1f);
        public FormulaDefinition coinMultiplier = FormulaDefinition.Constant(1f);
    }

    [Serializable]
    public class UpgradeCostGlobalFormula
    {
        public FormulaDefinition costMultiplier = FormulaDefinition.Constant(1f);
    }
}

[Serializable]
public class FormulaDefinition
{
    public FormulaType type = FormulaType.Constant;

    // coefficients / params (not all are used for each type)
    public float a = 1f;
    public float b = 0f;
    public float p = 1f;   // exponent
    public float r = 1f;   // base for exp

    public static FormulaDefinition Constant(float value)
    {
        return new FormulaDefinition { type = FormulaType.Constant, a = value };
    }

    public float Evaluate(float x)
    {
        // x is typically: global wave index (1..N) or upgrade level (0..N)
        switch (type)
        {
            case FormulaType.Constant:
                return a;

            case FormulaType.Linear:
                // a + b*x
                return a + (b * x);

            case FormulaType.Power:
                // a * x^p
                return a * Mathf.Pow(Mathf.Max(0f, x), p);

            case FormulaType.Exponential:
                // a * r^x
                return a * Mathf.Pow(r, x);

            case FormulaType.LinearPlusPower:
                // a + b*x + (c*x^p) where c is stored in b? (keep it simple: use b as c)
                // If you want separate c, add it later. Least disruptive now.
                return a + (b * Mathf.Pow(Mathf.Max(0f, x), p));

            default:
                return 1f;
        }
    }
}

public enum FormulaType
{
    Constant,
    Linear,
    Power,
    Exponential,
    LinearPlusPower
}
