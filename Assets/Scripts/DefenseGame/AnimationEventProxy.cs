using UnityEngine;

namespace DefenseGame
{
    public class AnimationEventProxy : MonoBehaviour
    {
        public void PlayEffect() { }
        public void PlayEffect(string effectName) { }
        public void PlayEffect(int effectIndex) { }
        public void PlayEffectTile() { }
        public void PlayEffectTile(string effectName) { }
        public void PlayEffectTile(int effectIndex) { }
        public void SpawnProp() { }
        public void SpawnProp(string propName) { }
        public void SpawnProp(int propIndex) { }
        public void PlaySound() { }
        public void PlaySound(string soundName) { }
        public void PlaySound(int soundIndex) { }
    }
}
