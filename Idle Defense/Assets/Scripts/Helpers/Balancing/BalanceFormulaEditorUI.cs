using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BalanceFormulaEditorUI : MonoBehaviour
{
    public enum TargetFormula
    {
        EnemyHealth,
        EnemyDamage,
        EnemyCoin,
        UpgradeCost
    }

    [Header("Target")]
    public TargetFormula target;

    [Header("Inputs")]
    public TMP_Dropdown typeDropdown;
    public TMP_InputField aInput;
    public TMP_InputField bInput;
    public TMP_InputField pInput;
    public TMP_InputField rInput;

    [Header("Preview")]
    public TMP_Text previewText;   // e.g. "W1=1.00 | W10=1.32 | W50=2.10 | W100=2.80"
    public TMP_Text sourcePathText; // optional: show persistent path

    [Header("Optional JSON Output")]
    public TMP_InputField jsonOutput; // optional: show current JSON for copy/paste (read-only)

    private void OnEnable()
    {
        LoadFromManager();
        UpdatePreview();
        UpdateSourcePathLabel();
    }

    public void LoadFromManager()
    {
        var fm = BalanceFormulaManager.Instance;
        if (fm == null) return;

        FormulaDefinition def = GetCurrentFormula(fm);
        if (def == null) def = FormulaDefinition.Constant(1f);

        typeDropdown.value = (int)def.type;

        RefreshFieldVisibility(def.type);
        ApplyPlaceholders(def.type);
        PopulateInputsForType(def, def.type);

        UpdatePreview();

        if (jsonOutput != null)
            jsonOutput.text = fm.ExportCurrentConfigToJson();
    }

    public void OnTypeChanged(int newValue)
    {
        FormulaType t = (FormulaType)newValue;

        RefreshFieldVisibility(t);
        ApplyPlaceholders(t);

        // Keep existing text if user already typed, otherwise let placeholders show.
        // This also avoids “0” replacing placeholders when switching types.
        FormulaDefinition def = ReadFromUI();
        PopulateInputsForType(def, t);

        UpdatePreview();
    }

    public void OnAnyValueChanged()
    {
        UpdatePreview();
    }

    public void Apply()
    {
        var fm = BalanceFormulaManager.Instance;
        if (fm == null) return;

        // 1) Read current UI values -> apply to manager
        FormulaDefinition def = ReadFromUI();
        SetCurrentFormula(fm, def);

        // 2) Apply in game (reload wave / refresh costs)
        fm.ApplyCurrentFormulasNow();

        // 3) Force UI to reflect the real stored values (and normalization behavior)
        LoadFromManager();

        // 4) Make sure preview is updated even if input events aren't wired
        UpdatePreview();

        if (jsonOutput != null)
            jsonOutput.text = fm.ExportCurrentConfigToJson();
    }

    public void SaveToJson()
    {
        var fm = BalanceFormulaManager.Instance;
        if (fm == null) return;

        // Ensure config matches UI before saving
        FormulaDefinition def = ReadFromUI();
        SetCurrentFormula(fm, def);

        fm.SaveCurrentConfigToPersistent();

        if (jsonOutput != null)
            jsonOutput.text = fm.ExportCurrentConfigToJson();

        UpdateSourcePathLabel();
    }

    private void UpdateSourcePathLabel()
    {
        if (sourcePathText == null) return;
        var fm = BalanceFormulaManager.Instance;
        if (fm == null) return;

        sourcePathText.text = "Formulas file:\n" + fm.GetPersistentPath();
    }

    private void UpdatePreview()
    {
        if (previewText == null) return;

        FormulaDefinition def = ReadFromUI();

        string xName = (target == TargetFormula.UpgradeCost) ? "level" : "wave";
        string formula = GetFormulaTemplate(def.type);

        float w1 = EvaluateDisplayed(def, 1);
        float w10 = EvaluateDisplayed(def, 10);
        float w50 = EvaluateDisplayed(def, 50);
        float w100 = EvaluateDisplayed(def, 100);

        previewText.text =
            "mult = " + formula + "   (x=" + xName + ")\n" +
            "W1=" + w1.ToString("0.###") +
            " | W10=" + w10.ToString("0.###") +
            " | W50=" + w50.ToString("0.###") +
            " | W100=" + w100.ToString("0.###");
    }

    private float EvaluateDisplayed(FormulaDefinition def, int x)
    {
        if (def == null) return 1f;

        if (target == TargetFormula.UpgradeCost)
        {
            // Upgrade cost is NOT normalized.
            float v = def.Evaluate(Mathf.Max(0, x));
            if (float.IsNaN(v) || float.IsInfinity(v)) return 1f;
            return Mathf.Max(0f, v);
        }

        // Enemy formulas: match BalanceFormulaManager normalization: f(w)/f(1)
        float raw = def.Evaluate(Mathf.Max(1, x));
        float baseAt1 = def.Evaluate(1f);

        if (Mathf.Approximately(baseAt1, 0f)) return 1f;

        float normalized = raw / baseAt1;
        if (float.IsNaN(normalized) || float.IsInfinity(normalized)) return 1f;

        return Mathf.Max(0f, normalized);
    }

    private FormulaDefinition ReadFromUI()
    {
        return new FormulaDefinition
        {
            type = (FormulaType)typeDropdown.value,
            a = ParseFloat(aInput, 1f),
            b = ParseFloat(bInput, 0f),
            p = ParseFloat(pInput, 1f),
            r = ParseFloat(rInput, 1f),
        };
    }

    private float ParseFloat(TMP_InputField f, float fallback)
    {
        if (f == null) return fallback;
        if (float.TryParse(f.text, System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out float v))
            return v;
        return fallback;
    }

    private void RefreshFieldVisibility(FormulaType type)
    {
        // Always show a
        if (aInput != null) aInput.gameObject.SetActive(true);

        // Show only fields relevant to the formula type
        if (bInput != null) bInput.gameObject.SetActive(type == FormulaType.Linear || type == FormulaType.LinearPlusPower);
        if (pInput != null) pInput.gameObject.SetActive(type == FormulaType.Power || type == FormulaType.LinearPlusPower);
        if (rInput != null) rInput.gameObject.SetActive(type == FormulaType.Exponential);
    }

    private FormulaDefinition GetCurrentFormula(BalanceFormulaManager fm)
    {
        switch (target)
        {
            case TargetFormula.EnemyHealth: return fm.Config.enemy.healthMultiplier;
            case TargetFormula.EnemyDamage: return fm.Config.enemy.damageMultiplier;
            case TargetFormula.EnemyCoin: return fm.Config.enemy.coinMultiplier;
            case TargetFormula.UpgradeCost: return fm.Config.upgrades.costMultiplier;
            default: return FormulaDefinition.Constant(1f);
        }
    }

    private void SetCurrentFormula(BalanceFormulaManager fm, FormulaDefinition def)
    {
        switch (target)
        {
            case TargetFormula.EnemyHealth: fm.SetEnemyHealthFormula(def); break;
            case TargetFormula.EnemyDamage: fm.SetEnemyDamageFormula(def); break;
            case TargetFormula.EnemyCoin: fm.SetEnemyCoinFormula(def); break;
            case TargetFormula.UpgradeCost: fm.SetUpgradeCostFormula(def); break;
        }
    }

    private void ApplyPlaceholders(FormulaType type)
    {
        // Show meaning + one-word hint. (Placeholders only appear when the field is empty.)
        SetPlaceholder(aInput, "a", "base");

        bool usesB = type == FormulaType.Linear || type == FormulaType.LinearPlusPower;
        bool usesP = type == FormulaType.Power || type == FormulaType.LinearPlusPower;
        bool usesR = type == FormulaType.Exponential;

        SetPlaceholder(bInput, "b", usesB ? (type == FormulaType.Linear ? "slope" : "scale") : "unused");
        SetPlaceholder(pInput, "p", usesP ? "exponent" : "unused");
        SetPlaceholder(rInput, "r", usesR ? "base" : "unused");
    }

    private void SetPlaceholder(TMP_InputField field, string symbol, string hint)
    {
        if (field == null) return;

        // TMP_InputField.placeholder is a Graphic; for TMP it's usually a TMP_Text.
        TMP_Text t = field.placeholder as TMP_Text;
        if (t == null) return;

        // Example: "a - base"
        t.text = symbol + " - " + hint;
    }

    private void PopulateInputsForType(FormulaDefinition def, FormulaType type)
    {
        if (def == null) def = FormulaDefinition.Constant(1f);

        // Always show a value for 'a' because it’s usually meaningful.
        if (aInput != null) aInput.text = def.a.ToString("0.###");

        bool usesB = type == FormulaType.Linear || type == FormulaType.LinearPlusPower;
        bool usesP = type == FormulaType.Power || type == FormulaType.LinearPlusPower;
        bool usesR = type == FormulaType.Exponential;

        // For the others: keep empty when at default so the placeholder stays visible.
        if (bInput != null)
            bInput.text = (usesB && !Mathf.Approximately(def.b, 0f)) ? def.b.ToString("0.###") : "";

        if (pInput != null)
            pInput.text = (usesP && !Mathf.Approximately(def.p, 1f)) ? def.p.ToString("0.###") : "";

        if (rInput != null)
            rInput.text = (usesR && !Mathf.Approximately(def.r, 1f)) ? def.r.ToString("0.###") : "";
    }

    private string GetFormulaTemplate(FormulaType type)
    {
        // ASCII-only for consistency across fonts/platforms.
        switch (type)
        {
            case FormulaType.Constant: return "a";
            case FormulaType.Linear: return "a + b * x";
            case FormulaType.Power: return "a * x^p";
            case FormulaType.Exponential: return "a * r^x";
            case FormulaType.LinearPlusPower: return "a + b * x^p";
            default: return "a";
        }
    }
}