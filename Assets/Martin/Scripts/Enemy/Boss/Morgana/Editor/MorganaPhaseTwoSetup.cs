using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// Prepara las animaciones de la fase 2 de Medusa Morgana:
// 1. Pone en loop el Iddle y el movimiento de fase 2 (venian sin loop).
// 2. Crea (o rehace) el Animator Controller de fase 2 con los triggers que usa Boss_MedusaMorgana.
// 3. Se lo asigna al modelo de fase 2 dentro del prefab Morgana.
// 4. Revisa que los frames del Inspector coincidan con los clips importados.
public static class MorganaPhaseTwoSetup
{
    private const string AnimFolder = "Assets/fran/morgana current files/anim morgana fase 2/";
    private const string ModelPath = "Assets/fran/morgana current files/morgana reworked fase2.fbx";

    private const string IdleFbx = AnimFolder + "iddle fase 2.fbx";
    private const string WalkFbx = AnimFolder + "movement fase 2.fbx";
    private const string ScreamFbx = AnimFolder + "morgana fase 2 scream.fbx";
    private const string InvocationFbx = AnimFolder + "enemy invocation fase 2.fbx";
    private const string MeteorBeamFbx = AnimFolder + "meteor beam  fase 2.fbx";   // ojo: el archivo tiene dos espacios
    private const string MeteorShowerFbx = AnimFolder + "meteor shower fase 2.fbx";

    private const string ControllerPath = AnimFolder + "Morgana Fase 2.controller";
    private const string BossPrefabPath = "Assets/Martin/Prefabs/Enemy/Boss/Morgana.prefab";

    // Deben coincidir con Boss_MedusaMorgana
    private const string IdleState = "Iddle";
    private const string ParamIsMoving = "IsMoving";
    private const string ParamScream = "Scream";
    private const string ParamInvocation = "Invocation";
    private const string ParamMeteorBeam = "MeteorBeam";
    private const string ParamMeteorShower = "MeteorShower";

    [MenuItem("Tools/Morgana/1. Configurar Animator de fase 2")]
    public static void SetupAll()
    {
        bool ok = true;

        ok &= SetLoop(IdleFbx, "Iddle");
        ok &= SetLoop(WalkFbx, "Walk");

        if (!ok)
        {
            Debug.LogError("[Morgana Setup] No se encontraron todas las animaciones de fase 2. Revisa las rutas en MorganaPhaseTwoSetup.cs");
            return;
        }

        AnimatorController controller = BuildController();
        if (controller == null) return;

        AssignToBossPrefab(controller);

        AssetDatabase.SaveAssets();
        Validate();
    }

    [MenuItem("Tools/Morgana/2. Revisar configuracion de fase 2")]
    public static void Validate()
    {
        List<string> problems = new List<string>();

        int bossScream = 70, bossClimax = 80, bossBeam = 70, bossShower = 70;
        ReadBossFrames(ref bossScream, ref bossClimax, ref bossBeam, ref bossShower, problems);

        CheckClip(ScreamFbx, bossScream, "phaseTwoScreamFrames", false, problems);
        CheckClip(InvocationFbx, bossClimax, "climaxFrames", false, problems);
        CheckClip(MeteorBeamFbx, bossBeam, "meteorBeamFrames", false, problems);
        CheckClip(MeteorShowerFbx, bossShower, "meteorShowerFrames", false, problems);
        CheckClip(IdleFbx, -1, null, true, problems);
        CheckClip(WalkFbx, -1, null, true, problems);

        CheckPrefabAnimator(problems);

        if (problems.Count == 0)
        {
            Debug.Log("[Morgana Setup] Fase 2 lista: controller asignado, loops correctos y frames iguales a los clips.");
            return;
        }

        Debug.LogWarning("[Morgana Setup] Revisa esto (" + problems.Count + "):\n- " + string.Join("\n- ", problems));
    }

    // ---------- AUDIO ----------

    private const string AudioFolder = "Assets/Martin/Scripts/Enemy/Boss/Morgana/BossAudio_Morgana/";

