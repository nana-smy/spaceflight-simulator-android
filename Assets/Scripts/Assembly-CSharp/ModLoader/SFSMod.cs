using System.Reflection;

namespace ModLoader
{
	// 兼容旧版 ModLoader.SFSMod API。
	// 部分旧 mod 继承 SFSMod 并重写 load()/unload()，
	// 但当前 ModLoader 只提供 Mod 基类。这里补一个桥接类，让旧 mod 能正常加载。
	public class SFSMod : Mod
	{
		public virtual void load() { }
		public virtual void unload() { }

		public override void Early_Load()
		{
			load();
		}

		public override string ModNameID => GetType().Assembly.GetName().Name;
		public override string DisplayName => GetType().Assembly.GetName().Name;
		public override string Author => "";
		public override string MinimumGameVersionNecessary => "1.5.10.2";
		public override string ModVersion => "1.0";
		public override string Description => "";
	}
}
