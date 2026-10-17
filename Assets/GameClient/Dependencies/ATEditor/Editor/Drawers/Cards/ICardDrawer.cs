namespace ATEditor.Editor
{
    /// <summary>
    /// 卡片抽屉通用接口
    /// 每个卡片负责自身的 Inspector 字段绘制与 SceneView 辅助渲染
    /// </summary>
    public interface ICardDrawer
    {
        /// <summary> 卡片标题 </summary>
        string CardTitle { get; }

        /// <summary> 是否为必须卡片（默认打勾且不可取消打勾） </summary>
        bool IsMandatory { get; }

        /// <summary> 是否已启用 </summary>
        bool IsEnabled { get; set; }

        /// <summary> 是否在 Inspector 中展开 </summary>
        bool IsExpanded { get; set; }

        /// <summary> 绘制卡片 Inspector 面板 </summary>
        void DrawInspector(CardDrawContext context);

        /// <summary> 绘制卡片在 SceneView 中的辅助 Gizmos / Handles </summary>
        void DrawSceneGUI(CardDrawContext context);
    }
}
