using System;

namespace SuperInseto.Editor
{
    // Measured from the supplied FBX AnimationStack/KeyTime records: 30 samples/second.
    // First/last are source frames, never fabricated animations. The originals remain byte-for-byte intact.
    internal static class PlayerClipCatalog
    {
        internal sealed class Clip
        {
            public readonly string file, name;
            public readonly float first, last;
            public readonly bool loop, extractY;
            public Clip(string file, string name, float first, float last, bool loop = false, bool extractY = false)
            { this.file = file; this.name = name; this.first = first; this.last = last; this.loop = loop; this.extractY = extractY; }
        }
        internal static readonly Clip[] All = {
            new Clip("Idle.fbx", "Idle", 0, 59, true),
            new Clip("Walk.fbx", "Walk", 0, 31, true),
            new Clip("Run.fbx", "Run", 0, 22, true),
            new Clip("Jump.fbx", "JumpTakeoff", 6, 18, false, true),
            new Clip("Floating.fbx", "Floating", 0, 67, true, true),
            new Clip("Fall.fbx", "FallLanding", 10, 32, false, true),
            new Clip("Climb.fbx", "Climb", 0, 67, true, true),
            new Clip("Mantle.fbx", "Mantle", 0, 116, false, true),
            new Clip("LightAtack01.fbx", "LightAttack01", 0, 26),
            new Clip("LightAttack02.fbx", "LightAttack02", 0, 41),
            new Clip("LightAttack03.fbx", "LightAttack03", 0, 37),
            new Clip("HeavyAttack.fbx", "HeavyAttack", 0, 34),
            new Clip("Dodge.fbx", "Dodge", 0, 30, false, true),
            new Clip("HitReaction.fbx", "HitReaction", 0, 24),
            new Clip("Death.fbx", "Death", 0, 117),
            new Clip("ChitingImpact.fbx", "ChitinImpact", 0, 51),
            new Clip("BioletricStinger.fbx", "BioelectricStinger", 0, 41)
        };
    }
}
