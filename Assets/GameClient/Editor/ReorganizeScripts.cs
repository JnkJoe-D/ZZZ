using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public class ReorganizeScripts
{
    static ReorganizeScripts()
    {
        EditorApplication.delayCall += DoReorganize;
    }

    public static void DoReorganize()
    {
        if (SessionState.GetBool("HasReorganizedLogic", false))
            return;
        SessionState.SetBool("HasReorganizedLogic", true);

        Debug.Log("Starting Directory Reorganization...");

        string[] dirs = new string[]
        {
            "Assets/GameClient/Logic/Entity",
            "Assets/GameClient/Logic/Action",
            "Assets/GameClient/Logic/Input",
            "Assets/GameClient/Logic/Movement",
            "Assets/GameClient/Logic/Combat",
            "Assets/GameClient/Logic/Team",
            "Assets/GameClient/Logic/Debug"
        };

        foreach (var dir in dirs)
        {
            if (!AssetDatabase.IsValidFolder(dir))
            {
                string[] parts = dir.Split('/');
                string current = parts[0];
                for (int i = 1; i < parts.Length; i++)
                {
                    string parent = current;
                    current = current + "/" + parts[i];
                    if (!AssetDatabase.IsValidFolder(current))
                    {
                        AssetDatabase.CreateFolder(parent, parts[i]);
                    }
                }
            }
        }

        void Move(string src, string dst)
        {
            if (AssetDatabase.LoadMainAssetAtPath(src) != null || AssetDatabase.IsValidFolder(src))
            {
                string msg = AssetDatabase.MoveAsset(src, dst);
                if (!string.IsNullOrEmpty(msg))
                    Debug.LogWarning("Failed to move " + src + " to " + dst + ": " + msg);
            }
        }

        // Entity
        Move("Assets/GameClient/Logic/Character/Entity/CharacterEntity.cs", "Assets/GameClient/Logic/Entity/CharacterEntity.cs");
        Move("Assets/GameClient/Logic/Character/Entity/RoleEntity.cs", "Assets/GameClient/Logic/Entity/RoleEntity.cs");
        Move("Assets/GameClient/Logic/Character/Entity/CharacterTeamContext.cs", "Assets/GameClient/Logic/Entity/CharacterTeamContext.cs");
        Move("Assets/GameClient/Logic/Character/Entity/CameraPointBinder.cs", "Assets/GameClient/Logic/Entity/CameraPointBinder.cs");
        Move("Assets/GameClient/Logic/Character/CharacterRuntimeData.cs", "Assets/GameClient/Logic/Entity/CharacterRuntimeData.cs");
        Move("Assets/GameClient/Logic/Character/CharacterEvents.cs", "Assets/GameClient/Logic/Entity/CharacterEvents.cs");
        Move("Assets/GameClient/Logic/Character/TargetFinder.cs", "Assets/GameClient/Logic/Entity/TargetFinder.cs");
        Move("Assets/GameClient/Logic/Character/Status", "Assets/GameClient/Logic/Entity/Status");
        Move("Assets/GameClient/Logic/Character/States", "Assets/GameClient/Logic/Entity/States");

        // Action
        Move("Assets/GameClient/Logic/Skill/ActionManager.cs", "Assets/GameClient/Logic/Action/ActionManager.cs");
        Move("Assets/GameClient/Logic/Character/ActionPlayer.cs", "Assets/GameClient/Logic/Action/ActionPlayer.cs");
        Move("Assets/GameClient/Logic/Skill/Combo/ActionController.cs", "Assets/GameClient/Logic/Action/ActionController.cs");
        Move("Assets/GameClient/Logic/Skill/Combo/ActionRoute.cs", "Assets/GameClient/Logic/Action/ActionRoute.cs");
        Move("Assets/GameClient/Logic/Skill/Combo/TransitionConditions.cs", "Assets/GameClient/Logic/Action/TransitionConditions.cs");
        Move("Assets/GameClient/Logic/Skill/Combo/ITransitionCondition.cs", "Assets/GameClient/Logic/Action/ITransitionCondition.cs");
        Move("Assets/GameClient/Logic/Skill/Combo/ComboTransition.cs", "Assets/GameClient/Logic/Action/ComboTransition.cs");
        Move("Assets/GameClient/Logic/Character/Motion", "Assets/GameClient/Logic/Action/Motion");
        Move("Assets/GameClient/Logic/Character/Mechanic", "Assets/GameClient/Logic/Action/Mechanic");

        // Input
        Move("Assets/GameClient/Logic/Skill/Combo/CommandBuffer.cs", "Assets/GameClient/Logic/Input/CommandBuffer.cs");
        Move("Assets/GameClient/Logic/Character/BufferedInputType.cs", "Assets/GameClient/Logic/Input/BufferedInputType.cs");
        Move("Assets/GameClient/Logic/Character/IActionCommandHandler.cs", "Assets/GameClient/Logic/Input/IActionCommandHandler.cs");
        Move("Assets/GameClient/Logic/Character/Commands", "Assets/GameClient/Logic/Input/Commands");

        // Movement
        Move("Assets/GameClient/Logic/Character/CharacterMotor.cs", "Assets/GameClient/Logic/Movement/CharacterMotor.cs");
        Move("Assets/GameClient/Logic/Character/ICharacterMotor.cs", "Assets/GameClient/Logic/Movement/ICharacterMotor.cs");
        Move("Assets/GameClient/Logic/Character/FootIKModule.cs", "Assets/GameClient/Logic/Movement/FootIKModule.cs");

        // Combat
        Move("Assets/GameClient/Logic/Character/HitReactionModule.cs", "Assets/GameClient/Logic/Combat/HitReactionModule.cs");
        Move("Assets/GameClient/Logic/Character/HitContext.cs", "Assets/GameClient/Logic/Combat/HitContext.cs");
        Move("Assets/GameClient/Logic/Character/Impact", "Assets/GameClient/Logic/Combat/Impact");

        // Team
        Move("Assets/GameClient/Logic/Character/TeamManager.cs", "Assets/GameClient/Logic/Team/TeamManager.cs");
        Move("Assets/GameClient/Logic/Character/SwitchExecutor.cs", "Assets/GameClient/Logic/Team/SwitchExecutor.cs");

        // Debug
        Move("Assets/GameClient/Logic/Character/CharacterDebugHUD.cs", "Assets/GameClient/Logic/Debug/CharacterDebugHUD.cs");
        Move("Assets/GameClient/Logic/Character/Test_Character.cs", "Assets/GameClient/Logic/Debug/Test_Character.cs");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Reorganization Complete!");
    }
}
