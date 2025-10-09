#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class PrestigeManagerMenu
{
    private static PrestigeManager GetPM()
    {
        var pm = Object.FindObjectOfType<PrestigeManager>();
        if (!pm)
        {
            Debug.LogWarning("[Prestige] No PrestigeManager found in the scene.");
        }
        return pm;
    }

    [MenuItem("Idle Defense/Prestige/Print Summary")]
    private static void Menu_PrintSummary()
    {
        var pm = GetPM();
        if (pm) pm.Debug_PrintSummary();
    }

    [MenuItem("Idle Defense/Prestige/Grant 100 Crimson")]
    private static void Menu_GrantCrimson()
    {
        var pm = GetPM();
        if (pm) pm.Debug_Grant100Crimson();
    }

    [MenuItem("Idle Defense/Prestige/Unlock All Nodes")]
    private static void Menu_UnlockAll()
    {
        var pm = GetPM();
        if (pm) pm.Debug_UnlockAll();
    }

    [MenuItem("Idle Defense/Prestige/Lock All Nodes")]
    private static void Menu_LockAll()
    {
        var pm = GetPM();
        if (pm) pm.Debug_LockAll();
    }

    [MenuItem("Idle Defense/Prestige/Rebuild Caches")]
    private static void Menu_Rebuild()
    {
        var pm = GetPM();
        if (pm) pm.Debug_Rebuild();
    }
}
#endif
