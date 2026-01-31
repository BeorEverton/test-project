using System;
using UnityEngine;

[Serializable]
public class BalanceFormulaConfig
{
    public int version = 1;

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
