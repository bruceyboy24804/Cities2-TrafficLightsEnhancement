using System;
using System.Collections.Generic;
using System.IO;
using Colossal.Json;
using Colossal.PSI.Environment;
using C2VM.TrafficLightsEnhancement.Components;

namespace C2VM.TrafficLightsEnhancement.Utils
{
	[Serializable]
	public class UserPreset
	{
		public string Id { get; set; }
		public string Name { get; set; }
		public int MinDuration { get; set; }
		public int MaxDuration { get; set; }
		public float WaitFlowBalance { get; set; }
		public int ChangeMetric { get; set; }
		public float TargetDurationMultiplier { get; set; }

		public UserPreset()
		{
			Id = Guid.NewGuid().ToString("N").Substring(0, 8);
			Name = "Custom Preset";
			MinDuration = 10;
			MaxDuration = 20;
			WaitFlowBalance = 1.0f;
			ChangeMetric = 0;
			TargetDurationMultiplier = 1.0f;
		}

		public UserPreset(string name, CustomPhaseData phase)
		{
			Id = Guid.NewGuid().ToString("N").Substring(0, 8);
			Name = name;
			MinDuration = phase.m_MinimumDuration;
			MaxDuration = phase.m_MaximumDuration;
			WaitFlowBalance = phase.m_WaitFlowBalance;
			ChangeMetric = (int)phase.m_ChangeMetric;
			TargetDurationMultiplier = phase.m_TargetDurationMultiplier;
		}

		public PhaseTemplateConfig ToConfig()
		{
			return new PhaseTemplateConfig
			{
				MinDuration = (ushort)MinDuration,
				MaxDuration = (ushort)MaxDuration,
				WaitFlowBalance = WaitFlowBalance,
				ChangeMetric = (CustomPhaseData.StepChangeMetric)ChangeMetric,
				TargetDurationMultiplier = TargetDurationMultiplier
			};
		}
	}

	public static class UserPresetsManager
	{
		private static readonly string PresetsDirectory = Path.Combine(
			EnvPath.kUserDataPath,
			"ModsData",
			Mod.m_Id,
			"Presets"
		);

		private static List<UserPreset> _presets = new List<UserPreset>();
		private static readonly object _lock = new object();

		private static string GetFilePath(string id) => Path.Combine(PresetsDirectory, id + ".json");

		private static bool EnsureDirectory()
		{
			if (!Directory.Exists(PresetsDirectory))
			{
				Directory.CreateDirectory(PresetsDirectory);
				return true;
			}
			return true;
		}

		public static void Initialize(string unused = null)
		{
			Load();
		}

		public static void Load()
		{
			lock (_lock)
			{
				_presets.Clear();
				try
				{
					if (Directory.Exists(PresetsDirectory))
					{
						var files = Directory.GetFiles(PresetsDirectory, "*.json");
						foreach (var file in files)
						{
							try
							{
								var text = File.ReadAllText(file);
								var preset = JSON.MakeInto<UserPreset>(JSON.Load(text));
								if (preset != null)
								{
									_presets.Add(preset);
								}
							}
							catch (Exception ex)
							{
								Mod.m_Log.Info($"[TLE] Error loading preset {file}: {ex.Message}");
							}
						}
					}
				}
				catch (Exception ex)
				{
					Mod.m_Log.Error($"[TLE] Failed to load user presets: {ex.Message}");
				}
			}
		}

		public static List<UserPreset> GetAllPresets()
		{
			lock (_lock)
			{
				return new List<UserPreset>(_presets);
			}
		}

		public static UserPreset GetPreset(string id)
		{
			lock (_lock)
			{
				return _presets.Find(p => p.Id == id);
			}
		}

		public static void AddPreset(UserPreset preset)
		{
			lock (_lock)
			{
				EnsureDirectory();
				_presets.Add(preset);
				SavePreset(preset);
			}
		}

		private static void SavePreset(UserPreset preset)
		{
			try
			{
				EnsureDirectory();
				File.WriteAllText(GetFilePath(preset.Id), JSON.Dump(preset));
			}
			catch (Exception ex)
			{
				Mod.m_Log.Error($"[TLE] Failed to save preset {preset.Name}: {ex.Message}");
			}
		}

		public static bool DeletePreset(string id)
		{
			lock (_lock)
			{
				var preset = _presets.Find(p => p.Id == id);
				if (preset != null)
				{
					_presets.Remove(preset);
					try
					{
						var path = GetFilePath(id);
						if (File.Exists(path))
						{
							File.Delete(path);
						}
					}
					catch (Exception ex)
					{
						Mod.m_Log.Error($"[TLE] Failed to delete preset file: {ex.Message}");
					}
					return true;
				}
				return false;
			}
		}

		public static bool UpdatePreset(string id, string newName)
		{
			lock (_lock)
			{
				var preset = _presets.Find(p => p.Id == id);
				if (preset != null)
				{
					preset.Name = newName;
					SavePreset(preset);
					return true;
				}
				return false;
			}
		}
	}
}