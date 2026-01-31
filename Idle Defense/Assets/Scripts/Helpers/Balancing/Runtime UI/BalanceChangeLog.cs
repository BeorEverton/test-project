using System;
using System.Collections.Generic;

[Serializable]
public class BalanceChangeLogEntry
{
    public string utcIsoTime;
    public string key;
    public string oldValue;
    public string newValue;
}

[Serializable]
public class BalanceSessionReport
{
    public string sessionId;
    public string utcIsoStart;
    public string utcIsoEnd;

    public BalanceTuningProfile finalProfile;
    public List<BalanceChangeLogEntry> changeLog = new List<BalanceChangeLogEntry>();

    // HUD snapshot at export time
    public int waveIndex;
    public int enemiesSpawnedSoFar;
    public int enemiesTotalThisWave;
    public int enemiesAlive;
    public float avgTimeToBaseSeconds;
}