    // campo de BossAudio -> archivos (varios = variaciones al azar)
    private static readonly (string field, string[] files)[] AudioMap =
    {
        ("phaseScream", new[] { "Morgana_PhaseScream" }),
        ("phaseTransform", new[] { "Morgana_PhaseTransform" }),
        ("phaseTwoEntrance", new[] { "Morgana_Phase2Scream" }),
        ("crystalGlow", new[] { "Climax_CrystalGlow" }),
        ("climaxScream", new[] { "Climax_Scream" }),
        ("arenaBorderStart", new[] { "Climax_BorderStart" }),
        ("arenaBorderLoop", new[] { "Climax_BorderLoop" }),
        ("borderTick", new[] { "Climax_BorderTick" }),
        ("waveSummon", new[] { "Climax_WaveSummon" }),
        ("meteorBeamScream", new[] { "MeteorBeam_Scream" }),
        ("meteorBeamCharge", new[] { "MeteorBeam_ChargeLoop" }),
        ("meteorBeamFire", new[] { "MeteorBeam_Fire" }),
        ("meteorBeamLoop", new[] { "MeteorBeam_FireLoop" }),
        ("meteorBeamHit", new[] { "MeteorBeam_Hit_01", "MeteorBeam_Hit_02" }),
        ("meteorShowerCast", new[] { "MeteorShower_Cast" }),
        ("meteorFall", new[] { "Meteor_Fall_01", "Meteor_Fall_02", "Meteor_Fall_03" }),
        ("meteorImpact", new[] { "Meteor_Impact_01", "Meteor_Impact_02", "Meteor_Impact_03" }),
    };

    // Llena los sonidos de BossAudio en el prefab. Solo pisa un campo si esta vacio o si
    // tenia un placeholder (clips de una carpeta "Placeholders"): no toca audio que el equipo ya puso.
    [MenuItem("Tools/Morgana/3. Asignar audios de fase 2")]
    public static void AssignAudio()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(BossPrefabPath);
        int assigned = 0, kept = 0;
        List<string> missing = new List<string>();

        try
        {
            BossAudio audio = root.GetComponentInChildren<BossAudio>(true);

            if (audio == null)
            {
                Debug.LogError("[Morgana Setup] El prefab no tiene BossAudio");
                return;
            }

            SerializedObject so = new SerializedObject(audio);

            foreach ((string field, string[] files) in AudioMap)
            {
                SerializedProperty clipsProp = so.FindProperty(field + ".clips");

                if (clipsProp == null)
                {
                    missing.Add("campo " + field);
                    continue;
                }

                if (!CanReplace(clipsProp))
                {
                    kept++;
                    continue;
                }

                List<AudioClip> clips = new List<AudioClip>();

                foreach (string file in files)
                {
                    AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(AudioFolder + file + ".wav");
                    if (clip != null) clips.Add(clip);
                    else missing.Add(file + ".wav");
                }

                if (clips.Count == 0) continue;

                clipsProp.arraySize = clips.Count;

                for (int i = 0; i < clips.Count; i++)
                {
                    clipsProp.GetArrayElementAtIndex(i).objectReferenceValue = clips[i];
                }

                SerializedProperty volume = so.FindProperty(field + ".volume");
                if (volume != null && volume.floatValue <= 0f) volume.floatValue = 1f;

                assigned++;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, BossPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        Debug.Log("[Morgana Setup] Audios asignados: " + assigned + ". Se respetaron " + kept + " campos que ya tenian audio propio.");

        if (missing.Count > 0)
        {
            Debug.LogWarning("[Morgana Setup] No se encontro: " + string.Join(", ", missing) + " (carpeta " + AudioFolder + ")");
        }
    }

    // Vacio o solo placeholders = se puede reemplazar
    private static bool CanReplace(SerializedProperty clipsProp)
    {
        for (int i = 0; i < clipsProp.arraySize; i++)
        {
            Object clip = clipsProp.GetArrayElementAtIndex(i).objectReferenceValue;
            if (clip == null) continue;

            string path = AssetDatabase.GetAssetPath(clip);
            if (!path.Contains("Placeholders")) return false;
        }

        return true;
    }

    // ---------- IMPORT ----------

    private static bool SetLoop(string fbxPath, string clipName)
    {
        ModelImporter importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;

        if (importer == null)
        {
            Debug.LogError("[Morgana Setup] No existe: " + fbxPath);
            return false;
        }

        ModelImporterClipAnimation[] clips = importer.clipAnimations;
        if (clips == null || clips.Length == 0) clips = importer.defaultClipAnimations;

        if (clips == null || clips.Length == 0)
        {
            Debug.LogError("[Morgana Setup] " + fbxPath + " no tiene animacion");
            return false;
        }

        bool changed = false;

        for (int i = 0; i < clips.Length; i++)
        {
            if (!clips[i].loopTime)
            {
                clips[i].loopTime = true;
                changed = true;
            }

            if (i == 0 && clips[i].name != clipName)
            {
                clips[i].name = clipName;
                changed = true;
            }
        }

        if (changed)
        {
            importer.clipAnimations = clips;
            importer.SaveAndReimport();
            Debug.Log("[Morgana Setup] Loop activado en " + clipName + " (" + fbxPath + ")");
        }

        return true;
    }

    private static AnimationClip LoadClip(string fbxPath)
    {
        return AssetDatabase.LoadAllAssetsAtPath(fbxPath)
            .OfType<AnimationClip>()
            .FirstOrDefault(c => !c.name.StartsWith("__preview__"));
    }

    // ---------- CONTROLLER ----------

    private static AnimatorController BuildController()
    {
        AnimationClip idle = LoadClip(IdleFbx);
        AnimationClip walk = LoadClip(WalkFbx);
        AnimationClip scream = LoadClip(ScreamFbx);
        AnimationClip invocation = LoadClip(InvocationFbx);
        AnimationClip beam = LoadClip(MeteorBeamFbx);
        AnimationClip shower = LoadClip(MeteorShowerFbx);

        if (idle == null || walk == null || scream == null || invocation == null || beam == null || shower == null)
        {
            Debug.LogError("[Morgana Setup] Falta algun clip de fase 2. Iddle=" + idle + " Walk=" + walk + " Scream=" + scream +
                           " Invocation=" + invocation + " MeteorBeam=" + beam + " MeteorShower=" + shower);
            return null;
        }

        // Si ya existe se vacia en vez de borrarlo, asi conserva su GUID y nada pierde la referencia
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);

        if (controller == null)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        }
        else
        {
            ClearController(controller);
        }

