using System;
using System.Collections.Generic;
using SFS.Parts;

namespace SFS.World
{
	[Serializable]
	public class StageSave
	{
		public int stageId;

		public int[] partIndexes;

		public static StageSave[] CreateSaves(Staging staging, List<Part> parts)
		{
			Dictionary<Part, int> partIndexes = new Dictionary<Part, int>(parts.Count);
			for (int i = 0; i < parts.Count; i++)
			{
				if (!partIndexes.ContainsKey(parts[i]))
				{
					partIndexes[parts[i]] = i;
				}
			}
			StageSave[] output = new StageSave[staging.stages.Count];
			for (int s = 0; s < staging.stages.Count; s++)
			{
				Stage stage = staging.stages[s];
				List<int> list = new List<int>(stage.PartCount);
				for (int i = 0; i < stage.PartCount; i++)
				{
					if (partIndexes.TryGetValue(stage.parts[i], out int num))
					{
						list.Add(num);
					}
				}
				output[s] = new StageSave
				{
					stageId = stage.stageId,
					partIndexes = list.ToArray()
				};
			}
			return output;
		}
	}
}
