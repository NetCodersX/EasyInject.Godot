using Godot;
using System;

/// <summary>
/// 通用单例基类---不需要继承节点或者不在场景中实例化时使用
/// </summary>
/// <typeparam name="T">需要实现单例的具体类型</typeparam>
public class Singleton<T> where T : class, new()
{
	// Lazy<T>提供线程安全的延迟初始化
	// 只有在第一次访问Instance属性时才会创建实例
	private static readonly Lazy<T> _instance = new Lazy<T>(() => new T());

	/// <summary>
	/// 获取单例实例
	/// </summary>
	public static T Instance => _instance.Value;

	/// <summary>
	/// 保护构造函数，防止外部实例化
	/// </summary>
	protected Singleton() { }
}

/// <summary>
/// 所有Godot节点的统一单例基类
/// </summary>
/// <typeparam name="T">继承此类的具体类型</typeparam>
/// <typeparam name="U">节点的类型</typeparam>
public partial class NodeSingleton<T, U> : Node where T : U where U : Node
{
    /// <summary>
    /// 单例实例
    /// </summary>
    private static T _instance;

    /// <summary>
    /// 获取单例实例
    /// 如果实例不存在，将返回null
    /// </summary>
    public static T Instance => _instance;

    /// <summary>
    /// 当节点进入场景树时调用
    /// 用于初始化单例实例或清理重复实例
    /// </summary>
    public override void _EnterTree()
    {
        // 如果单例实例不存在，将当前实例设置为单例
        if (_instance == null)
        {
            _instance = this as T;
        }
        // 如果已存在单例实例，销毁当前重复的实例
        else
        {
            GD.PrintErr($"检测到类型为 {typeof(T).Name} 的重复单例。正在删除重复项。");
            QueueFree();
        }
    }

    /// <summary>
    /// 当节点退出场景树时调用
    /// 用于清理单例实例引用
    /// </summary>
    public override void _ExitTree()
    {
        // 只有当前实例是单例实例时才清空引用
        if (_instance == this)
        {
            _instance = null;
        }
    }
}