        controller.AddParameter(ParamIsMoving, AnimatorControllerParameterType.Bool);
        controller.AddParameter(ParamScream, AnimatorControllerParameterType.Trigger);
        controller.AddParameter(ParamInvocation, AnimatorControllerParameterType.Trigger);
        controller.AddParameter(ParamMeteorBeam, AnimatorControllerParameterType.Trigger);
        controller.AddParameter(ParamMeteorShower, AnimatorControllerParameterType.Trigger);

        AnimatorStateMachine sm = controller.layers[0].stateMachine;

        AnimatorState idleState = sm.AddState(IdleState, new Vector3(300, 0, 0));
        idleState.motion = idle;
        sm.defaultState = idleState;

        AnimatorState walkState = sm.AddState("Walk", new Vector3(300, 120, 0));
        walkState.motion = walk;

        AnimatorStateTransition toWalk = idleState.AddTransition(walkState);
        toWalk.hasExitTime = false;
        toWalk.duration = 0.15f;
        toWalk.AddCondition(AnimatorConditionMode.If, 0, ParamIsMoving);

        AnimatorStateTransition toIdle = walkState.AddTransition(idleState);
        toIdle.hasExitTime = false;
        toIdle.duration = 0.15f;
        toIdle.AddCondition(AnimatorConditionMode.IfNot, 0, ParamIsMoving);

        AddAction(sm, idleState, "Scream", scream, ParamScream, new Vector3(600, -120, 0));
        AddAction(sm, idleState, "Invocation", invocation, ParamInvocation, new Vector3(600, 0, 0));
        AddAction(sm, idleState, "MeteorBeam", beam, ParamMeteorBeam, new Vector3(600, 120, 0));
        AddAction(sm, idleState, "MeteorShower", shower, ParamMeteorShower, new Vector3(600, 240, 0));

        EditorUtility.SetDirty(controller);
        Debug.Log("[Morgana Setup] Controller listo: " + ControllerPath);

