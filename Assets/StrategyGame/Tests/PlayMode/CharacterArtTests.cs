using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;
namespace Engchanok.StrategyGame.Tests
{
    public sealed class CharacterArtTests
    {
        [UnityTearDown] public IEnumerator Cleanup()
        {
            StrategySession.PracticeRequested=false; Time.timeScale=1;
            SceneManager.LoadScene("MainMenu"); yield return null;
        }
        [UnityTest] public IEnumerator ImportedRosterAnimatesAndHonorsPause()
        {
            StrategySession.PracticeRequested=true;
            SceneManager.LoadScene("Survival"); yield return null; yield return null;
            var match=Object.FindFirstObjectByType<StrategyMatch>();
            foreach(EntityKind kind in Enum.GetValues(typeof(EntityKind)))
            {
                if(!StrategyEntity.IsUnitKind(kind)) continue;
                var prefab=match.prefabs[(int)kind];
                Assert.IsNotNull(prefab.GetComponentInChildren<StrategyCharacterView>(), kind.ToString());
                Assert.Greater(prefab.GetComponentsInChildren<SkinnedMeshRenderer>().Length,0,kind.ToString());
                Assert.AreEqual(1,prefab.GetComponentsInChildren<Collider>().Length,kind.ToString());
                Assert.That(prefab.GetComponent<CapsuleCollider>().height,Is.InRange(1.8f,4f));
                Assert.Greater(prefab.GetComponentInChildren<StrategyCharacterView>().markerHeight,prefab.GetComponent<CapsuleCollider>().height);
                foreach(var renderer in prefab.GetComponentsInChildren<Renderer>())
                    foreach(var material in renderer.sharedMaterials) Assert.AreEqual("Universal Render Pipeline/Lit",material.shader.name);
            }
            var worker=match.Entities.First(e=>e.kind==EntityKind.Worker);
            var view=worker.GetComponentInChildren<StrategyCharacterView>();

            var bones=view.GetComponentsInChildren<Transform>();
            var before=bones.Select(t=>t.localRotation).ToArray();
            double started=view.AnimationTime;
            yield return new WaitForSeconds(.25f);
            Assert.Greater(view.AnimationTime,started,"The presentation clock must advance");
            Assert.IsTrue(bones.Where((t,i)=>Quaternion.Angle(before[i],t.localRotation)>.01f).Any(),"Idle clip must drive the imported bones");
            match.SetPaused(true);
            double paused=view.AnimationTime; var frozen=bones.Select(t=>t.localRotation).ToArray();
            yield return new WaitForSecondsRealtime(.2f);
            Assert.AreEqual(paused,view.AnimationTime); CollectionAssert.AreEqual(frozen,bones.Select(t=>t.localRotation).ToArray());
            match.SetPaused(false); yield return new WaitForSeconds(.1f);
            Assert.Greater(view.AnimationTime,paused);
            var supplies=GameObject.Find("Outpost supplies");
            Assert.IsNotNull(supplies); Assert.AreEqual(0,supplies.GetComponentsInChildren<Collider>().Length);
            var container=supplies.transform.Find("Supply camp 1/Container_Small");
            Assert.That(StrategyCameraController.VisualBounds(container).size.x,Is.InRange(1f,5f),"Imported prop scale must remain visible");
            SceneManager.LoadScene("MainMenu"); yield return null;
            Assert.IsNull(Object.FindFirstObjectByType<StrategyEntity>());
            Assert.Greater(Object.FindObjectsByType<StrategyCharacterView>(FindObjectsSortMode.None).Length,0);
        }
    }
}
