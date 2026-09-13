namespace SuperInseto
{
    // A separate signal, not generic damage. Only the player's bioelectric projectile sends it.
    public interface IBioelectricReceiver
    {
        bool ReceiveBioelectricImpact();
    }
}
