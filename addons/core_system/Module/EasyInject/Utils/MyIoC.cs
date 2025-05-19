using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EasyInject.Attributes;
using EasyInject.Behaviours;
using EasyInject.Models;
using Godot;

namespace EasyInject.Utils
{
    /// <summary>
    /// IoC容器实现类，用于管理节点（Node）和普通对象的依赖注入与生命周期
    /// </summary>
    public partial class MyIoC : IIoC
    {
        /// <summary>
        /// IoC容器，存储所有已注册的Bean对象，key为BeanInfo（包含名字、类型、场景），value为实例
        /// </summary>
        private readonly Dictionary<BeanInfo, object> _beans = new();

        /// <summary>
        /// 尚未完成依赖注入的实例，key为ShelvedInstance（包含名字和实例），value为已注入的字段名集合
        /// </summary>
        private readonly Dictionary<ShelvedInstance, HashSet<string>> _shelvedInstances = new();

        /// <summary>
        /// 构造函数，完成自身注册和普通对象Bean的初始化
        /// </summary>
        public MyIoC()
        {
            var type = GetType();
            // 注册自身为Bean，使其他对象可以直接注入IoC容器
            RegisterTypeAndParentsAndInterfaces(type.Name, this, type);
            // 初始化所有节点创建
            InitCreateNodes();
            // 初始化所有带ComponentAttribute的普通Bean（非Node节点）
            InitNormalBean();
        }

        /// <summary>
        /// 创建一个Node实例作为Bean并注册（不指定父节点）
        /// </summary>
        /// <typeparam name="T">节点类型</typeparam>
        /// <param name="original">原始节点作为模板</param>
        /// <param name="beanName">Bean的名称</param>
        /// <returns>创建的节点实例</returns>
        public T CreateNodeAsBean<T>(T original, string beanName) where T : Node
        {
            // 使用Godot的Duplicate方法克隆一个新节点
            var node = (T)original.Duplicate();
            // 检查并注册为新Bean
            return CheckNewNode<T>(node, beanName);
        }

        /// <summary>
        /// 创建一个Node实例作为Bean并注册，并指定父节点
        /// </summary>
        /// <typeparam name="T">节点类型</typeparam>
        /// <param name="original">原始节点作为模板</param>
        /// <param name="beanName">Bean的名称</param>
        /// <param name="parent">父节点，新节点会被添加为其子节点</param>
        /// <returns>创建的节点实例</returns>
        public T CreateNodeAsBean<T>(T original, string beanName, Node parent) where T : Node
        {
            // 克隆一个新节点
            var node = (T)original.Duplicate();
            // 添加到父节点
            parent.AddChild(node);
            // 检查并注册为Bean
            return CheckNewNode<T>(node, beanName);
        }

        /// <summary>
        /// 创建一个Node实例作为Bean并注册，父节点为null时可选择是否放入场景根节点
        /// </summary>
        /// <typeparam name="T">节点类型</typeparam>
        /// <param name="original">原始节点作为模板</param>
        /// <param name="beanName">Bean的名称</param>
        /// <param name="parent">父节点，可以为null</param>
        /// <param name="inWorld">当parent为null时，是否将节点添加到场景根节点</param>
        /// <returns>创建的节点实例</returns>
        public T CreateNodeAsBean<T>(T original, string beanName, Node parent, bool inWorld) where T : Node
        {
            // 克隆一个新节点
            var node = (T)original.Duplicate();
            if (parent != null)
                // 如果指定了父节点，将新节点添加为其子节点
                parent.AddChild(node);
            else if (inWorld)
                // 如果未指定父节点但要求放入场景树，添加到场景根节点
                GetRoot().Root.AddChild(node);
            // 检查并注册为Bean
            return CheckNewNode<T>(node, beanName);
        }

