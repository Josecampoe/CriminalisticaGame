using System.Linq;
using Caso404.Evidence;
using UnityEditor;
using UnityEngine;

namespace Caso404.EditorTools
{
    /// <summary>
    /// Guarda un prefab con el aspecto de cada evidencia de la escena (Prefabs/Evidence/ev_*.prefab) y lo asigna como
    /// <c>InspectPrefab</c> de su EvidenceData. Así el laboratorio puede mostrar en 3D una evidencia recogida en otra
    /// escena. La copia no tiene colliders ni scripts; las mallas que solo existían dentro de la escena (cubos con UV en
    /// metros) se guardan en un archivo de mallas junto al prefab.
    /// </summary>
    public static class EvidenceModels
    {
        const string Folder = "Assets/_Project/Prefabs/Evidence";

        /// <summary>Captura todas las evidencias de la escena abierta.</summary>
        public static int CaptureOpenScene()
        {
            SharedAssetsBuilder.EnsureFolder(Folder);
            int count = 0;
            foreach (var pickup in Object.FindObjectsByType<EvidencePickup>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var ev = pickup.Evidence;
                if (ev == null || pickup.GetComponentsInChildren<Renderer>(true).Length == 0) continue;
                var copy = Object.Instantiate(pickup.gameObject);
                copy.name = ev.Id;
                copy.SetActive(true);
                foreach (var c in copy.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
                foreach (var b in copy.GetComponentsInChildren<MonoBehaviour>(true)) Object.DestroyImmediate(b);
                foreach (var l in copy.GetComponentsInChildren<Light>(true)) Object.DestroyImmediate(l);
                foreach (var p in copy.GetComponentsInChildren<ParticleSystem>(true)) Object.DestroyImmediate(p.gameObject);
                foreach (var t in copy.GetComponentsInChildren<Transform>(true))
                {
                    t.gameObject.layer = 0;
                    GameObjectUtility.SetStaticEditorFlags(t.gameObject, 0);
                }
                copy.transform.position = Vector3.zero;

                var meshPath = $"{Folder}/{ev.Id}_mallas.asset";
                AssetDatabase.DeleteAsset(meshPath);
                Mesh container = null;
                foreach (var mf in copy.GetComponentsInChildren<MeshFilter>(true))
                {
                    if (mf.sharedMesh == null || AssetDatabase.Contains(mf.sharedMesh)) continue;
                    var mesh = Object.Instantiate(mf.sharedMesh);
                    mesh.name = $"{ev.Id}_{mf.name}";
                    if (container == null) { AssetDatabase.CreateAsset(mesh, meshPath); container = mesh; }
                    else AssetDatabase.AddObjectToAsset(mesh, container);
                    mf.sharedMesh = mesh;
                }

                var prefab = PrefabUtility.SaveAsPrefabAsset(copy, $"{Folder}/{ev.Id}.prefab");
                Object.DestroyImmediate(copy);
                ev.InspectPrefab = prefab;
                EditorUtility.SetDirty(ev);
                count++;
            }
            AssetDatabase.SaveAssets();
            return count;
        }
    }
}
