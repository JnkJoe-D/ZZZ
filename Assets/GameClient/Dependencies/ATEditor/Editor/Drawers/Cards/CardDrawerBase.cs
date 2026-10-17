using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace ATEditor.Editor
{
    /// <summary>
    /// 卡片抽屉模板基类
    /// 继承自 ActionInspectorBase 以获得成熟的属性反射绘制能力（包含 LayerMask, HitBoxShape, ReorderableList 等）
    /// 封装类似 Unity 原生粒子系统模块的统一 Header 交互（左侧 Checkbox + 展开折叠）
    /// </summary>
    /// <typeparam name="TData">卡片绑定的纯 C# 数据配置对象类型</typeparam>
    public abstract class CardDrawerBase<TData> : ActionInspectorBase, ICardDrawer where TData : class
    {
        // 静态折叠状态缓存，保证 Drawer 每次重构或重绘时折叠状态永不丢失
        private static readonly Dictionary<string, bool> _cardExpandedCache = new Dictionary<string, bool>();

        public abstract string CardTitle { get; }

        /// <summary> 是否为必须卡片（必要卡片默认打勾且不可取消打勾） </summary>
        public virtual bool IsMandatory => false;

        public bool IsEnabled { get; set; } = true;

        private string _cardKey;
        /// <summary> 卡片唯一标识键，用于跨帧跨实例维持展开/折叠状态 </summary>
        public string CardKey
        {
            get => !string.IsNullOrEmpty(_cardKey) ? _cardKey : $"{GetType().Name}_{CardTitle}";
            set => _cardKey = value;
        }

        public bool IsExpanded
        {
            get
            {
                if (!_cardExpandedCache.TryGetValue(CardKey, out bool expanded))
                {
                    expanded = true; // 默认展开
                    _cardExpandedCache[CardKey] = expanded;
                }
                return expanded;
            }
            set
            {
                _cardExpandedCache[CardKey] = value;
            }
        }

        /// <summary> 当前绑定的数据引用 </summary>
        public TData Data { get; protected set; }

        public virtual void BindData(TData data)
        {
            Data = data;
        }

        public virtual void DrawInspector(CardDrawContext context)
        {
            if (Data == null) return;

            // 保持 Undo 目标同步
            UndoContext = context.UndoContext;

            // 1. 绘制粒子系统风格的 Header 栏 (左侧 Checkbox，右侧折叠标题)
            bool prevEnabled = IsEnabled;
            bool newEnabled = prevEnabled;
            bool newExpanded = IsExpanded;

            DrawParticleStyleHeader(CardTitle, IsMandatory, ref newEnabled, ref newExpanded);

            if (newEnabled != prevEnabled)
            {
                IsEnabled = newEnabled;
                context.MarkDirty($"切换 {CardTitle} 状态");
                OnEnabledChanged(newEnabled, context);
            }
            IsExpanded = newExpanded;

            // 2. 展开时绘制卡片内部字段容器
            if (IsExpanded)
            {
                EditorGUILayout.BeginVertical("box");
                using (new EditorGUI.DisabledScope(!IsEnabled))
                {
                    OnDrawBody(context);
                }
                EditorGUILayout.EndVertical();
            }
        }

        public virtual void DrawSceneGUI(CardDrawContext context)
        {
            if (!IsEnabled || Data == null) return;
            OnDrawSceneGUI(context);
        }

        /// <summary> 子类实现：绘制卡片主体字段 </summary>
        protected abstract void OnDrawBody(CardDrawContext context);

        /// <summary> 子类实现：在 SceneView 中绘制辅助图形 (Gizmos/Handles) </summary>
        protected virtual void OnDrawSceneGUI(CardDrawContext context) { }

        /// <summary> 当启用状态改变时回调 </summary>
        protected virtual void OnEnabledChanged(bool isEnabled, CardDrawContext context) { }

        /// <summary>
        /// 核心：绘制类似 Unity 原生粒子系统 (Particle System) 的 Module Header
        /// 左侧内置复选框（与标题保持足够舒适间距），点击整条标题栏任意位置均可切换展开/折叠
        /// </summary>
        public static void DrawParticleStyleHeader(string title, bool isMandatory, ref bool isEnabled, ref bool isExpanded)
        {
            float height = 24f;
            Rect headerRect = GUILayoutUtility.GetRect(0, height, GUILayout.ExpandWidth(true));

            // 背景底色与分隔线
            Color bgColor = EditorGUIUtility.isProSkin ? new Color(0.22f, 0.22f, 0.22f, 1f) : new Color(0.75f, 0.75f, 0.75f, 1f);
            Color borderColor = EditorGUIUtility.isProSkin ? new Color(0.12f, 0.12f, 0.12f, 1f) : new Color(0.6f, 0.6f, 0.6f, 1f);

            EditorGUI.DrawRect(headerRect, bgColor);
            EditorGUI.DrawRect(new Rect(headerRect.x, headerRect.y + height - 1, headerRect.width, 1), borderColor);

            // 1. 左侧复选框 (x+6, y+4, 16x16, 边缘到 x+22)
            Rect toggleRect = new Rect(headerRect.x + 6, headerRect.y + 4, 16, 16);
            if (isMandatory)
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUI.Toggle(toggleRect, true);
                }
                isEnabled = true;
            }
            else
            {
                isEnabled = EditorGUI.Toggle(toggleRect, isEnabled);
            }

            // 2. 右侧折叠与标题 (从 x+28 开始，留出充足间距，绝不挤压)
            Rect foldoutRect = new Rect(headerRect.x + 28, headerRect.y, headerRect.width - 28, height);
            GUIStyle labelStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 12,
                padding = new RectOffset(4, 0, 0, 0)
            };

            if (!isEnabled)
            {
                labelStyle.normal.textColor = Color.gray;
            }

            // 3. 交互保障：点击整条 Header 栏任意位置（除 Toggle 框外）均可切换展开/折叠
            Event evt = Event.current;
            if (evt.type == EventType.MouseDown && evt.button == 0)
            {
                if (headerRect.Contains(evt.mousePosition) && !toggleRect.Contains(evt.mousePosition))
                {
                    isExpanded = !isExpanded;
                    GUI.changed = true;
                    evt.Use();
                }
            }

            // 绘制折叠小箭头与标题文本 (toggleOnLabelClick 设为 false，由上方统一处理全栏点击)
            EditorGUI.Foldout(foldoutRect, isExpanded, title, false, labelStyle);
        }

        #region 反射辅助方法

        /// <summary>
        /// 辅助反射绘制指定对象中的指定字段
        /// </summary>
        public void DrawSubField(object target, string fieldName)
        {
            if (target == null) return;
            var field = target.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null)
            {
                DrawField(field, target);
            }
        }

        #endregion
    }
}