        /// <summary>
        /// 创建一个Node3D实例作为Bean（指定位置和旋转，不指定父节点）
        /// </summary>
        /// <typeparam name="T">3D节点类型</typeparam>
        /// <param name="original">原始节点作为模板</param>
        /// <param name="position">节点在3D空间中的位置</param>
        /// <param name="rotation">节点在3D空间中的旋转（四元数）</param>
        /// <param name="beanName">Bean的名称</param>
        /// <returns>创建的3D节点实例</returns>
        public T CreateNodeAsBean<T>(T original, Vector3 position, Quaternion rotation, string beanName) where T : Node3D
        {
            // 克隆一个新节点
            var node = (T)original.Duplicate();
            // 设置空间位置和旋转
            node.Position = position;
            node.Rotation = rotation.GetEuler();
            // 检查并注册为Bean
            return CheckNewNode<T>(node, beanName);
        }

        /// <summary>
        /// 创建一个Node3D实例作为Bean（指定位置、旋转、父节点）
        /// </summary>
        /// <typeparam name="T">3D节点类型</typeparam>
        /// <param name="original">原始节点作为模板</param>
        /// <param name="beanName">Bean的名称</param>
        /// <param name="position">节点在3D空间中的位置</param>
        /// <param name="rotation">节点在3D空间中的旋转（四元数）</param>
        /// <param name="parent">父节点，新节点会被添加为其子节点</param>
        /// <returns>创建的3D节点实例</returns>
        public T CreateNodeAsBean<T>(T original, string beanName, Vector3 position, Quaternion rotation, Node parent = null) where T : Node3D
        {
            // 克隆一个新节点
            var node = (T)original.Duplicate();
            // 设置空间位置和旋转
            node.Position = position;
            node.Rotation = rotation.GetEuler();
            // 添加到父节点
            parent.AddChild(node);
            // 检查并注册为Bean
            return CheckNewNode<T>(node, beanName);
        }

        /// <summary>
        /// 检查新节点是否符合注册条件，并完成Bean注册
        /// </summary>
        /// <typeparam name="T">节点类型</typeparam>
        /// <param name="node">要检查和注册的节点</param>
        /// <param name="beanName">Bean名称</param>
        /// <returns>注册后的节点实例</returns>
        private T CheckNewNode<T>(T node, string beanName) where T : Node
        {
            // 获取当前场景名，若无法获取则为"root"
            var scene = GetRoot().CurrentScene?.Name ?? "root";

            // 检查是否已存在同名同类型Bean，存在则抛出异常
            if (_beans.ContainsKey(new BeanInfo(beanName, typeof(T))))
                throw new Exception($"Bean {beanName} 已存在");

            // 注册为Bean，并完成依赖注入
            AddBean(beanName, node, scene);
            return node;
        }

        /// <summary>
        /// 删除一个Node Bean，可延迟删除
        /// </summary>
        /// <typeparam name="T">节点类型</typeparam>
        /// <param name="bean">要删除的节点Bean</param>
        /// <param name="beanName">Bean名称</param>
        /// <param name="deleteNode">是否删除节点本身</param>
        /// <param name="t">延迟秒数</param>
        /// <returns>是否成功删除</returns>
        public bool DeleteNodeBean<T>(T bean, string beanName = "", bool deleteNode = false, float t = 0.0f) where T : Node
        {
            // 如果传入的bean为null，返回失败
            if (bean == null) return false;
            // 创建BeanInfo用于在容器中查找
            var beanInfo = new BeanInfo(beanName, bean.GetType());
            // 如果容器中不存在该Bean，返回失败
            if (!_beans.ContainsKey(beanInfo)) return false;
            // 从容器中移除该Bean
            _beans.Remove(beanInfo);

            if (!deleteNode)
            {
                // 只删除Bean引用，不移除节点
                // 根据延迟时间决定使用CallDeferred延迟执行还是直接QueueFree
                if (t > 0)
                    bean.CallDeferred("queue_free");
                else
                    bean.QueueFree();
            }
            else
            {
                // 删除Bean引用并移除节点
                if (t > 0)
                    bean.CallDeferred("queue_free");
                else
                    bean.QueueFree();
            }
            return true;
        }

