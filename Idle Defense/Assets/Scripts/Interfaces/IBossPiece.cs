public interface IBossPiece
{
    // Higher runs first 
    int Priority { get; }

    // Animator trigger to fire when this piece executes (e.g. "Skill_Summon", "Skill_Shockwave")
    string AnimationTrigger { get; }

    bool WantsToExecute(BossContext ctx);

    // Should queue the pending boss skill(s). No animation calls in here.
    void Execute(BossContext ctx);
}
