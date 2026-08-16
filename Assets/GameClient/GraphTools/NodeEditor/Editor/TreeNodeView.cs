using System;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

public class TreeNodeView : UnityEditor.Experimental.GraphView.Node
{
    public Game.Logic.AI.BehaviorTree.Node node;
    public Port input;
    public Port output;
    public Action<TreeNodeView> onNodeSelected;
    public TreeNodeView(Game.Logic.AI.BehaviorTree.Node node)
    {
        this.node = node;
        title = node.name;
        viewDataKey = node.guid;
        style.left = node.position.x;
        style.top = node.position.y;
        CreateInputPort();
        CreateOutputPort();
        SetupPortLayout();
    }

    private void SetupPortLayout()
    {
        // 设置交叉轴（水平方向）居中，因为默认 flexDirection 是 Column
        inputContainer.style.alignItems = UnityEngine.UIElements.Align.Center;
        mainContainer.Insert(0, inputContainer);

        outputContainer.style.alignItems = UnityEngine.UIElements.Align.Center;
        mainContainer.Add(outputContainer);

        // --- 核心：配置背景色与圆角 ---
        if (node != null)
        {
            var type = node.GetType();
            var attributes = type.GetCustomAttributes(typeof(Game.Logic.AI.BehaviorTree.NodeColorAttribute), false);
            if (attributes.Length > 0)
            {
                var colorAttr = attributes[0] as Game.Logic.AI.BehaviorTree.NodeColorAttribute;
                if (UnityEngine.ColorUtility.TryParseHtmlString(colorAttr.HexColor, out UnityEngine.Color color))
                {
                    inputContainer.style.backgroundColor = color;
                }
            }
        }
        // 切出顶部圆角防溢出
        inputContainer.style.borderTopLeftRadius = 8;
        inputContainer.style.borderTopRightRadius = 8;

        // 输出区域设为与名字区相近的深灰色
        outputContainer.style.backgroundColor = new UnityEngine.Color(63f / 255f, 63f / 255f, 63f / 255f, 0.8f);
        // 切出底部圆角防溢出
        outputContainer.style.borderBottomLeftRadius = 8;
        outputContainer.style.borderBottomRightRadius = 8;
        // ------------------------------

        var topContainer = this.Q<UnityEngine.UIElements.VisualElement>("top");
        if (topContainer != null) 
        {
            topContainer.style.display = UnityEngine.UIElements.DisplayStyle.None;
        }
        // 隐藏不需要的底层元素，确保纯粹居中
        CleanPort(input);
        CleanPort(output);

        // 彻底移除标题右侧的按钮容器，防止其占用空间导致文字偏右
        var titleButtonContainer = this.Q<UnityEngine.UIElements.VisualElement>("title-button-container");
        if (titleButtonContainer != null)
        {
            titleButtonContainer.style.display = UnityEngine.UIElements.DisplayStyle.None;
        }

        // 强行固定节点长宽
        this.style.width = 140;
        this.style.height = 80;

        // 设置整个title容器水平居中，并清除内部可能存在的左右边距
        var titleContainer = this.Q<UnityEngine.UIElements.VisualElement>("title");
        if (titleContainer != null)
        {
            titleContainer.style.justifyContent = UnityEngine.UIElements.Justify.Center;
            titleContainer.style.paddingLeft = 0;
            titleContainer.style.paddingRight = 0;
            titleContainer.style.marginLeft = 0;
            titleContainer.style.marginRight = 0;
        }

        // 动态调整标题字体大小（最大限制为14）
        var titleLabel = this.Q<UnityEngine.UIElements.Label>("title-label");
        if (titleLabel != null)
        {
            // 给标题文本加点限制，居中显示，并清除所有多余的边距
            titleLabel.style.unityTextAlign = UnityEngine.TextAnchor.MiddleCenter;
            titleLabel.style.flexGrow = 1;
            titleLabel.style.marginLeft = 0;
            titleLabel.style.marginRight = 0;
            titleLabel.style.paddingLeft = 0;
            titleLabel.style.paddingRight = 0;
            
            // 简单估算：120px的可用宽度 / (字符数 * 0.6 的估算字母比例)
            int maxFontSize = 14;
            int minFontSize = 8;
            int charCount = Mathf.Max(1, title.Length);
            int optimalSize = Mathf.FloorToInt(120f / (charCount * 0.55f));
            
            titleLabel.style.fontSize = Mathf.Clamp(optimalSize, minFontSize, maxFontSize);
            // 如果实在超长，结尾显示省略号
            titleLabel.style.textOverflow = UnityEngine.UIElements.TextOverflow.Ellipsis;
            titleLabel.style.overflow = UnityEngine.UIElements.Overflow.Hidden;
        }
    }

    private void CleanPort(Port port)
    {
        if (port == null) return;
        
        // 隐藏导致偏移的空文本标签
        var label = port.Q<UnityEngine.UIElements.Label>("type");
        if (label != null)
        {
            label.style.display = UnityEngine.UIElements.DisplayStyle.None;
            label.style.marginLeft = 0;
            label.style.marginRight = 0;
        }

        // 清除连接点自带的额外边距
        var connector = port.Q("connector");
        if (connector != null)
        {
            connector.style.marginLeft = 0;
            connector.style.marginRight = 0;
        }
    }
    public void CreateInputPort()
    {
        if(node is Game.Logic.AI.BehaviorTree.Root)
        {
            input = null;
            return;
        }
        input = InstantiatePort(Orientation.Vertical, Direction.Input, Port.Capacity.Single, typeof(float));
        if(input != null)
        {
            input.portName = "";
            inputContainer.Add(input);
        }
    }
    public void CreateOutputPort()
    {
        if(node is Game.Logic.AI.BehaviorTree.LeafNode)
        {
            output = null;
            return;
        }
        var capacity = (node is Game.Logic.AI.BehaviorTree.DecoratorNode||
        node is Game.Logic.AI.BehaviorTree.Root) ? Port.Capacity.Single : Port.Capacity.Multi;
        output = InstantiatePort(Orientation.Vertical, Direction.Output, capacity, typeof(float));
        if(output != null)
        {
            output.portName = "";
            outputContainer.Add(output);
        }
    }
    public override void OnSelected()
    {
        base.OnSelected();
        if(onNodeSelected == null)return;
        onNodeSelected?.Invoke(this);
    }

    public override void SetPosition(Rect newPos)
    {
        base.SetPosition(newPos);
        node.position.x = newPos.xMin;
        node.position.y = newPos.yMin;
    }

    public void UpdateState()
    {
        if (!Application.isPlaying) return;

        RemoveFromClassList("running");
        
        if (node != null && node.CurrentState == Game.Logic.AI.BehaviorTree.NodeState.Active)
        {
            AddToClassList("running");
        }
    }
}