        /// <summary>
        /// 立即删除一个Node Bean
        /// </summary>
        /// <typeparam name="T">节点类型</typeparam>
        /// <param name="bean">要删除的节点Bean</param>
        /// <param name="beanName">Bean名称</param>
        /// <param name="deleteNode">是否删除节点本身</param>
        /// <returns>是否成功删除</returns>
        public bool DeleteNodeBeanImmediate<T>(T bean, string beanName = "", bool deleteNode = false) where T : Node
        {
            // 如果传入的bean为null，返回失败
            if (bean == null) return false;
            // 创建BeanInfo用于在容器中查找
            var beanInfo = new BeanInfo(beanName, bean.GetType());
            // 如果容器中不存在该Bean，返回失败
            if (!_beans.ContainsKey(beanInfo)) return false;
            // 从容器中移除该Bean
            _beans.Remove(beanInfo);

            // 立即销毁节点（不等待下一帧）
            bean.QueueFree();
            return true;
        }

        /// <summary>
        /// 获取已注册的Bean
        /// </summary>
        /// <typeparam name="T">Bean类型</typeparam>
        /// <param name="name">Bean名称，默认空字符串</param>
        /// <returns>找到的Bean实例，未找到返回null</returns>
        public T GetBean<T>(string name = "") where T : class
        {
            // 创建BeanInfo用于在容器中查找
            var beanInfo = new BeanInfo(name, typeof(T));
            // 尝试获取Bean，成功则返回，失败返回null
            if (_beans.TryGetValue(beanInfo, out var value))
                return (T)value;
            return null;
        }

        /// <summary>
        /// 清空指定场景下的Bean
        /// </summary>
        /// <param name="scene">要清理的场景名称，默认当前场景</param>
        /// <param name="clearAcrossScenesBeans">是否同时清理跨场景Bean</param>
        public void ClearBeans(string scene = null, bool clearAcrossScenesBeans = false)
        {
            // 获取当前场景名，如果未指定则使用当前场景
            scene ??= GetRoot().CurrentScene?.Name ?? "root";
            // 遍历所有BeanInfo，移除属于本场景的Bean
            foreach (var (beanInfo, value) in _beans.Where(bean => bean.Key.Scenes.Contains(scene)).ToList())
            {
                _beans.Remove(beanInfo);
                // 如果是Node类型，销毁节点
                if (value is Node node)
                    node.QueueFree();
            }

            // 清空未完成注入的实例字典
            _shelvedInstances.Clear();
        }

        /// <summary>
        /// 清空指定/全部场景下的Bean（重载）
        /// </summary>
        /// <param name="clearAcrossScenesBeans">是否同时清理跨场景Bean</param>
        public void ClearBeans(bool clearAcrossScenesBeans)
        {
            // 调用主清理方法，使用默认场景参数
            ClearBeans(null, clearAcrossScenesBeans);
        }

