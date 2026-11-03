/// <summary>
/// Prototype-only switches. Set a value to true when that system is ready to be restored.
/// Keeping them here makes the temporary removals explicit and reversible.
/// </summary>
public static class TemporaryFeatureSwitches
{
    public static bool HealthSystemEnabled = false;
    public static bool ObstaclesEnabled = false;
    public static bool MapTransitionsEnabled = false;
    public static bool EnemyEnabled = false;
    public static bool VisionEffectsEnabled = false;
}