        return controller;
    }

    // Any State -> accion con su trigger, y al terminar el clip vuelve al Iddle
    private static void AddAction(AnimatorStateMachine sm, AnimatorState idle, string stateName, AnimationClip clip, string trigger, Vector3 pos)
    {
        AnimatorState state = sm.AddState(stateName, pos);
        state.motion = clip;

        AnimatorStateTransition enter = sm.AddAnyStateTransition(state);
        enter.hasExitTime = false;
        enter.duration = 0.1f;
        enter.canTransitionToSelf = false;
        enter.AddCondition(AnimatorConditionMode.If, 0, trigger);

        AnimatorStateTransition exit = state.AddTransition(idle);
        exit.hasExitTime = true;
        exit.exitTime = 0.95f;
        exit.duration = 0.15f;
    }

    private static void ClearController(AnimatorController controller)
    {
        foreach (AnimatorControllerParameter p in controller.parameters.ToArray())
        {
            controller.RemoveParameter(p);
        }

        AnimatorStateMachine sm = controller.layers[0].stateMachine;

        foreach (AnimatorStateTransition t in sm.anyStateTransitions.ToArray())
        {
            sm.RemoveAnyStateTransition(t);
        }

        foreach (ChildAnimatorState s in sm.states.ToArray())
        {
            sm.RemoveState(s.state);
        }
    }

    // ---------- PREFAB ----------

    private static void AssignToBossPrefab(AnimatorController controller)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(BossPrefabPath);

        try
        {
            Boss_MedusaMorgana boss = root.GetComponentInChildren<Boss_MedusaMorgana>(true);

            if (boss == null)
            {
                Debug.LogError("[Morgana Setup] " + BossPrefabPath + " no tiene Boss_MedusaMorgana");
                return;
            }

            GameObject phaseTwo = new SerializedObject(boss).FindProperty("phaseTwoModel").objectReferenceValue as GameObject;

            if (phaseTwo == null)
            {
                Debug.LogError("[Morgana Setup] phaseTwoModel no esta asignado en el prefab");
                return;
            }

            Animator animator = phaseTwo.GetComponentInChildren<Animator>(true);

            if (animator == null)
            {
                animator = phaseTwo.AddComponent<Animator>();
                Debug.Log("[Morgana Setup] Se agrego un Animator al modelo de fase 2");
            }

            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;

            if (animator.avatar == null)
            {
                Avatar avatar = AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<Avatar>().FirstOrDefault();
                if (avatar != null) animator.avatar = avatar;
            }

            PrefabUtility.SaveAsPrefabAsset(root, BossPrefabPath);
            Debug.Log("[Morgana Setup] Controller de fase 2 asignado a '" + animator.name + "' en " + BossPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    // ---------- VALIDACION ----------

    private static void ReadBossFrames(ref int scream, ref int climax, ref int beam, ref int shower, List<string> problems)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BossPrefabPath);
        Boss_MedusaMorgana boss = prefab != null ? prefab.GetComponentInChildren<Boss_MedusaMorgana>(true) : null;

        if (boss == null)
        {
            problems.Add("No se pudo leer Boss_MedusaMorgana en " + BossPrefabPath);
            return;
        }

        SerializedObject so = new SerializedObject(boss);
        scream = ReadInt(so, "phaseTwoScreamFrames", scream);
        climax = ReadInt(so, "climaxFrames", climax);
        beam = ReadInt(so, "meteorBeamFrames", beam);
        shower = ReadInt(so, "meteorShowerFrames", shower);
    }

    private static int ReadInt(SerializedObject so, string field, int fallback)
    {
        SerializedProperty p = so.FindProperty(field);
        return p != null ? p.intValue : fallback;
    }

    private static void CheckClip(string fbxPath, int expectedFrames, string field, bool mustLoop, List<string> problems)
    {
        AnimationClip clip = LoadClip(fbxPath);

        if (clip == null)
        {
            problems.Add("No hay clip en " + fbxPath);
            return;
        }

        int frames = Mathf.RoundToInt(clip.length * clip.frameRate);

        if (expectedFrames > 0 && Mathf.Abs(frames - expectedFrames) > 1)
        {
            problems.Add("'" + clip.name + "' dura " + frames + " frames a " + clip.frameRate + " fps, pero " + field +
                         " en el boss es " + expectedFrames + ". Cambia el campo o el Last Frame del import.");
        }

        if (mustLoop && !clip.isLooping)
        {
            problems.Add("'" + clip.name + "' no esta en loop (" + fbxPath + ")");
        }
    }

    private static void CheckPrefabAnimator(List<string> problems)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BossPrefabPath);
        Boss_MedusaMorgana boss = prefab != null ? prefab.GetComponentInChildren<Boss_MedusaMorgana>(true) : null;
        if (boss == null) return;

        GameObject phaseTwo = new SerializedObject(boss).FindProperty("phaseTwoModel").objectReferenceValue as GameObject;

        if (phaseTwo == null)
        {
            problems.Add("phaseTwoModel no esta asignado en el prefab");
            return;
        }

        Animator animator = phaseTwo.GetComponentInChildren<Animator>(true);

        if (animator == null)
        {
            problems.Add("El modelo de fase 2 no tiene Animator");
            return;
        }

        if (animator.runtimeAnimatorController == null)
        {
            problems.Add("El Animator de fase 2 no tiene controller (corre 'Tools/Morgana/1. Configurar Animator de fase 2')");
        }
    }
}