        /// <summary>
        /// 初始化场景中的Node Bean和所有带有GameObjectBeanAttribute的节点
        /// </summary>
        public void Init()
        {
            // 获取当前场景名，如果无法获取则默认为"root"
            var scene = GetRoot().CurrentScene?.Name ?? "root";
            // 查找所有继承自BeanObject的节点
            var beanObjects = FindNodesOfType<BeanObject>();

            // 遍历所有BeanObject节点
            foreach (var beanObject in beanObjects)
            {
                // 检查IoC容器中是否已存在同名同类型的Bean，存在则跳过
                if (_beans.ContainsKey(new BeanInfo(beanObject.Name, typeof(BeanObject))))
                {
                    continue;
                }

                // 将该节点注册为Bean，不进行依赖注入（startInject=false，通常依赖托管全局Bean）
                AddBean(beanObject.Name, beanObject, scene, false);
            }

            // 查找所有打了GameObjectBeanAttribute的节点
            var gameObjectBeans = FindNodesWithAttribute<GameObjectBeanAttribute>();

            // 遍历每个符合条件的节点
            foreach (var gameObjectBean in gameObjectBeans)
            {
                // 通过反射获取节点类型上的GameObjectBeanAttribute实例
                var attribute = gameObjectBean.GetType().GetCustomAttribute<GameObjectBeanAttribute>();
                string name = string.Empty;

                // 根据Attribute的NameType属性决定Bean的命名方式
                switch (attribute.NameType)
                {
                    case ENameType.Custom:
                        // 如果指定为自定义名，直接取Attribute.Name
                        name = attribute.Name;
                        break;
                    case ENameType.ClassName:
                        // 如果指定为类名，用节点脚本的类型名
                        name = gameObjectBean.GetType().Name;
                        break;
                    case ENameType.GameObjectName:
                        // 如果指定为节点名，用Godot中的Node.Name
                        name = gameObjectBean.Name;
                        break;
                    case ENameType.FieldValue:
                        // 如果指定为字段值，则查找带BeanNameAttribute的字段，并取其值作为Bean名
                        var field = gameObjectBean
                            .GetType()
                            .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                            .FirstOrDefault(f => f.GetCustomAttribute<BeanNameAttribute>() != null);

                        // 如果找到合适字段，取其值，否则为空字符串
                        name = field != null ? field.GetValue(gameObjectBean)?.ToString() ?? string.Empty : string.Empty;
                        break;
                    default: break;
                }

                // 如果name为空，兜底为类型名（防止Bean无名导致注入失败）
                if (string.IsNullOrEmpty(name))
                {
                    name = gameObjectBean.GetType().Name;
                }

                // 如果name仍为空，或已存在同名同类型Bean，则跳过注册
                if (string.IsNullOrEmpty(name) || _beans.ContainsKey(new BeanInfo(name, gameObjectBean.GetType())))
                {
                    continue;
                }

                // 注册该节点为Bean，并进行依赖注入
                AddBean(name, gameObjectBean, scene);
            }
        }

        /// <summary>
        /// 初始化所有带ComponentAttribute的普通Bean（非Node节点），并完成依赖注入
        /// </summary>
        private void InitNormalBean()
        {
            // 获取所有带ComponentAttribute的类型
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => t.GetCustomAttributes(typeof(ComponentAttribute), true).Length > 0).ToList();

            // 只要还有未处理的类型，循环处理
            while (types.Count > 0)
            {
                // 遍历types列表
                for (var i = 0; i < types.Count; i++)
                {
                    var type = types[i];
                    var constructors = type.GetConstructors();
                    object instance = null;

                    // 尝试所有构造函数（优先注入参数）
                    foreach (var constructor in constructors)
                    {
                        var parameters = constructor.GetParameters();
                        var parameterInstances = new object[parameters.Length];

                        // 遍历构造参数
                        for (var j = 0; j < parameters.Length; j++)
                        {
                            var parameterType = parameters[j].ParameterType;
                            // 查找构造参数上的AutowiredAttribute
                            var name = parameters[j].GetCustomAttribute<AutowiredAttribute>()?.Name ?? parameterType.Name;
                            var beanInfo = new BeanInfo(name, parameterType);

                            // 从容器中查找对应类型和名字的Bean，未找到则跳出
                            if (_beans.TryGetValue(beanInfo, out var parameterInstance))
                            {
                                parameterInstances[j] = parameterInstance;
                            }
                            else
                            {
                                break;
                            }
                        }

                        // 如果有任何参数未注入，跳过该构造函数
                        if (parameterInstances.Contains(null)) continue;

                        // 所有参数都注入成功，调用构造实例化
                        instance = constructor.Invoke(parameterInstances);
                        break;
                    }

                    // 如果没有参数构造器或找不到合适参数，则用无参构造
                    if (instance == null && type.GetConstructor(Type.EmptyTypes) != null)
                        instance = Activator.CreateInstance(type);

                    // 仍然无法实例化则跳过
                    if (instance == null) continue;
                    //获取要注入的类名称
                    var attributeName = type.GetCustomAttribute<ComponentAttribute>()?.Name ?? type.Name;

                    // 注册该类型以及其父类和接口为Bean
                    RegisterTypeAndParentsAndInterfaces(attributeName, instance, type);

                    // 从types列表移除已处理类型（防止重复处理）
                    types.RemoveAt(i);
                    i--;
                }
            }

