using UnityEditor;
using ATEditor.Editor;
using UnityEngine;

namespace Game.Adapters.Editor
{
    /// <summary>
    /// 娉ㄥ叆涓氬姟宸ュ巶鍒版妧鑳界紪杈戝櫒涓?
    /// 瀛樺湪浜?Editor 绋嬪簭闆嗭紝鍙互闅愬紡寮曠敤 Runtime 绋嬪簭闆嗕腑鐨勬父鎴忓眰涓庣紪杈戝櫒灞傘€?
    /// 杩欐牱灏卞疄鐜颁簡涓ょ鐗╃悊闅旂鑰屼笉鍙戠敓绋嬪簭闆嗙紪璇戦敊璇€?
    /// </summary>
    public static class SkillServiceFactoryRegister
    {
        [InitializeOnLoadMethod]
        private static void RegisterToSkillEditor()
        {
            // 灏嗕笟鍔″眰鐨?Service Factory 濮旀墭缁欐牳蹇冪紪杈戝櫒
            ATEditorGlobalSettings.DefaultServiceFactoryCreator = owner => ATServiceFactory.ProvideService;
            
            // 娉ㄥ唽缂栬緫鍣ㄧ獥鍙ｅ叧鍋滃悗缃竻鐞嗗姩浣滐紙闃蹭涪鍘婚噸锛?
            ATEditorGlobalSettings.OnEditorDispose -= ATServiceFactory.ClearAllStaticCaches;
            ATEditorGlobalSettings.OnEditorDispose += ATServiceFactory.ClearAllStaticCaches;
        }
    }
}
