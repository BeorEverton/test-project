using UnityEngine;
public enum OutOfRangeBehavior : byte
{
    ExecuteAnyway = 0,
    WaitUntilInRange = 1,
    SkipThisAttempt = 2
}

public interface IBossPiece
{
    // Higher runs first 
    int Priority { get; }

    // Animator trigger to fire when this piece executes (e.g. "Skill_Summon", "Skill_Shockwave")
    string AnimationTrigger { get; }

    OutOfRangeBehavior OutOfRangeBehavior { get; }

    bool WantsToExecute(BossContext ctx);

    // Should queue the pending boss skill(s). No animation calls in here.
    void Execute(BossContext ctx);

    // ----- Optional VFX Hooks -----

    GameObject VfxOnStart => null;
    GameObject VfxOnExecute => null;
    GameObject VfxOnEnd => null;

    Transform VfxAnchor => null;
    Vector3 VfxOffset => Vector3.zero;
}
