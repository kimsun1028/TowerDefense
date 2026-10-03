// Code Defense - sprite sheet -> AnimationClip / AnimatorController generator
// Unity 6000.3 / put this file anywhere under an "Editor" folder (e.g. Assets/Editor/).
//
// Menu: Tools > Code Defense > 1. Generate Animation Clips
//       Tools > Code Defense > 2. Generate Animator Controllers
//
// - Scans the selected folder in the Project window (or all of Assets if nothing is selected).
// - Every sliced (Sprite Mode: Multiple) texture whose name follows our naming rules becomes
//   <same folder>/Animations/<texture name>.anim
// - Running it again updates existing clips in place (GUIDs kept, prefab links survive).
// - Batch mode:  Unity -batchmode -quit -projectPath <path> -executeMethod CodeDefenseAnimGenerator.BatchAll

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class CodeDefenseAnimGenerator
{
    // ---------------------------------------------------------------- frame-rate / loop rules
    // Matched against the "action" part of the file name (everything after the owner).
    struct Rule { public float fps; public bool loop; public Rule(float f, bool l) { fps = f; loop = l; } }

    static Rule GetRule(string action)
    {
        string a = action.Split('_')[0]; // Walk_Up -> Walk
        switch (a)
        {
            case "Idle":          return new Rule(6, true);
            case "Walk":          return new Rule(8, true);
            case "Fly":           return new Rule(8, true);
            case "Attack":        return new Rule(10, false);
            case "Block":         return new Rule(10, false);
            case "Shoot":         return new Rule(10, false);
            case "Dodge":         return new Rule(12, false);
            case "Hurt":          return new Rule(12, false);
            case "Death":         return new Rule(10, false);
            case "Transform":     return new Rule(10, false);
            case "DoorOpen":      return new Rule(10, false);
            case "LightPulse":    return new Rule(8, true);
            case "LockIndicator": return new Rule(2, true);
            case "SpawnEffect":   return new Rule(12, false);
            case "Overdrive":     return new Rule(8, true);
            case "Projectile":    return new Rule(12, true);
            case "Hit":           return new Rule(12, false);
            case "Ground":        return new Rule(6, true);
            case "Build":         return new Rule(12, false);
            case "Respawn":       return new Rule(10, false);
            case "Apply":         return new Rule(12, false);
            case "Break":         return new Rule(12, false);
            case "Upgrade":       return new Rule(12, false);
            default:              return new Rule(8, true);
        }
    }

    static readonly HashSet<string> EffectKinds = new HashSet<string> { "Projectile", "Hit", "Ground", "SpawnEffect", "Build", "Upgrade" };

    // ---------------------------------------------------------------- name parsing
    // Enemy_<ID>_<Action...>        -> owner "Enemy_<ID>"
    // Soldier_Lv<n>_<Action...>     -> owner "Soldier_Lv<n>"
    // <Xxx>Tower_Lv<n>_<Action...>  -> owner "<Xxx>Tower_Lv<n>"
    // Hero_Lv<n>_<Action...>        -> owner "Hero_Lv<n>"
    // Effect_<Name>_<Action>        -> owner "Effect_<Name>" (clips only, no controller)
    static bool TryParse(string name, out string owner, out string action)
    {
        owner = action = null;
        string[] p = name.Split('_');
        if (p.Length < 3) return false;
        if (p[0] == "Enemy" || p[0] == "Soldier" || p[0] == "Hero" || p[0] == "Effect" || p[0].EndsWith("Tower"))
        {
            owner = p[0] + "_" + p[1];
            action = string.Join("_", p.Skip(2));
            return true;
        }
        return false;
    }

    // ---------------------------------------------------------------- menu: clips
    [MenuItem("Tools/Code Defense/1. Generate Animation Clips")]
    public static void GenerateClipsMenu()
    {
        var made = GenerateClips(GetRootFolder());
        EditorUtility.DisplayDialog("Code Defense", $"Animation clips created/updated: {made.Count}", "OK");
    }

    [MenuItem("Tools/Code Defense/2. Generate Animator Controllers")]
    public static void GenerateControllersMenu()
    {
        string root = GetRootFolder();
        var clips = GenerateClips(root);
        int n = GenerateControllers(clips);
        EditorUtility.DisplayDialog("Code Defense", $"Clips: {clips.Count}\nAnimator Controllers: {n}", "OK");
    }

    public static void BatchAll()
    {
        var clips = GenerateClips("Assets");
        int n = GenerateControllers(clips);
        Debug.Log($"[CodeDefense] batch done. clips={clips.Count}, controllers={n}");
    }

    static string GetRootFolder()
    {
        foreach (var obj in Selection.GetFiltered<UnityEngine.Object>(SelectionMode.Assets))
        {
            string path = AssetDatabase.GetAssetPath(obj);
            if (AssetDatabase.IsValidFolder(path)) return path;
            if (!string.IsNullOrEmpty(path)) return Path.GetDirectoryName(path).Replace('\\', '/');
        }
        return "Assets";
    }

    // ---------------------------------------------------------------- clip generation
    class ClipInfo { public AnimationClip clip; public string owner; public string action; public string folder; public string name; }

    static List<ClipInfo> GenerateClips(string root)
    {
        var result = new List<ClipInfo>();
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { root });

        // pass 1: pick the sheets and create every Animations folder BEFORE asset editing starts
        // (folders created inside StartAssetEditing are not registered yet, which produced
        //  duplicate empty "Animations 1", "Animations 2"... folders)
        var jobs = new List<(string texPath, string texName, string owner, string action, string folder)>();
        foreach (string guid in guids)
        {
            string texPath = AssetDatabase.GUIDToAssetPath(guid);
            string texName = Path.GetFileNameWithoutExtension(texPath);
            if (!TryParse(texName, out string owner, out string action)) continue;
            var importer = AssetImporter.GetAtPath(texPath) as TextureImporter;
            if (importer == null || importer.spriteImportMode != SpriteImportMode.Multiple) continue;
            string folder = Path.GetDirectoryName(texPath).Replace('\\', '/') + "/Animations";
            jobs.Add((texPath, texName, owner, action, folder));
        }
        foreach (string folder in jobs.Select(j => j.folder).Distinct())
            EnsureFolder(folder);
        AssetDatabase.Refresh();

        // pass 2: build / update clips
        try
        {
            AssetDatabase.StartAssetEditing();
            for (int i = 0; i < jobs.Count; i++)
            {
                var j = jobs[i];
                EditorUtility.DisplayProgressBar("Code Defense", j.texName, (float)i / jobs.Count);

                Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(j.texPath).OfType<Sprite>()
                    .OrderBy(s => FrameIndex(s.name)).ToArray();
                if (sprites.Length == 0) continue;

                AnimationClip clip = BuildClip(sprites, GetRule(j.action));
                clip.name = j.texName;

                string clipPath = $"{j.folder}/{j.texName}.anim";
                var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
                if (existing != null)
                {
                    EditorUtility.CopySerialized(clip, existing);
                    existing.name = j.texName;
                    EditorUtility.SetDirty(existing);
                }
                else
                {
                    AssetDatabase.CreateAsset(clip, clipPath);
                }
                result.Add(new ClipInfo { clip = null, owner = j.owner, action = j.action, folder = j.folder, name = j.texName });
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            EditorUtility.ClearProgressBar();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        foreach (var ci in result)
            ci.clip = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{ci.folder}/{ci.name}.anim");
        result.RemoveAll(ci => ci.clip == null);
        Debug.Log($"[CodeDefense] clips created/updated: {result.Count} (root: {root})");
        return result;
    }

    static int FrameIndex(string spriteName)
    {
        int u = spriteName.LastIndexOf('_');
        return (u >= 0 && int.TryParse(spriteName.Substring(u + 1), out int n)) ? n : 0;
    }

    static AnimationClip BuildClip(Sprite[] sprites, Rule rule)
    {
        var clip = new AnimationClip { frameRate = rule.fps };
        var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
        // one key per frame + a hold key so the last frame gets its full duration
        var keys = new ObjectReferenceKeyframe[sprites.Length + 1];
        for (int i = 0; i < sprites.Length; i++)
            keys[i] = new ObjectReferenceKeyframe { time = i / rule.fps, value = sprites[i] };
        keys[sprites.Length] = new ObjectReferenceKeyframe { time = sprites.Length / rule.fps, value = sprites[sprites.Length - 1] };
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);

        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = rule.loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        return clip;
    }

    static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder)) return;
        string parent = Path.GetDirectoryName(folder).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
    }

    // ---------------------------------------------------------------- controller generation
    // One controller per owner (Enemy_Goblin, Soldier_Lv1, ArcherTower_Lv1 ...).
    // States are named after the action ("Walk", "Walk_Up", "Attack", "DoorOpen" ...),
    // no transitions: play them from code, e.g. animator.Play("Attack").
    // Effects (Projectile / Hit / Ground / SpawnEffect) are left as plain clips.
    static int GenerateControllers(List<ClipInfo> clips)
    {
        int count = 0;
        foreach (var group in clips.Where(c => !EffectKinds.Contains(c.action.Split('_')[0]) && !c.owner.StartsWith("Effect_"))
                                   .GroupBy(c => c.folder + "|" + c.owner))
        {
            var list = group.ToList();
            string folder = list[0].folder;
            string owner = list[0].owner;
            string path = $"{folder}/{owner}.controller";

            var ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (ctrl == null) ctrl = AnimatorController.CreateAnimatorControllerAtPath(path);
            var sm = ctrl.layers[0].stateMachine;

            foreach (var ci in list)
            {
                var state = sm.states.Select(s => s.state).FirstOrDefault(s => s.name == ci.action)
                            ?? sm.AddState(ci.action);
                state.motion = ci.clip;
            }
            string[] defaults = { "Idle", "Walk", "Fly" };
            foreach (var d in defaults)
            {
                var st = sm.states.Select(s => s.state).FirstOrDefault(s => s.name == d);
                if (st != null) { sm.defaultState = st; break; }
            }
            EditorUtility.SetDirty(ctrl);
            count++;
        }
        AssetDatabase.SaveAssets();
        Debug.Log($"[CodeDefense] animator controllers created/updated: {count}");
        return count;
    }
}
