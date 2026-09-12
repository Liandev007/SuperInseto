namespace SuperInseto
{
    // Independent of visuals and Unity frame timing; at most three steps in a chain.
    public sealed class LightCombo
    {
        public int Step { get; private set; }
        float expiresAt;
        public int Next(float now, float validFor)
        {
            if (now > expiresAt || Step >= 3) Step = 0;
            Step++;
            expiresAt = now + validFor;
            return Step;
        }
        public void Reset() { Step = 0; expiresAt = 0f; }
    }
}
