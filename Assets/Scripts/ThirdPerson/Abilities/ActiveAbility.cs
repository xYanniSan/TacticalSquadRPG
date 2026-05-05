namespace TacticalRPG.ThirdPerson.Abilities
{
    /// <summary>
    /// Base class for all combat abilities executed through AbilityExecutor.
    /// Each ability manages its own timing, movement, and animation.
    /// </summary>
    public abstract class ActiveAbility
    {
        protected AbilityContext Ctx;

        // ── Called by AbilityExecutor ────────────────────────────────

        public void Bind(AbilityContext context) { Ctx = context; }

        /// <summary>Called once when the ability begins.</summary>
        public abstract void OnStart();

        /// <summary>
        /// Called every frame while active.
        /// Return true to signal the ability has finished and execution can transition to Recover.
        /// </summary>
        public abstract bool OnTick(float dt);

        /// <summary>
        /// Called when an animation event arrives (e.g. "HitFrame", "AttackEnd", "BlockEnd", "HitEnd").
        /// Abilities subscribe to the events they care about.
        /// </summary>
        public abstract void OnAnimationEvent(string eventName);

        /// <summary>Called once when the ability ends (success or override).</summary>
        public virtual void OnEnd() { }
    }
}
