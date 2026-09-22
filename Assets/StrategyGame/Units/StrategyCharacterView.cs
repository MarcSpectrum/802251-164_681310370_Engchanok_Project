using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
namespace Engchanok.StrategyGame
{
    // Presentation only: simulation owns movement, targeting and damage.
    public sealed class StrategyCharacterView : MonoBehaviour
    {
        public Animator animator;
        public float markerHeight=2.3f;
        public AnimationClip idle, run, action, work;
        PlayableGraph graph;
        AnimationMixerPlayable mixer;
        AnimationClipPlayable[] clips;
        StrategyEntity entity;
        StrategyMatch match;
        float actionRemaining;
        public double AnimationTime => clips != null ? clips[0].GetTime() : 0;
        void OnEnable()
        {
            if (animator == null || idle == null || run == null || action == null || work == null) return;
            entity = GetComponentInParent<StrategyEntity>();
            match = entity != null ? FindFirstObjectByType<StrategyMatch>() : null;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            graph = PlayableGraph.Create(name + " character");
            graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            mixer = AnimationMixerPlayable.Create(graph, 4);
            clips = new AnimationClipPlayable[4];
            var source = new[] { idle, run, action, work };
            for (int i = 0; i < 4; i++)
            {
                clips[i] = AnimationClipPlayable.Create(graph, source[i]);
                clips[i].SetApplyFootIK(false);
                graph.Connect(clips[i], 0, mixer, i);
            }
            AnimationPlayableOutput.Create(graph, "Pose", animator).SetSourcePlayable(mixer);
            mixer.SetInputWeight(0, 1);
            graph.Play(); graph.Evaluate(0);
        }
        public void PlayAction()
        {
            if (!graph.IsValid() || (entity != null && (match == null || !match.Running))) return;
            actionRemaining = action.length; clips[2].SetTime(0);
        }
        void LateUpdate()
        {
            if (!graph.IsValid() || (entity != null && (!entity.Alive || match == null || !match.Running))) return;
            float dt = Time.deltaTime * (entity != null ? entity.Pace : 1);
            actionRemaining = Mathf.Max(0, actionRemaining - dt);
            bool moving = entity != null && entity.Agent != null && entity.Agent.velocity.sqrMagnitude > .04f;
            bool working = entity != null && !moving && (entity.Order == UnitOrder.Gather || entity.Order == UnitOrder.Heal || entity.Order == UnitOrder.Repair);
            int state = moving ? 1 : actionRemaining > 0 ? 2 : working ? 3 : 0;
            for (int i = 0; i < 4; i++)
            {
                mixer.SetInputWeight(i, Mathf.MoveTowards(mixer.GetInputWeight(i), i == state ? 1 : 0, dt * 12));
                var clip = i == 0 ? idle : i == 1 ? run : i == 2 ? action : work;
                if (i != 2 && clips[i].GetTime() >= clip.length) clips[i].SetTime(clips[i].GetTime() % clip.length);
            }
            graph.Evaluate(dt);
        }
        void OnDisable() { if (graph.IsValid()) graph.Destroy(); clips = null; }
    }
}