            // 遍历所有已经注册的Bean，进行字段/属性依赖注入
            foreach (var type in _beans.Keys.ToList())
            {
                var instance = _beans[type];

                // 如果有PersistAcrossScenesAttribute标记（跨场景持久Bean），则跳过注入
                if (instance.GetType().GetCustomAttribute<PersistAcrossScenesAttribute>() != null)
                {
                    continue;
                }

                // 找到所有带AutowiredAttribute的字段
                var fields = instance.GetType()
                    .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    .Where(f => f.GetCustomAttributes(typeof(AutowiredAttribute), true).Length > 0).ToList();

                // 用于记录已注入的字段名
                var injected = new HashSet<string>();

                // 遍历每个需注入的字段
                foreach (var field in fields)
                {
                    // 获取注入名
                    var name = field.GetCustomAttribute<AutowiredAttribute>()?.Name ?? field.Name;
                    var beanInfo = new BeanInfo(name, field.FieldType);

                    // 如果找到对应Bean，注入并标记已注入
                    if (_beans.TryGetValue(beanInfo, out var value))
                    {
                        field.SetValue(instance, value);
                        injected.Add(field.Name);
                    }
                    else
                    {
                        // 如果找不到依赖Bean，将当前实例和字段注册到_shelvedInstances等待后续注入
                        _shelvedInstances.TryAdd(new ShelvedInstance(type.Name, instance), injected);
                    }
                }

                // 找到所有带AutowiredAttribute的属性
                var properties = instance.GetType()
                    .GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    .Where(p => p.GetCustomAttributes(typeof(AutowiredAttribute), true).Length > 0).ToList();

                // 遍历每个需注入的属性
                foreach (var property in properties)
                {
                    var name = property.GetCustomAttribute<AutowiredAttribute>()?.Name ?? property.PropertyType.Name;
                    var beanInfo = new BeanInfo(name, property.PropertyType);

                    if (_beans.TryGetValue(beanInfo, out var value))
                    {
                        property.SetValue(instance, value);
                        injected.Add(property.Name);
                    }
                    else
                    {
                        // 如果找不到依赖Bean，将当前实例和字段注册到_shelvedInstances等待后续注入
                        _shelvedInstances.TryAdd(new ShelvedInstance(type.Name, instance), injected);
                    }
                }
            }
        }
        /// <summary>
        /// 初始化所有带CreateNodeAttribute的类和方法，创建并注册节点实例
        /// </summary>
        private void InitCreateNodes()
        {
            // 获取当前场景名
            var scene = GetRoot().CurrentScene?.Name ?? "root";

            // 查找所有带CreateNodeAttribute的类型
            var createNodeTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => t.GetCustomAttribute<CreateNodeAttribute>() != null &&
                            typeof(Node).IsAssignableFrom(t) &&
                            !t.IsAbstract &&
                            t.GetConstructor(Type.EmptyTypes) != null)
                .ToList();

