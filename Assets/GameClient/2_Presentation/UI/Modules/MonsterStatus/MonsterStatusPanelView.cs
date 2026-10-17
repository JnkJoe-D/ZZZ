using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Animations;
using TMPro;
using Game.UI;

namespace Game.UI
{
    /// <summary>
    /// 怪物状态面板 View（挂载于 MonsterStatusPanel 根预制体）。
    /// 职责：
    ///   1. 自动绑定血条、失衡条、失衡数值文本等 UGUI 视觉组件；
    ///   2. 管理 RotationConstraint（朝向玩家相机）与 PositionConstraint（跟随怪物 UIPoint）；
    ///   3. 响应 Model 数据变更，单向刷新视觉呈现。
    /// </summary>
    public class MonsterStatusPanelView : UIView
    {
        public MonsterStatusPanelModel Model { get; set; }

        // 自动生成的UI组件字段
        public Image HpFillImage { get; private set; }
        public Image DazeFillImage { get; private set; }
        public TMP_Text DazeValueTMP { get; private set; }

        // 约束组件
        public RotationConstraint RotationConstraint { get; private set; }
        public PositionConstraint PositionConstraint { get; private set; }

        public override void OnInit()
        {
            base.OnInit();
            BindUIComponents();
            InitializeMaterials();
        }

        private void BindUIComponents()
        {
            // 自动绑定UI组件
            HpFillImage = transform.Find("View/HpFillImage")?.GetComponent<Image>();
            DazeFillImage = transform.Find("View/DazeFillImage")?.GetComponent<Image>();
            DazeValueTMP = transform.Find("View/DazeValueTMP")?.GetComponent<TMP_Text>();

            // 绑定约束组件
            RotationConstraint = GetComponent<RotationConstraint>();
            PositionConstraint = GetComponent<PositionConstraint>();

            // 强制确认为水平 Filled 模式（防止预制体误设为 Simple 导致无法裁剪）
            if (HpFillImage != null)
            {
                HpFillImage.type = Image.Type.Filled;
                HpFillMethodEnsure(HpFillImage);
            }
            if (DazeFillImage != null)
            {
                DazeFillImage.type = Image.Type.Filled;
                HpFillMethodEnsure(DazeFillImage);
            }
        }

        private static void HpFillMethodEnsure(Image img)
        {
            img.fillMethod = Image.FillMethod.Horizontal;
            img.fillOrigin = (int)Image.OriginHorizontal.Left;
        }

        private void InitializeMaterials()
        {
            // 实例化独立的材质副本，避免多怪物实例同屏时修改材质属性互相干扰污染
            CloneMaterial(HpFillImage);
            CloneMaterial(DazeFillImage);
        }

        private void CloneMaterial(Image img)
        {
            if (img != null && img.material != null && img.material != Canvas.GetDefaultCanvasMaterial())
            {
                img.material = new Material(img.material);
            }
        }

        private void OnEnable()
        {
            if (RotationConstraint != null && RotationConstraint.sourceCount > 0)
            {
                RotationConstraint.constraintActive = true;
            }
            if (PositionConstraint != null && PositionConstraint.sourceCount > 0)
            {
                PositionConstraint.constraintActive = true;
            }
        }

        private void OnDisable()
        {
            if (RotationConstraint != null)
            {
                RotationConstraint.constraintActive = false;
            }
            if (PositionConstraint != null)
            {
                PositionConstraint.constraintActive = false;
            }
        }

        // ================= 约束装配接口 =================

        /// <summary>
        /// 装配相机朝向与怪物挂点跟随约束
        /// </summary>
        public void BindConstraints(Transform cameraTransform, Transform uiPointTransform)
        {
            if (RotationConstraint != null && cameraTransform != null)
            {
                while (RotationConstraint.sourceCount > 0)
                {
                    RotationConstraint.RemoveSource(0);
                }
                RotationConstraint.AddSource(new ConstraintSource
                {
                    sourceTransform = cameraTransform,
                    weight = 1.0f
                });
                RotationConstraint.constraintActive = true;
            }

            if (PositionConstraint != null && uiPointTransform != null)
            {
                while (PositionConstraint.sourceCount > 0)
                {
                    PositionConstraint.RemoveSource(0);
                }
                PositionConstraint.AddSource(new ConstraintSource
                {
                    sourceTransform = uiPointTransform,
                    weight = 1.0f
                });
                PositionConstraint.constraintActive = true;
            }
        }

        /// <summary>
        /// 清理约束引用，防止回收入池时残留空引用
        /// </summary>
        public void ClearConstraints()
        {
            if (RotationConstraint != null)
            {
                RotationConstraint.constraintActive = false;
                while (RotationConstraint.sourceCount > 0)
                {
                    RotationConstraint.RemoveSource(0);
                }
            }

            if (PositionConstraint != null)
            {
                PositionConstraint.constraintActive = false;
                while (PositionConstraint.sourceCount > 0)
                {
                    PositionConstraint.RemoveSource(0);
                }
            }
        }

        // ================= 视图渲染刷新 =================

        /// <summary>
        /// 全量刷新视图
        /// </summary>
        public void Refresh()
        {
            if (Model == null) return;

            UpdateHpFill(Model.HpPercent);
            UpdateDazeFill(Model.DazePercent);
            UpdateDazeText(Model.DazeText);
        }

        private void UpdateHpFill(float percent)
        {
            if (HpFillImage == null) return;

            HpFillImage.fillAmount = percent;
            if (HpFillImage.material != null && HpFillImage.material.HasProperty("_FillAmount"))
            {
                HpFillImage.material.SetFloat("_FillAmount", percent);
            }
        }

        private void UpdateDazeFill(float percent)
        {
            if (DazeFillImage == null) return;

            DazeFillImage.fillAmount = percent;
            if (DazeFillImage.material != null && DazeFillImage.material.HasProperty("_FillAmount"))
            {
                DazeFillImage.material.SetFloat("_FillAmount", percent);
            }
        }

        private void UpdateDazeText(string text)
        {
            if (DazeValueTMP != null)
            {
                DazeValueTMP.text = text;
            }
        }

        public override void OnRemove()
        {
            ClearConstraints();
            base.OnRemove();
        }
    }
}
