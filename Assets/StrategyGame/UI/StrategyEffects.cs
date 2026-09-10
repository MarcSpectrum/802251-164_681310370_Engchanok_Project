using System.Collections.Generic;
using UnityEngine;
namespace Engchanok.StrategyGame
{
    // A bounded, scene-owned pool. Cosmetic lifetimes follow the match clock.
    public sealed class StrategyEffects : MonoBehaviour
    {
        sealed class Effect
        {
            public LineRenderer line;
            public readonly MaterialPropertyBlock tint=new();
            public Vector3 start, end;
            public Color color;
            public float age, duration, size;
            public bool ring;
        }
        const int Capacity = 128;
        readonly List<Effect> effects = new();
        StrategyMatch match;
        public int ActiveCount { get { int count=0; foreach(var e in effects) if(e.line.gameObject.activeSelf) count++; return count; } }
        public static StrategyEffects For(StrategyMatch owner)
        {
            var pool=owner.GetComponent<StrategyEffects>();
            if(pool==null) { pool=owner.gameObject.AddComponent<StrategyEffects>(); pool.match=owner; }
            return pool;
        }
        public void Emit(Vector3 start, Vector3 end, Color color, float duration, float size, bool ring=false)
        {
            if(match==null || !match.Running) return;
            Effect effect=null;
            foreach(var candidate in effects) if(!candidate.line.gameObject.activeSelf) { effect=candidate; break; }
            if(effect==null)
            {
                if(effects.Count>=Capacity) return;
                var obj=new GameObject("Pooled feedback"); obj.layer=2; obj.transform.SetParent(transform,false);
                var line=obj.AddComponent<LineRenderer>(); line.sharedMaterial=match.beamMaterial;
                line.useWorldSpace=true; line.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off; line.receiveShadows=false;
                effect=new Effect { line=line }; effects.Add(effect);
            }
            effect.start=start; effect.end=end; effect.color=color; effect.duration=Mathf.Max(.01f,duration);
            effect.age=0; effect.size=size; effect.ring=ring; effect.line.gameObject.SetActive(true);
            Draw(effect);
        }
        void Draw(Effect e)
        {
            float progress=e.age/e.duration;
            e.line.loop=e.ring; e.line.positionCount=e.ring?32:2;
            e.line.startWidth=e.line.endWidth=(e.ring?.08f:e.size)*(1-progress);
            e.tint.SetColor("_BaseColor",e.color); e.line.SetPropertyBlock(e.tint);
            if(e.ring) for(int i=0;i<32;i++) { float a=i*Mathf.PI*2/32; e.line.SetPosition(i,e.start+new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*e.size*(.6f+progress*.6f)); }
            else { e.line.SetPosition(0,e.start); e.line.SetPosition(1,e.end); }
        }
        void Update()
        {
            if(match==null) return;
            if(match.Waves!=null && match.Waves.Result!=MatchResult.Playing) { foreach(var e in effects) e.line.gameObject.SetActive(false); return; }
            foreach(var effect in effects) effect.line.enabled = !match.InspectionPaused;
            if(!match.Running) return;
            foreach(var e in effects)
            {
                if(!e.line.gameObject.activeSelf) continue;
                e.age+=Time.deltaTime;
                if(e.age>=e.duration) e.line.gameObject.SetActive(false); else Draw(e);
            }
        }
    }
}
