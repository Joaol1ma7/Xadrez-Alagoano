using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

[InitializeOnLoad]
public static class PawnIntegration
{
    const string Work = "C:/Users/Joao/Documents/Codex/2026-10-04/referenced-chatgpt-conversation-this-is-an-2/work/";
    const string ModelPath = "Assets/Models/Chess/Pawn.fbx";
    static double nextPoll;
    static PawnIntegration() { EditorApplication.update += Poll; }
    static void Poll()
    {
        if (EditorApplication.timeSinceStartup < nextPoll || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        nextPoll = EditorApplication.timeSinceStartup + 1;
        var path = Work + "unity-command.txt";
        if (!File.Exists(path)) return;
        var command = File.ReadAllText(path).Trim();
        File.Delete(path);
        try
        {
            if (command == "inspect") Inspect();
            else if (command == "apply") Apply();
            else if (command == "verify") Verify();
        }
        catch (Exception e) { File.WriteAllText(Work + "unity-error.txt", e.ToString()); Debug.LogException(e); }
    }
    [Serializable] class Item
    {
        public string name, hierarchy, type, team, tag, prefab;
        public string instanceId;
        public Vector3 position, rotation, scale;
        public string[] colliders, renderers, materials;
        public Vector3 renderSize, renderCenter;
    }
    [Serializable] class Report
    {
        public string scene, pipeline;
        public bool dirty, playing;
        public Item[] pieces;
        public string[] model;
    }
    static string Hierarchy(Transform t) { return t.parent ? Hierarchy(t.parent) + "/" + t.name : t.name; }
    static PecaDeXadrez[] Pieces() { return UnityEngine.Object.FindObjectsByType<PecaDeXadrez>(FindObjectsInactive.Include).Where(p=>p.gameObject.scene == EditorSceneManager.GetActiveScene()).OrderBy(p=>Hierarchy(p.transform)).ToArray(); }
    static Bounds BoundsOf(IEnumerable<Renderer> renderers)
    {
        var list = renderers.ToArray();
        if (list.Length == 0) throw new Exception("No renderers");
        var b = list[0].bounds;
        foreach (var r in list.Skip(1)) b.Encapsulate(r.bounds);
        return b;
    }
    static void Inspect()
    {
        var rep = new Report { scene = EditorSceneManager.GetActiveScene().path, dirty = EditorSceneManager.GetActiveScene().isDirty,
            playing = EditorApplication.isPlaying, pipeline = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline?.name ?? "BuiltIn" };
        rep.pieces = Pieces().Select(p=> {
            var rs = p.GetComponentsInChildren<Renderer>(true);
            var b = BoundsOf(rs.Where(r=>r.enabled));
            return new Item { name=p.name, hierarchy=Hierarchy(p.transform), type=p.tipo.ToString(), team=p.equipe.ToString(),
                instanceId=p.GetEntityId().ToString(), tag=p.tag, prefab=PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(p.gameObject),
                position=p.transform.position, rotation=p.transform.eulerAngles, scale=p.transform.lossyScale,
                colliders=p.GetComponentsInChildren<Collider>(true).Select(c=>Hierarchy(c.transform)+":"+c.GetType().Name+":"+c.enabled).ToArray(),
                renderers=rs.Select(r=>Hierarchy(r.transform)+":"+r.enabled).ToArray(),
                materials=rs.SelectMany(r=>r.sharedMaterials).Where(m=>m).Select(m=>m.name+":"+m.shader.name).Distinct().ToArray(),
                renderSize=b.size, renderCenter=b.center };
        }).ToArray();
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        rep.model = model ? model.GetComponentsInChildren<MeshFilter>().Select(m=>m.name+":"+m.sharedMesh.vertexCount+":"+m.sharedMesh.bounds.ToString()).ToArray() : new string[0];
        File.WriteAllText(Work+"unity-inspect.json", JsonUtility.ToJson(rep,true));
    }
    [Serializable] class Original
    {
        public string hierarchy, logicalId, data, transformData, prefab;
        public string[] colliderIds, colliderData;
        public Vector3 size, center;
    }
    [Serializable] class Baseline { public Original[] pieces; }
    static Original Snapshot(PecaDeXadrez p)
    {
        var cs = p.GetComponentsInChildren<Collider>(true);
        var b = BoundsOf(p.GetComponentsInChildren<Renderer>(true).Where(r=>r.enabled));
        return new Original { hierarchy=Hierarchy(p.transform), logicalId=GlobalObjectId.GetGlobalObjectIdSlow(p).ToString(),
            data=EditorJsonUtility.ToJson(p), transformData=EditorJsonUtility.ToJson(p.transform),
            prefab=PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(p.gameObject),
            colliderIds=cs.Select(c=>GlobalObjectId.GetGlobalObjectIdSlow(c).ToString()).ToArray(),
            colliderData=cs.Select(c=>EditorJsonUtility.ToJson(c)).ToArray(), size=b.size, center=b.center };
    }
    static Material Wood(string name, string sourcePath)
    {
        var path="Assets/Models/Chess/"+name+".mat";
        var source=AssetDatabase.LoadAssetAtPath<Material>(sourcePath);
        if (!source) throw new Exception("Missing wood source: "+sourcePath);
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        bool create = !mat;
        if(create) mat = new Material(source) { name=name };
        mat.SetFloat("_Metallic",0);
        mat.SetFloat("_Smoothness",0.4f);
        mat.SetFloat("_WorkflowMode",1);
        mat.SetTexture("_MetallicGlossMap",null);
        mat.DisableKeyword("_METALLICSPECGLOSSMAP");
        mat.DisableKeyword("_METALLICGLOSSMAP");
        mat.DisableKeyword("_SPECULAR_SETUP");
        mat.SetFloat("_SmoothnessTextureChannel",0);
        // Keep the project's existing wood color maps and team colors.
        if(create) AssetDatabase.CreateAsset(mat,path); else EditorUtility.SetDirty(mat);
        return mat;
    }
    static void Apply()
    {
        if (EditorApplication.isPlaying) throw new Exception("Exit Play Mode first");
        var scene=EditorSceneManager.GetActiveScene();
        if(scene.path!="Assets/Scenes/SampleScene.unity") throw new Exception("Unexpected scene");
        var all=Pieces();
        var pawns=all.Where(p=>p.tipo==TipoPeca.Peao).ToArray();
        if(all.Length!=32 || pawns.Length!=16 || pawns.Count(p=>p.equipe==Equipe.Branca)!=8) throw new Exception("Unexpected piece count");
        if(pawns.Any(p=>p.transform.Find("Visual") || p.tag!="piece" || p.GetComponents<Collider>().Length!=1)) throw new Exception("Unexpected pawn structure");
        var baseline=new Baseline { pieces=all.Select(Snapshot).ToArray() };
        File.WriteAllText(Work+"unity-baseline.json",JsonUtility.ToJson(baseline,true));
        EditorSceneManager.SaveScene(scene,Work+"backup/SampleScene.live.before.unity",true);
        var importer=(ModelImporter)AssetImporter.GetAtPath(ModelPath);
        importer.globalScale=1f;
        importer.importAnimation=false;
        importer.addCollider=false;
        importer.materialImportMode=ModelImporterMaterialImportMode.None;
        importer.SaveAndReimport();
        var model=AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        if(!model) throw new Exception("FBX import failed");
        var light=Wood("Pawn_Wood","Assets/Chess MEGA-pack/Materials/LightOak/Oak.mat");
        var dark=Wood("Pawn_Wood_Dark","Assets/Chess MEGA-pack/Materials/DarkWood/DarkWood.mat");
        Undo.IncrementCurrentGroup();
        var group=Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Integrar peoes modelados");
        try
        {
            foreach(var p in pawns)
            {
                var old=p.GetComponentsInChildren<Renderer>(true).Where(r=>r.enabled).ToArray();
                var target=BoundsOf(old);
                var visual=new GameObject("Visual");
                Undo.RegisterCreatedObjectUndo(visual,"Adicionar Visual");
                visual.transform.SetParent(p.transform,false);
                var instance=(GameObject)PrefabUtility.InstantiatePrefab(model,visual.transform);
                Undo.RegisterCreatedObjectUndo(instance,"Adicionar Pawn_Modelado");
                instance.name="Pawn_Modelado";
                var rs=instance.GetComponentsInChildren<Renderer>(true);
                foreach(var r in rs)
                {
                    Undo.RecordObject(r,"Material de madeira");
                    r.sharedMaterials=Enumerable.Repeat(p.equipe==Equipe.Branca?light:dark,r.sharedMaterials.Length).ToArray();
                    PrefabUtility.RecordPrefabInstancePropertyModifications(r);
                }
                var source=BoundsOf(rs);
                float scale=Mathf.Min(target.size.x/source.size.x,target.size.y/source.size.y,target.size.z/source.size.z);
                visual.transform.localScale=Vector3.one*scale;
                var fitted=BoundsOf(rs);
                visual.transform.position+=new Vector3(target.center.x-fitted.center.x,target.min.y-fitted.min.y,target.center.z-fitted.center.z);
                foreach(var r in old)
                {
                    Undo.RecordObject(r,"Ocultar apenas visual original");
                    r.enabled=false;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(r);
                }
                PrefabUtility.RecordPrefabInstancePropertyModifications(instance.transform);
                PrefabUtility.RecordPrefabInstancePropertyModifications(instance);
            }
            VerifyAgainst(baseline);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Undo.CollapseUndoOperations(group);
            Inspect();
            File.WriteAllText(Work+"unity-applied.txt","16 pawns integrated; scene saved; logical objects, scripts, colliders and prefab links preserved.");
            Selection.activeGameObject=pawns.First(p=>p.equipe==Equipe.Branca).gameObject;
            SceneView.lastActiveSceneView?.Frame(new Bounds(new Vector3(0,0.8f,0),new Vector3(9,2,9)),false);
        }
        catch { Undo.RevertAllDownToGroup(group); throw; }
    }
    static void VerifyAgainst(Baseline baseline)
    {
        var all=Pieces();
        if(all.Length!=baseline.pieces.Length) throw new Exception("Piece count changed");
        foreach(var original in baseline.pieces)
        {
            var p=all.Single(x=>Hierarchy(x.transform)==original.hierarchy);
            var snap=Snapshot(p);
            if(snap.logicalId!=original.logicalId || snap.data!=original.data || snap.prefab!=original.prefab || !snap.colliderIds.SequenceEqual(original.colliderIds) || !snap.colliderData.SequenceEqual(original.colliderData))
                throw new Exception("Logical/collider/prefab data changed: "+original.hierarchy);
            if(snap.transformData!=original.transformData)
                throw new Exception("Logical transform changed: "+original.hierarchy);
            if(p.tipo==TipoPeca.Peao)
            {
                var visual=p.transform.Find("Visual/Pawn_Modelado");
                if(!visual || visual.GetComponentsInChildren<Collider>(true).Length!=0) throw new Exception("Invalid visual structure");
                if(p.GetComponentsInChildren<Renderer>(true).Count(r=>r.enabled)!=1) throw new Exception("Unexpected visible renderer count");
                var b=BoundsOf(visual.GetComponentsInChildren<Renderer>());
                if(Mathf.Abs(b.min.y-(original.center.y-original.size.y/2))>0.0001f || b.size.x>original.size.x+0.0001f || b.size.z>original.size.z+0.0001f)
                    throw new Exception("Visual does not fit previous footprint");
            }
        }
        File.WriteAllText(Work+"unity-verified.txt","PASS: 32 logical pieces unchanged; 16 new visuals; all collider properties, team/type data and prefab links preserved; bases and footprints match.");
    }
    static void Verify()
    {
        VerifyAgainst(JsonUtility.FromJson<Baseline>(File.ReadAllText(Work+"unity-baseline.json")));
        Inspect();
    }
}

