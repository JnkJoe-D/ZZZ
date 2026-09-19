using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.Architecture
{
    public class ArchitectureNamingRulesTest
    {
        [Test]
        public void Verify_ComponentSuffix_MustInheritMonoBehaviour()
        {
            var assembly = typeof(Game.GamePlay.MovementComponent).Assembly;
            foreach (var type in assembly.GetTypes())
            {
                if (type.Name.EndsWith("Component") && !type.IsInterface)
                {
                    Assert.IsTrue(typeof(MonoBehaviour).IsAssignableFrom(type),
                        $"架构违规：类 [{type.FullName}] 以 Component 结尾，但未继承 MonoBehaviour！");
                }
            }
        }

        [Test]
        public void Verify_ModuleSuffix_MustNotInheritMonoBehaviour()
        {
            var assembly = typeof(Game.GamePlay.MovementComponent).Assembly;
            foreach (var type in assembly.GetTypes())
            {
                // 排除标注为 Obsolete 的过渡别名类
                if (type.GetCustomAttribute<ObsoleteAttribute>() != null) continue;

                if (type.Name.EndsWith("Module") && !type.IsInterface)
                {
                    Assert.IsFalse(typeof(MonoBehaviour).IsAssignableFrom(type),
                        $"架构违规：类 [{type.FullName}] 以 Module 结尾，却继承了 MonoBehaviour！Mono 组件必须使用 *Component 后缀！");
                }
            }
        }

        [Test]
        public void Verify_ControllerSuffix_MustNotInheritMonoBehaviour()
        {
            var assembly = typeof(Game.GamePlay.MovementComponent).Assembly;
            foreach (var type in assembly.GetTypes())
            {
                if (type.Name.EndsWith("Controller") && !type.Name.Contains("Camera") && !type.IsInterface)
                {
                    Assert.IsFalse(typeof(MonoBehaviour).IsAssignableFrom(type),
                        $"架构违规：控制器 [{type.FullName}] 必须是纯 C# 对象，严禁继承 MonoBehaviour！");
                }
            }
        }
    }
}
