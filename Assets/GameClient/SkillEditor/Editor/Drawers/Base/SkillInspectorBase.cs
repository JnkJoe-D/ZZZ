using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using SkillEditor;
using Object = UnityEngine.Object;

namespace SkillEditor.Editor
{
    /// <summary>
    /// 基于反射的 Inspector 基类
    /// </summary>
    public class SkillInspectorBase
    {
        public Object[] UndoContext { get; set; }

        /// <summary>
        /// 值发生改变时触发
        /// </summary>
        public event System.Action OnInspectorChanged;

        public virtual void DrawInspector(object target)
        {
            if (target == null) return;
            DrawDefaultInspector(target);
        }

        protected void DrawDefaultInspector(object obj)
        {
            var targetType = obj.GetType();
            
            // 获取继承链 (Base -> Derived)
            var typeHierarchy = new Stack<Type>();
            var current = targetType;
            while (current != null && current != typeof(object))
            {
                typeHierarchy.Push(current);
                current = current.BaseType;
            }

            // 按顺序绘制每一层的字段和属性
            foreach (var type in typeHierarchy)
            {
                FieldInfo[] fieldInfos = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                
                foreach (var field in fieldInfos)
                {
                    if (!field.IsPublic && !field.IsDefined(typeof(SkillPropertyAttribute), true))
                    {
                        continue;
                    }

                    if (ShouldShow(field, obj))
                    {
                        DrawField(field, obj);
                    }
                }

                PropertyInfo[] propInfos = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                
                foreach (var prop in propInfos)
                {
                    if (prop.IsDefined(typeof(SkillPropertyAttribute), true))
                    {
                        if (prop.CanRead && prop.CanWrite && ShouldShowProperty(prop, obj))
                        {
                            DrawProperty(prop, obj);
                        }
                    }
                }
            }

            // 全局兜底校验：防止因直接缩短 duration 导致 blendIn + blendOut > duration
            if (obj is ClipBase clipBase && clipBase.SupportsBlending)
            {
                if (clipBase.BlendInDuration + clipBase.BlendOutDuration > clipBase.Duration)
                {
                    // 若超出总长，则按比例缩小，或简单地把二者都按其相对比例分配到剩余的 duration 里
                    float totalBlend = clipBase.BlendInDuration + clipBase.BlendOutDuration;
                    if (totalBlend > 0)
                    {
                        float ratioIn = clipBase.BlendInDuration / totalBlend;
                        float ratioOut = clipBase.BlendOutDuration / totalBlend;
                        
                        // 强制写回，不走反射流程以防递归
                        clipBase.BlendInDuration = clipBase.Duration * ratioIn;
                        clipBase.BlendOutDuration = clipBase.Duration * ratioOut;
                        
                        // 由于我们在此处强制修改了数据，可能需要标记 Dirty 等，但 Inspector 基类通常只抛出 Changed
                        if (UndoContext != null && UndoContext.Length > 0)
                        {
                            Undo.RecordObjects(UndoContext, "Auto Clamp Blend Durations");
                        }
                    }
                }
            }
        }

