namespace EasyInject.Models
{
	/// <summary>
	/// 用于记录还未完成依赖注入的 Bean 实例及其名称。
	/// </summary>
	public class ShelvedInstance
	{
		/// <summary>
		/// Bean 实例对象
		/// </summary>
		public readonly object Instance;
		/// <summary>
		/// Bean 名称
		/// </summary>
		public readonly string BeanName;
		/// <summary>
		/// 
		/// </summary>
		/// <param name="beanName"></param>
		/// <param name="instance"></param>
		public ShelvedInstance(string beanName, object instance)
		{
			BeanName = beanName;
			Instance = instance;
		}

		/// <summary>
		/// 重写 Equals，实现唯一性比较
		/// </summary>
		public override bool Equals(object obj)
		{
			if (obj is ShelvedInstance shelvedInstance)
			{
				return Instance == shelvedInstance.Instance && BeanName == shelvedInstance.BeanName;
			}
			return false;
		}
		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode()
		{
			return Instance.GetHashCode() ^ BeanName.GetHashCode();
		}
	}
}