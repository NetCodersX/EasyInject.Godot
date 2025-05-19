using System;
using System.Collections.Generic;

/// <summary>
/// 通用对象池，用于缓存和重用事件对象，减少垃圾收集
/// </summary>
public class ObjectPool<T> where T : class, new()
{
	private readonly Queue<T> _pool = new Queue<T>();
	private readonly Action<T> _resetAction;
	private readonly int _maxSize;

	/// <summary>
	/// 创建一个新的对象池
	/// </summary>
	/// <param name="resetAction">重置对象状态的操作</param>
	/// <param name="initialSize">初始池大小</param>
	/// <param name="maxSize">池的最大大小，超过此值的对象将被丢弃</param>
	public ObjectPool(Action<T> resetAction = null, int initialSize = 0, int maxSize = 100)
	{
		_resetAction = resetAction;
		_maxSize = maxSize;

		// 预创建对象
		for (int i = 0; i < initialSize; i++)
		{
			_pool.Enqueue(new T());
		}
	}

	/// <summary>
	/// 从池中获取一个对象，如果池为空则创建新对象
	/// </summary>
	public T Get()
	{
		T item = _pool.Count > 0 ? _pool.Dequeue() : new T();
		return item;
	}

	/// <summary>
	/// 将对象返回到池中
	/// </summary>
	public void Release(T item)
	{
		if (item == null) return;

		// 如果提供了重置操作，则重置对象状态
		_resetAction?.Invoke(item);

		// 如果池未满，则将对象放回池中
		if (_pool.Count < _maxSize)
		{
			_pool.Enqueue(item);
		}
	}

	/// <summary>
	/// 清空对象池
	/// </summary>
	public void Clear()
	{
		_pool.Clear();
	}

	/// <summary>
	/// 获取池中当前对象数量
	/// </summary>
	public int Count => _pool.Count;
}