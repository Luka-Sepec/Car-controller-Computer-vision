using UnityEngine;
using UnityEditor;

public class AddMeshColliders
{
    [MenuItem("Tools/Add mesh colliders to selection")]
    public static void AddColliders()
    {
        GameObject[] selected = Selection.gameObjects;
        int count = 0;
        foreach(GameObject go in selected)
        {
            MeshFilter[] meshes = go.GetComponentsInChildren<MeshFilter>();
            foreach(MeshFilter meshFilter in meshes)
            {
                if (meshFilter.GetComponent<Collider>() == null)
                {
                    MeshCollider mc = meshFilter.gameObject.AddComponent<MeshCollider>();
                    mc.convex = false;
                    count++;
                }
            }
        }
        Debug.Log($"Added {count} Mesh colliders");
    }
}
