public class TeleportSpell : SpellBase
{
    public float ManaCost = 20f;
    public float CastDelay = 0.1f;
    public float Range = 6f;

    public override float CalculateDamage(PlayerStats stats)
    {
        return 0f; // Teleporte não causa dano
    }
}