            // 处理每个CreateNode类型
            foreach (var type in createNodeTypes)
            {
                // 获取特性
                var attr = type.GetCustomAttribute<CreateNodeAttribute>();
                string beanName = DetermineBeanName(attr, type);

                // 检查是否已存在同名Bean
                if (_beans.ContainsKey(new BeanInfo(beanName, type)))
                    continue;

                try
                {
                    // 创建节点实例
                    var node = (Node)Activator.CreateInstance(type);
                    node.Name = beanName;
                    // 如果需要添加到场景树
                    if (attr.AddToScene && !string.IsNullOrEmpty(attr.ParentBeanName))
                    {
                        var parentBean = GetBean<Node>(attr.ParentBeanName);
                        if (parentBean != null)
                        {
                            parentBean.AddChild(node);
                        }
                        else
                        {
                            GetRoot().Root.GetNode(CoreSystemEditorPlugin.SYSTEM_NAME).AddChild(node);
                        }
                    }

                    // 注册为Bean
                    AddBean(beanName, node, scene);
                }
                catch (Exception ex)
                {
                    GD.PrintErr($"Failed to create node instance for {type.Name}: {ex.Message}");
                }
            }

            // 查找并处理所有带CreateNodeAttribute的方法
            var createNodeMethods = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Static))
                .Where(m => m.GetCustomAttribute<CreateNodeAttribute>() != null &&
                           typeof(Node).IsAssignableFrom(m.ReturnType) &&
                           m.GetParameters().Length == 0)
                .ToList();

            // 处理每个工厂方法
            foreach (var method in createNodeMethods)
            {
                var attr = method.GetCustomAttribute<CreateNodeAttribute>();
                string beanName = DetermineBeanName(attr, method.ReturnType);

                // 检查是否已存在同名Bean
                if (_beans.ContainsKey(new BeanInfo(beanName, method.ReturnType)))
                    continue;

                try
                {
                    // 调用工厂方法创建节点
                    var node = (Node)method.Invoke(null, null);

                    // 如果需要添加到场景树
                    if (attr.AddToScene && !string.IsNullOrEmpty(attr.ParentBeanName))
                    {
                        var parentBean = GetBean<Node>(attr.ParentBeanName);
                        if (parentBean != null)
                        {
                            parentBean.AddChild(node);
                        }
                    }

                    // 注册为Bean
                    AddBean(beanName, node, scene);
                }
                catch (Exception ex)
                {
                    GD.PrintErr($"Failed to create node from factory method {method.Name}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 根据命名策略确定Bean名称
        /// </summary>
        /// <param name="attr">NodeBean特性</param>
        /// <param name="type">标记的类型</param>
        /// <returns>确定的Bean名称</returns>
        private string DetermineBeanName(CreateNodeAttribute attr, Type type)
        {
            // 如果是自定义名称且名称不为空，则使用自定义名称
            if (attr.NameType == ENameType.Custom && !string.IsNullOrEmpty(attr.Name))
            {
                return attr.Name;
            }

            // 根据命名策略确定名称
            switch (attr.NameType)
            {
                case ENameType.Custom:
                    // 如果是自定义但名称为空，回退到类名
                    return type.Name;

                case ENameType.ClassName:
                    return type.Name;

                case ENameType.GameObjectName:
                    // 对于节点，可能需要在创建后设置名称
                    return type.Name;

                case ENameType.FieldValue:
                    // 对于NodeBean，通常不会使用字段值命名
                    return type.Name;

                default:
                    return type.Name; // 默认使用类名
            }
        }
        /// <summary>
        /// 对传入实例进行依赖注入（用于Node对象）
        /// </summary>
        /// <param name="beanName">Bean名称</param>
        /// <param name="instance">要注入的实例</param>
        private void Inject(string beanName, object instance)
        {
            var type = instance.GetType();

            // 找到所有需注入的字段
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(f => f.GetCustomAttributes(typeof(AutowiredAttribute), true).Length > 0);

            // 记录已注入字段的集合
            var injected = new HashSet<string>();

            // 遍历每个需注入的字段
            foreach (var field in fields)
            {
                // 获取字段上的AutowiredAttribute的Name属性作为Bean名
                var autowiredAttr = field.GetCustomAttribute<AutowiredAttribute>();
                var name = autowiredAttr?.Name;
                // 如果name为空，则尝试根据字段类型获取实现类名称
                if (string.IsNullOrEmpty(name))
                {
                    name = field.FieldType.Name;
                }
                var beanInfo = new BeanInfo(name, field.FieldType);

                // 如果能找到对应Bean，则注入，否则加入挂起队列等待后续注入
                if (_beans.TryGetValue(beanInfo, out var value))
                {
                    field.SetValue(instance, value);
                    injected.Add(field.Name);
                }
                else
                {
                    var insKey = new ShelvedInstance(beanName, instance);
                    _shelvedInstances.TryAdd(insKey, injected);
                    break; // 遇到未注入就break，避免重复加入_shelvedInstances
                }
            }

            // 找到所有需注入的属性
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(p => p.GetCustomAttributes(typeof(AutowiredAttribute), true).Length > 0);

            // 遍历每个需注入的属性
            foreach (var property in properties)
            {
                var autowiredAttr = property.GetCustomAttribute<AutowiredAttribute>();
                var name = autowiredAttr?.Name;
                // 如果name为空，则尝试根据属性类型获取实现类名称
                if (string.IsNullOrEmpty(name))
                {
                    name = property.PropertyType.Name;
                }
                var beanInfo = new BeanInfo(name, property.PropertyType);

                if (_beans.TryGetValue(beanInfo, out var value))
                {
                    property.SetValue(instance, value);
                    injected.Add(property.Name);
                }
                else
                {
                    var insKey = new ShelvedInstance(beanName, instance);
                    _shelvedInstances.TryAdd(insKey, injected);
                    break;
                }
            }

            // 检查是否有原本挂起的实例现在能完成注入
            CheckShelvedInstances();
        }

        /// <summary>
        /// 新增一个Bean实例（一般为Node），并完成类型注册与依赖注入
        /// </summary>
        /// <typeparam name="T">Bean类型</typeparam>
        /// <param name="name">Bean名称</param>
        /// <param name="instance">Bean实例</param>
        /// <param name="scene">所属场景名</param>
        /// <param name="startInject">是否立即执行依赖注入</param>
        private void AddBean<T>(string name, T instance, string scene, bool startInject = true) where T : Node
        {
            // 注册类型及父类、接口
            RegisterTypeAndParentsAndInterfaces(name, instance, instance.GetType(), scene);

            // 是否立即执行依赖注入
            if (startInject)
            {
                Inject(name, instance);
            }
        }

        /// <summary>
        /// 检查所有未完成注入的实例，若依赖Bean已就绪则完成注入
        /// </summary>
        private void CheckShelvedInstances()
        {
            // 遍历所有待注入的实例副本（避免集合修改异常）
            foreach (var (key, injected) in _shelvedInstances.ToList())
            {
                // 获取所有需注入的字段
                var insFields = key.Instance.GetType()
                    .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    .Where(f => f.GetCustomAttributes(typeof(AutowiredAttribute), true).Length > 0).ToList();

                // 获取所有需注入的属性
                var insProperties = key.Instance.GetType()
                    .GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    .Where(p => p.GetCustomAttributes(typeof(AutowiredAttribute), true).Length > 0).ToList();

                // 计算总的需注入项数
                var count = insFields.Count + insProperties.Count;

                // 遍历所有字段
                foreach (var field in insFields)
                {
                    // 如果字段已注入则跳过
                    if (injected.Contains(field.Name))
                    {
                        count--;
                        continue;
                    }

                    // 获取字段上的注入名
                    var name = field.GetCustomAttribute<AutowiredAttribute>().Name ?? field.FieldType.Name;
                    var beanInfo = new BeanInfo(name, field.FieldType);

                    // 如果找到依赖Bean，注入并计数
                    if (!_beans.TryGetValue(beanInfo, out var value))
                    {
                        continue;
                    }

                    // 完成字段注入
                    field.SetValue(key.Instance, value);
                    injected.Add(field.Name);
                    count--;
                }

                // 遍历所有属性
                foreach (var property in insProperties)
                {
                    // 如果属性已注入则跳过
                    if (injected.Contains(property.Name))
                    {
                        count--;
                        continue;
                    }

                    // 获取属性上的注入名
                    var name = property.GetCustomAttribute<AutowiredAttribute>().Name ?? property.PropertyType.Name;
                    var beanInfo = new BeanInfo(name, property.PropertyType);

                    if (!_beans.TryGetValue(beanInfo, out var value))
                    {
                        continue;
                    }

                    // 完成属性注入
                    property.SetValue(key.Instance, value);
                    injected.Add(property.Name);
                    count--;
                }

                // 如果所有字段和属性都已注入完成，从挂起队列中移除
                if (count == 0)
                {
                    _shelvedInstances.Remove(key);
                }
            }
        }

        /// <summary>
        /// 按继承链注册类型本身、父类和所有接口
        /// </summary>
        /// <param name="name">Bean名称</param>
        /// <param name="instance">Bean实例</param>
        /// <param name="type">要注册的类型</param>
        /// <param name="scene">所属场景名</param>
        private void RegisterTypeAndParentsAndInterfaces(string name, object instance, Type type, string scene = "")
        {
            // 确保name不为null
            name ??= string.Empty;
            // 创建BeanInfo对象（包含名称、类型、场景）
            var beanInfo = new BeanInfo(name, type, scene);

            // 若已注册则抛异常
            if (!_beans.TryAdd(beanInfo, instance))
                throw new Exception($"Bean {name} already exists");

            // 递归注册父类型（不包含Godot引擎内置类型和Object类型）
            var baseType = type.BaseType;
            if (baseType != null && baseType != typeof(object) && baseType.Namespace != null &&
                !baseType.Namespace.Contains("Godot"))
            {
                RegisterTypeAndParentsAndInterfaces(name, instance, baseType, scene);
            }

            // 注册所有接口（不包含Godot内置接口）
            var interfaces = type.GetInterfaces();
            foreach (var @interface in interfaces)
            {
                // 跳过Godot命名空间下的接口
                if (@interface.Namespace == null || @interface.Namespace.Contains("Godot")) continue;
                RegisterTypeAndParentsAndInterfaces(name, instance, @interface, scene);
            }
        }

        /// <summary>
        /// 工具方法：递归查找所有类型为T的节点
        /// </summary>
        /// <typeparam name="T">要查找的节点类型</typeparam>
        /// <returns>找到的所有符合类型的节点列表</returns>
        private List<T> FindNodesOfType<T>() where T : Node
        {
            var result = new List<T>();

            // 递归遍历节点树的本地函数
            void Find(Node n)
            {
                // 如果当前节点是T类型，添加到结果列表
                if (n is T node)
                    result.Add(node);

                // 递归遍历所有子节点
                foreach (Node child in n.GetChildren())
                    Find(child);
            }

            // 从场景根节点开始递归
            Find(GetRoot().Root);
            return result;
        }

        /// <summary>
        /// 工具方法：递归查找所有带有指定Attribute的节点
        /// </summary>
        /// <typeparam name="TAttribute">要查找的特性类型</typeparam>
        /// <returns>找到的所有带有指定特性的节点列表</returns>
        private List<Node> FindNodesWithAttribute<TAttribute>() where TAttribute : Attribute
        {
            var result = new List<Node>();

            // 递归遍历节点树的本地函数
            void Find(Node n)
            {
                // 判断节点类型上是否有指定Attribute
                if (n.GetType().GetCustomAttribute<TAttribute>() != null)
                    result.Add(n);

                // 递归遍历所有子节点
                foreach (Node child in n.GetChildren())
                    Find(child);
            }

            // 从场景根节点开始递归
            Find(GetRoot().Root);
            return result;
        }

        /// <summary>
        /// 获取SceneTree实例（用于获取场景根节点、当前场景等）
        /// </summary>
        /// <returns>当前场景树</returns>
        private SceneTree GetRoot() => Engine.GetMainLoop() as SceneTree;
    }
}