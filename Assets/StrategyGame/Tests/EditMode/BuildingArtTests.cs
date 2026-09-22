using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
namespace Engchanok.StrategyGame.Tests
{
    public sealed class BuildingArtTests
    {
        [TestCase(EntityKind.Headquarters)]
        [TestCase(EntityKind.Barracks)]
        [TestCase(EntityKind.RangerPost)]
        [TestCase(EntityKind.SupportBay)]
        [TestCase(EntityKind.SupplyRelay)]
        [TestCase(EntityKind.Turret)]
        public void MedievalBuildingFitsItsExistingGameplayFootprint(EntityKind kind)
        {
            var settings=AssetDatabase.LoadAssetAtPath<StrategySettings>("Assets/StrategyGame/Data/DefaultStrategy.asset");
            var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/StrategyGame/Prefabs/"+kind+".prefab");
            Assert.IsNotNull(source);
            var instance=Object.Instantiate(source);
            try
            {
                Assert.AreEqual(kind,instance.GetComponent<StrategyEntity>().kind);
                Assert.AreEqual(1,instance.GetComponentsInChildren<Collider>().Length);
                Assert.AreEqual(settings.Radius(kind),instance.GetComponent<CapsuleCollider>().radius);
                var obstacle=instance.GetComponent<NavMeshObstacle>();
                Assert.IsTrue(obstacle.carving); Assert.AreEqual(settings.Radius(kind),obstacle.radius);
                // Compare the saved navigation extents; capsule height access differs from the serialized half-height.
                Assert.AreEqual(new Vector3(settings.Radius(kind),1.5f,settings.Radius(kind)),new SerializedObject(obstacle).FindProperty("m_Extents").vector3Value);
                Assert.AreEqual(new Vector3(0,1.5f,0),obstacle.center);
                Assert.IsNull(instance.GetComponent<NavMeshAgent>());
                var bounds=StrategyCameraController.VisualBounds(instance.transform);
                Assert.That(bounds.min.y,Is.EqualTo(0).Within(.02f));
                Assert.LessOrEqual(new Vector2(bounds.extents.x,bounds.extents.z).magnitude,settings.Radius(kind)+.02f);
                Assert.Greater(bounds.size.y,1);
                Assert.Greater(instance.GetComponent<StrategyEntity>().buildingMarkerHeight,bounds.max.y);
                foreach(var mesh in instance.GetComponentsInChildren<MeshFilter>())
                    StringAssert.Contains("/MedievalHexagon/",AssetDatabase.GetAssetPath(mesh.sharedMesh));
                foreach(var renderer in instance.GetComponentsInChildren<Renderer>())
                    foreach(var material in renderer.sharedMaterials)
                    {
                        Assert.AreEqual("Universal Render Pipeline/Lit",material.shader.name);
                        Assert.IsNotNull(material.GetTexture("_BaseMap"));
                    }
            }
            finally { Object.DestroyImmediate(instance); }
        }
    }
}
