using C2VM.TrafficLightsEnhancement.Components;
using Unity.Entities;

namespace C2VM.TrafficLightsEnhancement.Utils;

public enum PhaseTemplate
{
	Default = 0,
	QuickCycle = 1,
	HeavyTraffic = 2,
	PedestrianFriendly = 3,
	RailPriority = 4,
	NightMode = 5,
}

public struct PhaseTemplateConfig
{
	public ushort MinDuration;
	public ushort MaxDuration;
	public float WaitFlowBalance;
	public CustomPhaseData.StepChangeMetric ChangeMetric;
	public float TargetDurationMultiplier;
}

public static class PhaseTemplates
{
	public static PhaseTemplateConfig GetTemplateConfig(PhaseTemplate template)
	{
		return template switch
		{
			PhaseTemplate.QuickCycle => new PhaseTemplateConfig
			{
				MinDuration = 5,
				MaxDuration = 15,
				WaitFlowBalance = 1.5f,
				ChangeMetric = CustomPhaseData.StepChangeMetric.Default,
				TargetDurationMultiplier = 0.8f
			},
			PhaseTemplate.HeavyTraffic => new PhaseTemplateConfig
			{
				MinDuration = 15,
				MaxDuration = 45,
				WaitFlowBalance = 0.5f,
				ChangeMetric = CustomPhaseData.StepChangeMetric.Default,
				TargetDurationMultiplier = 1.5f
			},
			PhaseTemplate.PedestrianFriendly => new PhaseTemplateConfig
			{
				MinDuration = 8,
				MaxDuration = 20,
				WaitFlowBalance = 1.2f,
				ChangeMetric = CustomPhaseData.StepChangeMetric.Default,
				TargetDurationMultiplier = 0.9f
			},
			PhaseTemplate.RailPriority => new PhaseTemplateConfig
			{
				MinDuration = 5,
				MaxDuration = 60,
				WaitFlowBalance = 1.0f,
				ChangeMetric = CustomPhaseData.StepChangeMetric.FirstWait,
				TargetDurationMultiplier = 1.0f
			},
			PhaseTemplate.NightMode => new PhaseTemplateConfig
			{
				MinDuration = 3,
				MaxDuration = 10,
				WaitFlowBalance = 2.0f,
				ChangeMetric = CustomPhaseData.StepChangeMetric.NoWait,
				TargetDurationMultiplier = 0.5f
			},
			_ => new PhaseTemplateConfig
			{
				MinDuration = 10,
				MaxDuration = 20,
				WaitFlowBalance = 1.0f,
				ChangeMetric = CustomPhaseData.StepChangeMetric.Default,
				TargetDurationMultiplier = 1.0f
			}
		};
	}

	public static void ApplyTemplate(DynamicBuffer<CustomPhaseData> phases, PhaseTemplate template)
	{
		var config = GetTemplateConfig(template);
		for (int i = 0; i < phases.Length; i++)
		{
			var phase = phases[i];
			phase.m_MinimumDuration = config.MinDuration;
			phase.m_MaximumDuration = config.MaxDuration;
			phase.m_WaitFlowBalance = config.WaitFlowBalance;
			phase.m_ChangeMetric = config.ChangeMetric;
			phase.m_TargetDurationMultiplier = config.TargetDurationMultiplier;
			phases[i] = phase;
		}
	}
}