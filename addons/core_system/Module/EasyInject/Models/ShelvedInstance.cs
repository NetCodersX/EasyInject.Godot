namespace EasyInject.Models
{
	/// <summary>
	/// 用于记录还未完成依赖注入的 Node 实例及其名称。
	/// </summary>
	public class ShelvedInstance
	{
		/// <summary>
		/// Node 实例对象
		/// </summary>
		public readonly object Instance;
		/// <summary>
		/// Node 名称
		/// </summary>
		public readonly string NodeName;
		/// <summary>
		/// 
		/// </summary>
		/// <param name="nodeName"></param>
		/// <param name="instance"></param>
		public ShelvedInstance(string nodeName, object instance)
		{
			NodeName = nodeName;
			Instance = instance;
		}

		/// <summary>
		/// 重写 Equals，实现唯一性比较
		/// </summary>
		public override bool Equals(object obj)
		{
			if (obj is ShelvedInstance shelvedInstance)
			{
				return Instance == shelvedInstance.Instance && NodeName == shelvedInstance.NodeName;
			}
			return false;
		}
		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode()
		{
			return Instance.GetHashCode() ^ NodeName.GetHashCode();
		}
	}
}