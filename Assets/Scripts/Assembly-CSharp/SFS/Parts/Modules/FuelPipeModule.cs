using System;
using System.Collections.Generic;
using System.Linq;
using SFS.World;
using UnityEngine;

namespace SFS.Parts.Modules
{
	public class FuelPipeModule : MonoBehaviour
	{
		public SurfaceData surface_In;

		public SurfaceData surface_Out;

		private List<FuelPipeModule> previousPipes = new List<FuelPipeModule>();

		private ResourceModule resource_In;

		private ResourceModule resource_Out;

		public void FindNeighbours(JointGroup group)
		{
			previousPipes.Clear();
			resource_In = null;
			resource_Out = null;
			Part componentInParent = GetComponentInParent<Part>();
			foreach (PartJoint connectedJoint in group.GetConnectedJoints(componentInParent))
			{
				Part otherPart = connectedJoint.GetOtherPart(componentInParent);
				if (otherPart.HasModule<FuelPipeModule>() && SurfaceUtility.SurfacesConnect(otherPart.GetModules<FuelPipeModule>()[0].surface_Out, surface_In, out var overlap, out var center))
				{
					previousPipes.Add(otherPart.GetModules<FuelPipeModule>()[0]);
				}
				if (otherPart.HasModule<ResourceModule>())
				{
					if (SurfaceUtility.SurfacesConnect(otherPart, surface_In, out overlap, out center))
					{
						resource_In = otherPart.GetModules<ResourceModule>()[0].parent;
					}
					if (SurfaceUtility.SurfacesConnect(otherPart, surface_Out, out overlap, out center))
					{
						resource_Out = otherPart.GetModules<ResourceModule>()[0].parent;
					}
				}
			}
		}

		public (ResourceModule[], ResourceModule)? FindFlow()
		{
			if (resource_Out == null)
			{
				return null;
			}
			ResourceModule[] array = (from a in FindFromTanks()
				where a.resourceType == resource_Out.resourceType
				select a).ToArray();
			if (array.Length == 0)
			{
				return null;
			}
			return (array, resource_Out);
		}

		public List<ResourceModule> FindFlowsForEngine(FlowModule.FlowType flowType)
		{
			if (flowType == FlowModule.FlowType.Negative)
			{
				return FindFromTanks();
			}
			throw new NotImplementedException();
		}

		private List<ResourceModule> FindFromTanks()
		{
			List<FuelPipeModule> list = new List<FuelPipeModule>();
			Stack<FuelPipeModule> stack = new Stack<FuelPipeModule>();
			stack.Push(this);
			List<ResourceModule> list2 = new List<ResourceModule>();
			while (stack.Count > 0)
			{
				FuelPipeModule fuelPipeModule = stack.Pop();
				if (fuelPipeModule.resource_In != null && !list2.Contains(fuelPipeModule.resource_In))
				{
					list2.Add(fuelPipeModule.resource_In);
				}
				foreach (FuelPipeModule previousPipe in fuelPipeModule.previousPipes)
				{
					if (!list.Contains(previousPipe))
					{
						list.Add(previousPipe);
						stack.Push(previousPipe);
					}
				}
			}
			return list2;
		}

		public static void FixedUpdate_FuelPipeFlow(List<(ResourceModule[] froms, ResourceModule to)> flows)
		{
			if (flows == null || flows.Count == 0)
			{
				return;
			}
			// 手动筛选有燃料且目标有空间的流，空闲时不分配，避免每帧 LINQ/字典开销
			List<(ResourceModule[], ResourceModule)> active = null;
			for (int i = 0; i < flows.Count; i++)
			{
				(ResourceModule[], ResourceModule) flow = flows[i];
				ResourceModule[] froms = flow.Item1;
				ResourceModule to = flow.Item2;
				bool hasFuel = false;
				for (int j = 0; j < froms.Length; j++)
				{
					if (froms[j].ResourceAmount > 0.0)
					{
						hasFuel = true;
						break;
					}
				}
				if (hasFuel && to.ResourceSpace > 0.0)
				{
					if (active == null)
					{
						active = new List<(ResourceModule[], ResourceModule)>();
					}
					active.Add(flow);
				}
			}
			if (active == null)
			{
				return;
			}
			Dictionary<ResourceModule, int> dictionary = new Dictionary<ResourceModule, int>();
			Dictionary<ResourceModule, int> dictionary2 = new Dictionary<ResourceModule, int>();
			for (int i = 0; i < active.Count; i++)
			{
				(ResourceModule[], ResourceModule) tuple = active[i];
				ResourceModule[] src = tuple.Item1;
				ResourceModule dst = tuple.Item2;
				foreach (ResourceModule key in src)
				{
					if (!dictionary.ContainsKey(key))
					{
						dictionary.Add(key, 0);
					}
					dictionary[key]++;
				}
				if (!dictionary2.ContainsKey(dst))
				{
					dictionary2.Add(dst, 0);
				}
				dictionary2[dst]++;
			}
			Dictionary<ResourceModule, double> allowedToTake = dictionary.ToDictionary((KeyValuePair<ResourceModule, int> a) => a.Key, (KeyValuePair<ResourceModule, int> b) => b.Key.ResourceAmount / (double)b.Value);
			Dictionary<ResourceModule, double> dictionary3 = dictionary2.ToDictionary((KeyValuePair<ResourceModule, int> a) => a.Key, (KeyValuePair<ResourceModule, int> b) => b.Key.ResourceSpace / (double)b.Value);
			for (int i = 0; i < active.Count; i++)
			{
				(ResourceModule[], ResourceModule) tuple = active[i];
				ResourceModule[] src = tuple.Item1;
				ResourceModule dst = tuple.Item2;
				double num = Math.Min(dst.resourceType.transferRate * (double)Time.fixedDeltaTime, Math.Min(src.Sum((ResourceModule from) => allowedToTake[from]), dictionary3[dst]));
				for (int j = 0; j < src.Length; j++)
				{
					src[j].TakeResource(num / (double)src.Length);
				}
				dst.AddResource(num);
			}
		}
	}
}
