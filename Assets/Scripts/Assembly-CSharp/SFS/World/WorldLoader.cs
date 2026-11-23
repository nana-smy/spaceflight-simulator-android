using System;
using UnityEngine;

namespace SFS.World
{
	public class WorldLoader : MonoBehaviour
	{
		public WorldLocation location;

		public GameObject holder;

		public double loadDistance;

		public bool Loaded { get; private set; }

		public event Action<bool, bool> onLoadedChange_Before;

		public event Action<bool, bool> onLoadedChange_After;

		private void Start()
		{
			WorldView main = WorldView.main;
			main.onViewLocationChange_Before = (Action<Location, Location>)Delegate.Combine(main.onViewLocationChange_Before, new Action<Location, Location>(OnLocationChange_Before));
			WorldView main2 = WorldView.main;
			main2.onViewLocationChange_After = (Action<Location, Location>)Delegate.Combine(main2.onViewLocationChange_After, new Action<Location, Location>(OnLocationChange_After));
			location.planet.OnChange += new Action(OnLocationDataChange);
			location.position.OnChange += new Action(OnLocationDataChange);
			// 启动时按当前视点立即计算一次加载状态（等价于订阅后立刻触发一次位置更新），
			// 避免 Start 顺序导致 holder 在位置已设置后才订阅、永远停在关闭状态。
			UpdateLoading(WorldView.main.ViewLocation);
		}

		private void OnDestroy()
		{
			WorldView main = WorldView.main;
			main.onViewLocationChange_Before = (Action<Location, Location>)Delegate.Remove(main.onViewLocationChange_Before, new Action<Location, Location>(OnLocationChange_Before));
			WorldView main2 = WorldView.main;
			main2.onViewLocationChange_After = (Action<Location, Location>)Delegate.Remove(main2.onViewLocationChange_After, new Action<Location, Location>(OnLocationChange_After));
			if (location != null)
			{
				location.planet.OnChange -= new Action(OnLocationDataChange);
				location.position.OnChange -= new Action(OnLocationDataChange);
			}
		}

		private void OnLocationDataChange()
		{
			UpdateLoading(WorldView.main.ViewLocation);
		}

		private void OnLocationChange_Before(Location oldValue, Location newValue)
		{
			if (Loaded)
			{
				UpdateLoading(newValue);
			}
		}

		private void OnLocationChange_After(Location oldValue, Location newValue)
		{
			if (!Loaded)
			{
				UpdateLoading(newValue);
			}
		}

		private void UpdateLoading(Location newValue)
		{
			bool flag = location.Value != null && location.planet.Value == newValue.planet && (location.Value.position - newValue.position).Mag_LessThan(loadDistance * (Loaded ? 1.2 : 0.8));
			if (flag != Loaded)
			{
				SetLoaded(flag);
			}
		}

		private void SetLoaded(bool newValue)
		{
			bool arg = newValue;
			this.onLoadedChange_Before?.Invoke(arg, newValue);
			Loaded = newValue;
			holder.SetActive(newValue);
			this.onLoadedChange_After?.Invoke(arg, newValue);
		}
	}
}
