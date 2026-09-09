using System.Collections.Generic;
using UnityEngine;
namespace Engchanok.StrategyGame
{
    public sealed class StrategyFeedback : MonoBehaviour
    {
        public static bool Muted;
        static readonly Dictionary<int, AudioClip> clips = new();
        StrategyEntity entity;
        StrategyMatch match;
        Renderer[] bodies;
        MaterialPropertyBlock tint;
        LineRenderer ring, rally;
        float flash;
        public void Initialize(StrategyEntity target, StrategyMatch owner)
        {
            entity = target; match = owner; bodies = GetComponentsInChildren<Renderer>(); tint = new MaterialPropertyBlock();
            ring = Ring(transform, owner.beamMaterial, owner.settings.Radius(entity.kind) + .25f, Color.cyan);
            rally = Ring(transform, owner.beamMaterial, .8f, new Color(.2f, 1, .7f));
        }
        public static LineRenderer Ring(Transform parent, Material material, float radius, Color color)
        {
            var line = new GameObject("Ground indicator").AddComponent<LineRenderer>(); line.transform.SetParent(parent, false);
            line.sharedMaterial = material; line.useWorldSpace = false; line.loop = true; line.positionCount = 40;
            line.startWidth = line.endWidth = .07f; line.startColor = line.endColor = color;
            var tint = new MaterialPropertyBlock(); tint.SetColor("_BaseColor", color); line.SetPropertyBlock(tint);
            for (int i = 0; i < 40; i++) { float a = i * Mathf.PI * 2 / 40; line.SetPosition(i, new Vector3(Mathf.Cos(a) * radius, .07f, Mathf.Sin(a) * radius)); }
            return line;
        }
        public void Hit() { flash = .12f; }
        public void Fire() { Pulse(match, transform.position + Vector3.up * 1.6f, Color.yellow, .18f, .09f); }
        void Update()
        {
            ring.enabled = entity.Selected;
            rally.enabled = entity.Selected && entity.RallyPoint.HasValue;
            if (entity.RallyPoint.HasValue) rally.transform.position = entity.RallyPoint.Value;
            if (!match.Running) return;
            flash = Mathf.Max(0, flash - Time.deltaTime);
            foreach (var body in bodies)
            {
                if (flash > 0) { tint.SetColor("_BaseColor", Color.white); body.SetPropertyBlock(tint); }
                else body.SetPropertyBlock(null);
            }
        }
        static void Pulse(StrategyMatch owner, Vector3 position, Color color, float size, float lifetime)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Sphere); Destroy(obj.GetComponent<Collider>()); obj.layer = 2;
            obj.transform.position = position; obj.transform.localScale = Vector3.one * size;
            obj.GetComponent<Renderer>().sharedMaterial = owner.beamMaterial;
            var block = new MaterialPropertyBlock(); block.SetColor("_BaseColor", color); obj.GetComponent<Renderer>().SetPropertyBlock(block);
            Destroy(obj, lifetime);
        }
        public static void Burst(StrategyMatch owner, Vector3 position, Color color)
        {
            for (int i = 0; i < 6; i++) Pulse(owner, position + Random.insideUnitSphere * .7f, color, .3f, .3f);
            Sound(owner, 100, .16f);
        }
        public static void Marker(StrategyMatch owner, Vector3 position, Color color)
        {
            var ring = Ring(null, owner.beamMaterial, .7f, color); ring.transform.position = position; Destroy(ring.gameObject, .8f); Sound(owner, 700, .06f);
        }
        public static void Sound(StrategyMatch owner, int frequency, float duration)
        {
            if (Muted || !owner.Running) return;
            if (!clips.TryGetValue(frequency, out var clip) || clip == null)
            {
                int count = Mathf.CeilToInt(22050 * duration); var data = new float[count];
                for (int i = 0; i < count; i++) data[i] = Mathf.Sin(i * 2 * Mathf.PI * frequency / 22050) * .12f * (1f - (float)i / count);
                clip = AudioClip.Create("Outpost cue", count, 1, 22050, false); clip.SetData(data, 0); clips[frequency] = clip;
            }
            var source = owner.GetComponent<AudioSource>(); if (source == null) source = owner.gameObject.AddComponent<AudioSource>();
            source.PlayOneShot(clip);
        }
    }
}
