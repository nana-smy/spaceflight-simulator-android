using System;
using System.Collections.Generic;
using System.Linq;
using SFS.Translations;
using SFS.UI;
using SFS.World.Maps;
using UnityEngine;

namespace SFS.World
{
	public class LocationDrawer : MonoBehaviour
	{
		public static LocationDrawer main;

		public TextAdapter heightText;

		public TextAdapter velocityText;

		public TextAdapter heightTextSide;

		public TextAdapter velocityTextSide;

		public TextAdapter angleText;

		public TextAdapter heightTitle;

		public TextAdapter velocityTitle;

		public AngleInfo currentAngleInfo;

		private void Awake()
		{
			main = this;
			OptionsMenuDrawer.LocalizeSettingsTexts(base.transform.root);
		}

		private void Update()
		{
			bool flag = false;
			Location location = ((PlayerController.main.player.Value != null) ? PlayerController.main.player.Value.location.Value : WorldView.main.ViewLocation);
			SelectableObject target = Map.navigation.target;
			string velocityFull;
			string heightFull;
			if (target is MapRocket && (location.position - target.Location.position).Mag_LessThan(10000.0))
			{
				velocityFull = BuildField(flag ? Loc.main.Velocity_Relative_Vertical : Loc.main.Velocity_Relative_Horizontal, "speed", (location.velocity - target.Location.velocity).magnitude.ToVelocityString());
				heightFull = BuildField(flag ? Loc.main.Distance_Relative_Vertical : Loc.main.Distance_Relative_Horizontal, "distance", (location.position - target.Location.position).magnitude.ToDistanceString());
			}
			else
			{
				velocityFull = BuildField(flag ? Loc.main.Velocity_Vertical : Loc.main.Velocity_Horizontal, "speed", location.velocity.magnitude.ToVelocityString());
				bool showHeight = !(location.TerrainHeight < 2000.0) && !(location.Height < 500.0);
				Field heightField = flag ? (showHeight ? Loc.main.Height_Vertical : Loc.main.Height_Terrain_Vertical) : (showHeight ? Loc.main.Height_Horizontal : Loc.main.Height_Terrain_Horizontal);
				heightFull = BuildField(heightField, "height", (showHeight ? location.Height : location.TerrainHeight).ToDistanceString());
			}
			GetIdealAngle(out var showAngle, out var angle, out var targetAngle);
			currentAngleInfo = new AngleInfo(showAngle, angle, targetAngle);
			SplitTitleValue(heightFull, out string heightTitleValue, out string heightValue);
			heightTitle.Text = heightTitleValue;
			heightText.Text = heightValue;
			SplitTitleValue(velocityFull, out string velocityTitleValue, out string velocityValue);
			velocityTitle.Text = velocityTitleValue;
			velocityText.Text = velocityValue;
		}

		private static string BuildField(Field field, string variableName, string value)
		{
			return string.Join("", field.GetSubs().Select((x) => x.Item2)).Replace("%" + variableName + "%", value);
		}

		private static void SplitTitleValue(string full, out string title, out string value)
		{
			int index = full.IndexOf(':');
			if (index >= 0)
			{
				title = full.Substring(0, index);
				value = full.Substring(index + 1).TrimStart();
			}
			else
			{
				title = "";
				value = full;
			}
		}

		private static void GetIdealAngle(out bool showAngle, out double angle, out double targetAngle)
		{
			showAngle = false;
			angle = 0.0;
			targetAngle = 0.0;
			if (PlayerController.main.player.Value == null || !(PlayerController.main.player.Value is Rocket rocket))
			{
				return;
			}
			List<I_Path> paths = rocket.physics.GetTrajectory().paths;
			if (paths.Count == 0 || paths[0].Planet.codeName != "Earth" || !(paths[0] is Orbit orbit))
			{
				return;
			}
			double num = orbit.apoapsis - orbit.Planet.Radius;
			if (!(num > orbit.Planet.data.atmospherePhysics.height) && !(rocket.location.velocity.Value.Rotate(0.0 - rocket.location.position.Value.AngleRadians + Math.PI / 2.0).y < 5.0))
			{
				showAngle = true;
				for (angle = 90f - rocket.GetRotation(); angle < -180.0; angle += 360.0)
				{
				}
				while (angle > 180.0)
				{
					angle -= 360.0;
				}
				num /= 1.1;
				double num2 = Base.worldBase.settings.difficulty.AtmosphereScale(paths[0].Planet.data);
				targetAngle = ((!(num < num2 * 300.0)) ? ((num < num2 * 1000.0) ? 5 : ((num < num2 * 2500.0) ? 10 : ((num < num2 * 4500.0) ? 20 : ((num < num2 * 7500.0) ? 30 : ((num < num2 * 12000.0) ? 45 : ((num < num2 * 20000.0) ? 60 : 75)))))) : 0);
				if (angle < 0.0)
				{
					targetAngle = 0.0 - targetAngle;
				}
			}
		}
	}
}
