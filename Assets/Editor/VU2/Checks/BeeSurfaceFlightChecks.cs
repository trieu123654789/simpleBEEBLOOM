using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class BeeSurfaceFlightChecks
{
    [MenuItem("Let It Bee/Check Surface Flight")]
    public static void RunMenu() => Debug.Log(Run());

    public static string Run()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Stop Play Mode first.");
        var previous = SceneManager.GetActiveScene();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        int checks = 0;
        Action<bool, string> check = (ok, message) => { if (!ok) throw new Exception(message); checks++; };
        Vector3 origin = new Vector3(10000f, 1000f, 10000f);
        try
        {
            var bee = new GameObject("Test bee").transform;
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Test floor";
            floor.transform.position = origin + Vector3.down * 0.5f;
            floor.transform.localScale = new Vector3(20f, 1f, 20f);
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.transform.position = origin + new Vector3(2f, 2f, 0f);
            wall.transform.localScale = new Vector3(0.2f, 4f, 20f);
            Physics.SyncTransforms();
            var solver = new BeeFlightCollision();
            bool grounded, ceiling;
            bee.position = origin + Vector3.up * 5f;
            var p = solver.Move(bee, null, origin + Vector3.down * 5f, .25f, .35f, ~0, out grounded, out ceiling);
            check(grounded && p.y >= origin.y + .349f, "Fast descent crossed floor");
            bee.position = origin + Vector3.up;
            p = solver.Move(bee, null, bee.position + Vector3.right * 5f, .25f, .35f, ~0, out grounded, out ceiling);
            check(p.x < origin.x + 1.66f && p.x > origin.x + 1.5f, "Side sweep failed");
            p = solver.Move(bee, null, bee.position + new Vector3(5f, 2f, 3f), .25f, .35f, ~0, out grounded, out ceiling);
            check(p.x < origin.x + 1.66f && p.z > origin.z + 2.9f, "Ascending slide failed");
            wall.transform.position = origin + Vector3.up * 3f;
            wall.transform.localScale = new Vector3(10f, .2f, 10f);
            Physics.SyncTransforms();
            p = solver.Move(bee, null, bee.position + Vector3.up * 8f, .25f, .35f, ~0, out grounded, out ceiling);
            check(ceiling && p.y < origin.y + 2.8f, "Ceiling sweep failed");
            p = solver.Move(bee, null, bee.position + Vector3.right * .01f, .25f, .35f, ~0, out grounded, out ceiling);
            check(Mathf.Abs(p.x - bee.position.x - .01f) < .002f, "Short movement overcast");
            floor.name = "SM_Terrain_Island_01";
            bee.position = origin - Vector3.up * .2f;
            p = solver.Move(bee, null, bee.position, .25f, .35f, ~0, out grounded, out ceiling);
            check(grounded && p.y > origin.y + .35f, "Terrain penetration recovery failed");
            check(BeeSurfaceSkim.EvaluateIntensity(.3f, 0f, 1.5f, 1.2f) == 0f, "Stationary skim emitted");
            check(BeeSurfaceSkim.EvaluateIntensity(2f, 6f, 1.5f, 1.2f) == 0f, "Distant skim emitted");
            check(BeeSurfaceSkim.EvaluateIntensity(.3f, 6f, 1.5f, 1.2f) > .7f, "Near-surface feedback missing");
            check(BeeMapSurfaces.IsWater("SF_Water_Sea (1)") && !BeeMapSurfaces.IsWater("SM_Waterbucket_1"), "Water identification failed");
            var mesh = GameObject.CreatePrimitive(PrimitiveType.Plane);
            mesh.name = "SF_Water_Sea";
            mesh.transform.position = origin + Vector3.right * 30f;
            UnityEngine.Object.DestroyImmediate(mesh.GetComponent<Collider>());
            check(BeeMapSurfaces.Install(scene) == 1 && mesh.GetComponent<MeshCollider>() != null, "Missing collider not installed");
            check(BeeMapSurfaces.Install(scene) == 0, "Collider installation duplicated");
            check(Resources.Load<Shader>("BeeSurfaceSkim") != null, "Skim shader missing from Resources");
        }
        finally
        {
            EditorSceneManager.CloseScene(scene, true);
            SceneManager.SetActiveScene(previous);
        }
        return "Surface flight: " + checks + " checks passed.";
    }
}