        protected virtual bool ShouldShow(FieldInfo field, object obj)
        {
            if (field.IsDefined(typeof(HideInInspector), true)) return false;
            
            var showIf = field.GetCustomAttribute<ShowIfAttribute>();
            if (showIf != null)
            {
                var sourceField = obj.GetType().GetField(showIf.conditionalSourceField, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (sourceField != null)
                {
                    var sourceValue = sourceField.GetValue(obj);
                    if (!object.Equals(sourceValue, showIf.expectedValue)&& showIf.isEqual)
                        return false;
                    else if(object.Equals(sourceValue, showIf.expectedValue) && !showIf.isEqual)
                        return false;
                }
            }
            
            // 硬编码的 Blending 逻辑 
            if (field.Name == "blendInDuration" || field.Name == "blendOutDuration")
            {
                if (obj is ClipBase c && !c.SupportsBlending) return false;
            }

            // 硬编码的 CustomBoneName 显示逻辑
            if (field.Name == "customBoneName")
            {
                var bindPointField = obj.GetType().GetField("bindPoint", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (bindPointField != null)
                {
                    var bindPointVal = bindPointField.GetValue(obj);
                    if (bindPointVal is BindPoint bp && bp != BindPoint.CustomBone)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        protected virtual bool ShouldShowProperty(PropertyInfo prop, object obj)
        {
            // 额外的动态过滤可以在这里写
            return true;
        }

        protected virtual void DrawProperty(PropertyInfo prop, object obj)
        {
            var value = prop.GetValue(obj);
            var propType = prop.PropertyType;
            var attribute = prop.GetCustomAttribute<SkillPropertyAttribute>();
            var name = attribute != null ? attribute.Name : ObjectNames.NicifyVariableName(prop.Name);

            object newValue = value;
            EditorGUI.BeginChangeCheck();

            if (propType == typeof(int)) { newValue = EditorGUILayout.IntField(name, (int)value); }
            else if (propType == typeof(float)) 
            { 
                if (prop.Name == "BlendInDuration" || prop.Name == "BlendOutDuration")
                {
                    float maxDuration = 10f;
                    // if (obj is ClipBase c) 
                    // {
                    //     if (prop.Name == "BlendInDuration")
                    //     {
                    //         maxDuration = Mathf.Max(0f, c.Duration - c.BlendOutDuration);
                    //     }
                    //     else
                    //     {
                    //         maxDuration = Mathf.Max(0f, c.Duration - c.BlendInDuration);
                    //     }
                    // }
                    newValue = EditorGUILayout.Slider(name, (float)value, 0f, maxDuration);
                }
                else
                {
                    float floatVal = EditorGUILayout.FloatField(name, (float)value);
                    // if (prop.Name == "StartTime")
                    // {
                    //     floatVal = Mathf.Max(0f, floatVal);
                    // }
                    // if (prop.Name == "Duration")
                    // {
                    //     floatVal = Mathf.Max(0.1f, floatVal);
                    // }
                    newValue = floatVal;
                }
            }
            else if (propType == typeof(bool)) { newValue = EditorGUILayout.Toggle(name, (bool)value); }
            else if (propType == typeof(string)) { newValue = EditorGUILayout.TextField(name, (string)value); }
            else if (propType == typeof(Vector2)) { newValue = EditorGUILayout.Vector2Field(name, (Vector2)value); }
            else if (propType == typeof(Vector3)) { newValue = EditorGUILayout.Vector3Field(name, (Vector3)value); }
            else if (propType == typeof(Color)) { newValue = EditorGUILayout.ColorField(name, (Color)value); }
            else if (propType == typeof(AnimationCurve)) { newValue = EditorGUILayout.CurveField(name, (AnimationCurve)value ?? new AnimationCurve()); }
            else if (typeof(Object).IsAssignableFrom(propType)) { newValue = EditorGUILayout.ObjectField(name, (Object)value, propType, false); }
            else if (propType.IsEnum) { newValue = EditorGUILayout.EnumPopup(name, (Enum)value); }

            if (EditorGUI.EndChangeCheck())
            {
                if (UndoContext != null && UndoContext.Length > 0)
                {
                    Undo.RecordObjects(UndoContext, "Inspector Change: " + name);
                }
                prop.SetValue(obj, newValue);
                OnInspectorChanged?.Invoke();
            }
        }

        protected virtual void DrawField(FieldInfo field, object obj)
        {
            var value = field.GetValue(obj);
            var fieldType = field.FieldType;
            var attribute = field.GetCustomAttribute<SkillPropertyAttribute>();
            var name = attribute != null ? attribute.Name : ObjectNames.NicifyVariableName(field.Name);

            object newValue = value;

            EditorGUI.BeginChangeCheck();

            if (fieldType == typeof(int))
            {
                newValue = EditorGUILayout.IntField(name, (int)value);
            }
            else if (fieldType == typeof(float))
            {
                // 特殊处理：如果是 startTime/duration 等需要限制非负
                // 也可以引入 [Min] 属性
                
                if (field.Name == "blendInDuration" || field.Name == "blendOutDuration")
                {
                    float maxDuration = 10f;
                    if (obj is ClipBase c) 
                    {
                        // 动态阻隔：最大范围为总时长 - 另一端的时长
                        if (field.Name == "blendInDuration")
                        {
                            maxDuration = Mathf.Max(0f, c.Duration - c.BlendOutDuration);
                        }
                        else
                        {
                            maxDuration = Mathf.Max(0f, c.Duration - c.BlendInDuration);
                        }
                    }
                    
                    newValue = EditorGUILayout.Slider(name, (float)value, 0f, maxDuration);
                }
                else
                {
                    float floatVal = EditorGUILayout.FloatField(name, (float)value);
                    if (field.Name == "startTime")
                    {
                        floatVal = Mathf.Max(0f, floatVal);
                    }
                    if (field.Name == "duration")
                    {
                        floatVal = Mathf.Max(0.1f, floatVal);
                    }
                    newValue = floatVal;
                }
            }
            else if (fieldType == typeof(bool))
            {
                newValue = EditorGUILayout.Toggle(name, (bool)value);
            }
            else if (fieldType == typeof(string))
            {
                newValue = EditorGUILayout.TextField(name, (string)value);
            }
            else if (fieldType == typeof(Vector2))
            {
                newValue = EditorGUILayout.Vector2Field(name, (Vector2)value);
            }
            else if (fieldType == typeof(Vector3))
            {
                newValue = EditorGUILayout.Vector3Field(name, (Vector3)value);
            }
            else if (fieldType == typeof(Color))
            {
                newValue = EditorGUILayout.ColorField(name, (Color)value);
            }
            else if (fieldType == typeof(AnimationCurve))
            {
                newValue = EditorGUILayout.CurveField(name, (AnimationCurve)value ?? new AnimationCurve());
            }
            else if (typeof(Object).IsAssignableFrom(fieldType))
            {
                newValue = EditorGUILayout.ObjectField(name, (Object)value, fieldType, false);
            }
            else if (fieldType.IsEnum)
            {
                newValue = EditorGUILayout.EnumPopup(name, (Enum)value);
            }
            else if (fieldType == typeof(LayerMask))
            {
                // unity's built in LayerMask extension for EditorGUILayout
                LayerMask tempMask = (LayerMask)value;
                int maskField = EditorGUILayout.MaskField(name, InternalEditorUtility.LayerMaskToConcatenatedLayersMask(tempMask), InternalEditorUtility.layers);
                newValue = InternalEditorUtility.ConcatenatedLayersMaskToLayerMask(maskField);
            }
            else if (value is HitBoxShape shape)
            {
                EditorGUILayout.LabelField(name, EditorStyles.boldLabel);
                EditorGUI.indentLevel++;
                
                shape.shapeType = (HitBoxType)EditorGUILayout.EnumPopup("形状类型", shape.shapeType);
                if (shape.shapeType == HitBoxType.Box)
                {
                    shape.size = EditorGUILayout.Vector3Field("尺寸 (Box)", shape.size);
                }
                if (shape.shapeType == HitBoxType.Sphere || shape.shapeType == HitBoxType.Capsule || shape.shapeType == HitBoxType.Sector || shape.shapeType == HitBoxType.Ring)
                {
                    shape.radius = EditorGUILayout.FloatField("半径", shape.radius);
                }
                if (shape.shapeType == HitBoxType.Capsule || shape.shapeType == HitBoxType.Ring || shape.shapeType == HitBoxType.Sector)
                {
                    shape.height = EditorGUILayout.FloatField("高度", shape.height);
                }
                if (shape.shapeType == HitBoxType.Sector)
                {
                    shape.angle = EditorGUILayout.Slider("角度", shape.angle, 0f, 360f);
                }
                if (shape.shapeType == HitBoxType.Ring)
                {
                    shape.innerRadius = EditorGUILayout.FloatField("内半径", shape.innerRadius);
                }
                EditorGUI.indentLevel--;
                newValue = shape;
            }
            else if (value is List<SkillEventParam> paramList)
            {
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(name, EditorStyles.boldLabel);
                if (GUILayout.Button("+", GUILayout.Width(20)))
                {
                    paramList.Add(new SkillEventParam());
                    GUI.FocusControl(null);
                }
                EditorGUILayout.EndHorizontal();

                for (int i = 0; i < paramList.Count; i++)
                {
                    var p = paramList[i];
                    EditorGUILayout.BeginVertical("helpbox");
                    EditorGUILayout.BeginHorizontal();
                    p.key = EditorGUILayout.TextField("参数名", p.key);
                    if (GUILayout.Button("X", GUILayout.Width(20)))
                    {
                        paramList.RemoveAt(i);
                        GUI.FocusControl(null);
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.EndVertical();
                        break;
                    }
                    EditorGUILayout.EndHorizontal();
                    p.stringValue = EditorGUILayout.TextField("字符串", p.stringValue);
                    p.floatValue = EditorGUILayout.FloatField("浮点数", p.floatValue);
                    p.intValue = EditorGUILayout.IntField("整数", p.intValue);
                    EditorGUILayout.EndVertical();
                }
                EditorGUILayout.EndVertical();
                newValue = paramList;
            }
            else if (fieldType == typeof(string[]))
            {
                var stringArray = (string[])value;
                if (stringArray == null) stringArray = new string[0];

                // 尝试搜寻本地配置
                string[] availableTargetTagsArray = null;
                var guids = AssetDatabase.FindAssets("t:SkillTagConfig");
                if (guids.Length > 0)
                {
                    var config = AssetDatabase.LoadAssetAtPath<global::SkillEditor.SkillTagConfig>(AssetDatabase.GUIDToAssetPath(guids[0]));
                    if (config != null && config.availableTargetTags != null)
                    {
                        availableTargetTagsArray = config.availableTargetTags.ToArray();
                    }
                }

                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(name, EditorStyles.boldLabel);
                
                // 没找到配置库警告
                if (availableTargetTagsArray == null || availableTargetTagsArray.Length == 0)
                {
                    EditorGUILayout.HelpBox("未找到 SkillTagConfig 资产！", MessageType.Warning);
                }

                if (GUILayout.Button("+", GUILayout.Width(20)))
                {
                    Array.Resize(ref stringArray, stringArray.Length + 1);
                    stringArray[stringArray.Length - 1] = (availableTargetTagsArray != null && availableTargetTagsArray.Length > 0) ? availableTargetTagsArray[0] : "";
                    GUI.FocusControl(null);
                }
                EditorGUILayout.EndHorizontal();

                for (int i = 0; i < stringArray.Length; i++)
                {
                    EditorGUILayout.BeginHorizontal();

                    string currentVal = stringArray[i];

                    if (availableTargetTagsArray != null && availableTargetTagsArray.Length > 0)
                    {
                        int currentIndex = Array.IndexOf(availableTargetTagsArray, currentVal);

                        if (currentIndex == -1) // 词库丢失或非法值
                        {
                            var oldColor = GUI.color;
                            GUI.color = Color.red;
                            stringArray[i] = EditorGUILayout.TextField($"[已丢失] {currentVal}");
                            GUI.color = oldColor;
                        }
                        else
                        {
                            int newIndex = EditorGUILayout.Popup(currentIndex, availableTargetTagsArray);
                            stringArray[i] = availableTargetTagsArray[newIndex];
                        }
                    }
                    else
                    {
                        // 兜底方案：如果没有预设库，退化为文本框
                        stringArray[i] = EditorGUILayout.TextField(stringArray[i]);
                    }

                    if (GUILayout.Button("X", GUILayout.Width(20)))
                    {
                        var list = new List<string>(stringArray);
                        list.RemoveAt(i);
                        stringArray = list.ToArray();
                        GUI.FocusControl(null);
                        EditorGUILayout.EndHorizontal();
                        break;
                    }
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndVertical();
                newValue = stringArray;
            }
            else if (value is HitEffectEntry[] hitEffectsArray)
            {
                // --- HitEffectEntry[] 渲染 ---
                // 加载 SkillTagConfig
                string[] eventTagOptions = null;
                string[] targetTagOptions = null;
                var tagGuids = AssetDatabase.FindAssets("t:SkillTagConfig");
                if (tagGuids.Length > 0)
                {
                    var tagConfig = AssetDatabase.LoadAssetAtPath<global::SkillEditor.SkillTagConfig>(AssetDatabase.GUIDToAssetPath(tagGuids[0]));
                    if (tagConfig != null)
                    {
                        eventTagOptions = tagConfig.availableEventTags?.ToArray();
                        targetTagOptions = tagConfig.availableTargetTags?.ToArray();
                    }
                }

                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(name, EditorStyles.boldLabel);
                if (GUILayout.Button("+", GUILayout.Width(20)))
                {
                    Array.Resize(ref hitEffectsArray, hitEffectsArray.Length + 1);
                    hitEffectsArray[hitEffectsArray.Length - 1] = new HitEffectEntry();
                    GUI.FocusControl(null);
                }
                EditorGUILayout.EndHorizontal();

                for (int i = 0; i < hitEffectsArray.Length; i++)
                {
                    var entry = hitEffectsArray[i];
                    if (entry == null) { entry = new HitEffectEntry(); hitEffectsArray[i] = entry; }

                    EditorGUILayout.BeginVertical("helpbox");
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"效果 [{i}]", EditorStyles.miniLabel);
                    if (GUILayout.Button("X", GUILayout.Width(20)))
                    {
                        var list = new List<HitEffectEntry>(hitEffectsArray);
                        list.RemoveAt(i);
                        hitEffectsArray = list.ToArray();
                        GUI.FocusControl(null);
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.EndVertical();
                        break;
                    }
                    EditorGUILayout.EndHorizontal();

                    // eventTag 下拉
                    if (eventTagOptions != null && eventTagOptions.Length > 0)
                    {
                        int idx = Array.IndexOf(eventTagOptions, entry.eventTag);
                        if (idx == -1)
                        {
                            var oldC = GUI.color; GUI.color = Color.yellow;
                            entry.eventTag = EditorGUILayout.TextField("效果标签 [未知]", entry.eventTag);
                            GUI.color = oldC;
                        }
                        else
                        {
                            int newIdx = EditorGUILayout.Popup("效果标签", idx, eventTagOptions);
                            entry.eventTag = eventTagOptions[newIdx];
                        }
                    }
                    else
                    {
                        entry.eventTag = EditorGUILayout.TextField("效果标签", entry.eventTag);
                    }

                    // targetTags 下拉列表
                    EditorGUILayout.BeginVertical();
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("目标标签", EditorStyles.miniLabel);
                    if (GUILayout.Button("+", GUILayout.Width(20)))
                    {
                        var tags = entry.targetTags ?? new string[0];
                        Array.Resize(ref tags, tags.Length + 1);
                        tags[tags.Length - 1] = (targetTagOptions != null && targetTagOptions.Length > 0) ? targetTagOptions[0] : "";
                        entry.targetTags = tags;
                        GUI.FocusControl(null);
                    }
                    EditorGUILayout.EndHorizontal();

                    if (entry.targetTags != null)
                    {
                        for (int j = 0; j < entry.targetTags.Length; j++)
                        {
                            EditorGUILayout.BeginHorizontal();
                            if (targetTagOptions != null && targetTagOptions.Length > 0)
                            {
                                int tIdx = Array.IndexOf(targetTagOptions, entry.targetTags[j]);
                                if (tIdx == -1)
                                {
                                    var oldC2 = GUI.color; GUI.color = Color.red;
                                    entry.targetTags[j] = EditorGUILayout.TextField($"[已丢失] {entry.targetTags[j]}");
                                    GUI.color = oldC2;
                                }
                                else
                                {
                                    int newTIdx = EditorGUILayout.Popup(tIdx, targetTagOptions);
                                    entry.targetTags[j] = targetTagOptions[newTIdx];
                                }
                            }
                            else
                            {
                                entry.targetTags[j] = EditorGUILayout.TextField(entry.targetTags[j]);
                            }
                            if (GUILayout.Button("X", GUILayout.Width(20)))
                            {
                                var tList = new List<string>(entry.targetTags);
                                tList.RemoveAt(j);
                                entry.targetTags = tList.ToArray();
                                GUI.FocusControl(null);
                                EditorGUILayout.EndHorizontal();
                                break;
                            }
                            EditorGUILayout.EndHorizontal();
                        }
                    }
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.EndVertical();
                }
                EditorGUILayout.EndVertical();
                newValue = hitEffectsArray;
            }
            else if (value is SkillAssetReference assetRef)
            {
                var attr = field.GetCustomAttribute<SkillAssetReferenceAttribute>();
                if (attr != null)
                {
                    var targetField = obj.GetType().GetField(attr.TargetFieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (targetField != null)
                    {
                        var targetObj = targetField.GetValue(obj) as Object;
                        UpdateAssetReference(assetRef, targetObj);
                    }
                }
                
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.LabelField(name);
                    EditorGUI.indentLevel++;
                    EditorGUILayout.TextField("GUID", assetRef.guid);
                    EditorGUILayout.TextField("Name", assetRef.assetName);
                    EditorGUILayout.TextField("Path", assetRef.assetPath);
                    EditorGUI.indentLevel--;
                }
                newValue = assetRef;
            }
            else if (value is List<SkillAssetReference> assetRefList)
            {
                var attr = field.GetCustomAttribute<SkillAssetReferenceAttribute>();
                if (attr != null)
                {
                    var targetField = obj.GetType().GetField(attr.TargetFieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (targetField != null)
                    {
                        var targetList = targetField.GetValue(obj) as IList;
                        if (targetList != null)
                        {
                            while (assetRefList.Count < targetList.Count) assetRefList.Add(new SkillAssetReference());
                            while (assetRefList.Count > targetList.Count) assetRefList.RemoveAt(assetRefList.Count - 1);

                            for (int i = 0; i < targetList.Count; i++)
                            {
                                UpdateAssetReference(assetRefList[i], targetList[i] as Object);
                            }
                        }
                    }
                }
                
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.LabelField(name, $"已同步资源数: {assetRefList.Count}");
                }
                newValue = assetRefList;
            }
            else if (typeof(IList).IsAssignableFrom(fieldType))
            {
                EditorGUILayout.LabelField(name, "List (Not Implemented in Base)");
            }
            else
            {
                EditorGUILayout.LabelField(name, $"Unsupported Type: {fieldType.Name}");
            }

            if (EditorGUI.EndChangeCheck())
            {
                if (UndoContext != null && UndoContext.Length > 0)
                {
                    Undo.RecordObjects(UndoContext, "Inspector Change: " + name);
                }
                field.SetValue(obj, newValue);
                OnInspectorChanged?.Invoke();
            }
        }

        private void UpdateAssetReference(SkillAssetReference assetRef, Object target)
        {
            if (target == null)
            {
                if (assetRef.guid != string.Empty)
                {
                    assetRef.Clear();
                }
                return;
            }

            string path = AssetDatabase.GetAssetPath(target);
            string guid = AssetDatabase.AssetPathToGUID(path);

            if (assetRef.guid != guid || assetRef.assetName != target.name || assetRef.assetPath != path)
            {
                assetRef.guid = guid;
                assetRef.assetName = target.name;
                assetRef.assetPath = path;
            }
        }
    }
}
