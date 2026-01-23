using C2VM.TrafficLightsEnhancement.Systems.Serialization;
using Colossal.Serialization.Entities;
using Unity.Entities;
using Unity.Mathematics;

namespace C2VM.TrafficLightsEnhancement.Components;

public struct CustomPhaseData : IBufferElementData, ISerializable
{
    public enum Options : uint
    {
        PrioritiseTrack = 1 << 0,

        PrioritisePublicCar = 1 << 1,

        PrioritisePedestrian = 1 << 2,

        LinkedWithNextPhase = 1 << 3,

        EndPhasePrematurely = 1 << 4,
        
        PrioritiseBicycle = 1 << 5,

    }

    public enum StepChangeMetric : byte
    {
        Default = 0,      
        FirstFlow = 1,    
        FirstWait = 2,    
        NoFlow = 3,       
        NoWait = 4,       
    }

    public ushort m_TurnsSinceLastRun;

    public ushort m_LowFlowTimer;

    public ushort m_LowPriorityTimer;

    public float3 m_CarFlow;

    public ushort m_CarLaneOccupied;

    public ushort m_PublicCarLaneOccupied;

    public ushort m_TrackLaneOccupied;

    public ushort m_PedestrianLaneOccupied;
    
    public ushort m_BicycleLaneOccupied;
    
    public float m_WeightedWaiting;

    public float m_TargetDuration;

    public int m_Priority;

    public Options m_Options;

    public ushort m_MinimumDuration;

    public ushort m_MaximumDuration;

    public float m_TargetDurationMultiplier;

    public float m_LaneOccupiedMultiplier;

    public float m_IntervalExponent;

    public StepChangeMetric m_ChangeMetric;

    public float m_WaitFlowBalance;

    public float m_CarWeight;
    public float m_PublicCarWeight;
    public float m_TrackWeight;
    public float m_PedestrianWeight;
    public float m_BicycleWeight;

    public float m_FlowRatio;
    public float m_WaitRatio;
    public float m_SmoothingFactor; 

   
    public short m_NextStepRefIndex;
    
    public float m_CurrentFlow;
    public float m_CurrentWait;

    public short m_CarOpenDelay;
    public short m_CarCloseDelay;
    public short m_PublicCarOpenDelay;
    public short m_PublicCarCloseDelay;
    public short m_TrackOpenDelay;
    public short m_TrackCloseDelay;
    public short m_PedestrianOpenDelay;
    public short m_PedestrianCloseDelay;
    public short m_BicycleOpenDelay;
    public short m_BicycleCloseDelay;

    public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
    {
        writer.Write((ushort)TLEDataVersion.V2);
        writer.Write(m_TurnsSinceLastRun);
        writer.Write(m_LowFlowTimer);
        writer.Write(m_LowPriorityTimer);
        writer.Write(m_CarFlow);
        writer.Write(m_CarLaneOccupied);
        writer.Write(m_PublicCarLaneOccupied);
        writer.Write(m_TrackLaneOccupied);
        writer.Write(m_PedestrianLaneOccupied);
        writer.Write(m_BicycleLaneOccupied);
        writer.Write(m_WeightedWaiting);
        writer.Write(m_TargetDuration);
        writer.Write(m_Priority);
        writer.Write((uint)m_Options);
        writer.Write(m_MinimumDuration);
        writer.Write(m_MaximumDuration);
        writer.Write(m_TargetDurationMultiplier);
        writer.Write(m_LaneOccupiedMultiplier);
        writer.Write(m_IntervalExponent);
        writer.Write((byte)m_ChangeMetric);
        writer.Write(m_WaitFlowBalance);
        writer.Write(m_CarOpenDelay);
        writer.Write(m_CarCloseDelay);
        writer.Write(m_PublicCarOpenDelay);
        writer.Write(m_PublicCarCloseDelay);
        writer.Write(m_TrackOpenDelay);
        writer.Write(m_TrackCloseDelay);
        writer.Write(m_PedestrianOpenDelay);
        writer.Write(m_PedestrianCloseDelay);
        writer.Write(m_BicycleOpenDelay);
        writer.Write(m_BicycleCloseDelay);
        writer.Write(m_CarWeight);
        writer.Write(m_PublicCarWeight);
        writer.Write(m_TrackWeight);
        writer.Write(m_PedestrianWeight);
        writer.Write(m_BicycleWeight);
        writer.Write(m_FlowRatio);
        writer.Write(m_WaitRatio);
        writer.Write(m_SmoothingFactor);
        writer.Write(m_NextStepRefIndex);
        writer.Write(m_CurrentFlow);
        writer.Write(m_CurrentWait);
    }

    public void Deserialize<TReader>(TReader reader) where TReader : IReader
    {
        Initialisation();
        
        reader.Read(out ushort version);
        
        if (version < TLEDataVersion.V1)
        {
            m_TurnsSinceLastRun = version;
            reader.Read(out m_LowFlowTimer);
            reader.Read(out m_LowPriorityTimer);
            reader.Read(out m_CarFlow);
            reader.Read(out m_CarLaneOccupied);
            reader.Read(out m_PublicCarLaneOccupied);
            reader.Read(out m_TrackLaneOccupied);
            reader.Read(out m_PedestrianLaneOccupied);
            reader.Read(out m_WeightedWaiting);
            reader.Read(out m_TargetDuration);
            reader.Read(out m_Priority);
            reader.Read(out uint options);
            m_Options = (Options)options;
            reader.Read(out m_MinimumDuration);
            reader.Read(out m_MaximumDuration);
            reader.Read(out m_TargetDurationMultiplier);
            reader.Read(out m_LaneOccupiedMultiplier);
            reader.Read(out m_IntervalExponent);
        }
        else if (version < TLEDataVersion.V2)
        {
            reader.Read(out m_TurnsSinceLastRun);
            reader.Read(out m_LowFlowTimer);
            reader.Read(out m_LowPriorityTimer);
            reader.Read(out m_CarFlow);
            reader.Read(out m_CarLaneOccupied);
            reader.Read(out m_PublicCarLaneOccupied);
            reader.Read(out m_TrackLaneOccupied);
            reader.Read(out m_PedestrianLaneOccupied);
            reader.Read(out m_WeightedWaiting);
            reader.Read(out m_TargetDuration);
            reader.Read(out m_Priority);
            reader.Read(out uint options);
            m_Options = (Options)options;
            reader.Read(out m_MinimumDuration);
            reader.Read(out m_MaximumDuration);
            reader.Read(out m_TargetDurationMultiplier);
            reader.Read(out m_LaneOccupiedMultiplier);
            reader.Read(out m_IntervalExponent);
        }
        else
        {
            reader.Read(out m_TurnsSinceLastRun);
            reader.Read(out m_LowFlowTimer);
            reader.Read(out m_LowPriorityTimer);
            reader.Read(out m_CarFlow);
            reader.Read(out m_CarLaneOccupied);
            reader.Read(out m_PublicCarLaneOccupied);
            reader.Read(out m_TrackLaneOccupied);
            reader.Read(out m_PedestrianLaneOccupied);
            reader.Read(out m_BicycleLaneOccupied);
            reader.Read(out m_WeightedWaiting);
            reader.Read(out m_TargetDuration);
            reader.Read(out m_Priority);
            reader.Read(out uint options);
            m_Options = (Options)options;
            reader.Read(out m_MinimumDuration);
            reader.Read(out m_MaximumDuration);
            reader.Read(out m_TargetDurationMultiplier);
            reader.Read(out m_LaneOccupiedMultiplier);
            reader.Read(out m_IntervalExponent);
            reader.Read(out byte changeMetric);
            m_ChangeMetric = (StepChangeMetric)changeMetric;
            reader.Read(out m_WaitFlowBalance);
            reader.Read(out m_CarOpenDelay);
            reader.Read(out m_CarCloseDelay);
            reader.Read(out m_PublicCarOpenDelay);
            reader.Read(out m_PublicCarCloseDelay);
            reader.Read(out m_TrackOpenDelay);
            reader.Read(out m_TrackCloseDelay);
            reader.Read(out m_PedestrianOpenDelay);
            reader.Read(out m_PedestrianCloseDelay);
            reader.Read(out m_BicycleOpenDelay);
            reader.Read(out m_BicycleCloseDelay);
            reader.Read(out m_CarWeight);
            reader.Read(out m_PublicCarWeight);
            reader.Read(out m_TrackWeight);
            reader.Read(out m_PedestrianWeight);
            reader.Read(out m_BicycleWeight);
            reader.Read(out m_FlowRatio);
            reader.Read(out m_WaitRatio);
            reader.Read(out m_SmoothingFactor);
            reader.Read(out m_NextStepRefIndex);
            reader.Read(out m_CurrentFlow);
            reader.Read(out m_CurrentWait);
        }
    }

    private void Initialisation()
    {
        m_TurnsSinceLastRun = 0;
        m_LowFlowTimer = 0;
        m_LowPriorityTimer = 0;
        m_CarFlow = 0;
        m_CarLaneOccupied = 0;
        m_PublicCarLaneOccupied = 0;
        m_TrackLaneOccupied = 0;
        m_PedestrianLaneOccupied = 0;
        m_BicycleLaneOccupied = 0;
        m_WeightedWaiting = 0;
        m_TargetDuration = 0;
        m_Priority = 0;
        m_Options = Options.PrioritiseTrack;
        m_MinimumDuration = 2;
        m_MaximumDuration = 20;
        m_TargetDurationMultiplier = 1f;
        m_LaneOccupiedMultiplier = 1f;
        m_IntervalExponent = 2f;
        m_ChangeMetric = StepChangeMetric.Default;
        m_WaitFlowBalance = 1f;
        
        m_CarOpenDelay = 0;
        m_CarCloseDelay = 0;
        m_PublicCarOpenDelay = 0;
        m_PublicCarCloseDelay = 0;
        m_TrackOpenDelay = 0;
        m_TrackCloseDelay = 0;
        m_PedestrianOpenDelay = 0;
        m_PedestrianCloseDelay = 0;
        m_BicycleOpenDelay = 0;
        m_BicycleCloseDelay = 0;
        
        m_CarWeight = 1.0f;
        m_PublicCarWeight = 2.0f;      
        m_TrackWeight = 3.0f;          
        m_PedestrianWeight = 1.0f;
        m_BicycleWeight = 1.0f;
        m_FlowRatio = 0f;
        m_WaitRatio = 0f;
        m_SmoothingFactor = 0.5f;     
        
        m_NextStepRefIndex = -1;       
        m_CurrentFlow = 0f;
        m_CurrentWait = 0f;
    }

    public CustomPhaseData()
    {
        Initialisation();
    }

    public readonly float AverageCarFlow()
    {
        return (m_CarFlow.x + m_CarFlow.y + m_CarFlow.z) / 3f;
    }

    public readonly int TotalLaneOccupied()
    {
        return m_CarLaneOccupied + m_TrackLaneOccupied + m_PedestrianLaneOccupied;
    }

    
    public readonly float WeightedLaneOccupied()
    {
        return (m_CarLaneOccupied * m_CarWeight) +
               (m_PublicCarLaneOccupied * m_PublicCarWeight) +
               (m_TrackLaneOccupied * m_TrackWeight) +
               (m_PedestrianLaneOccupied * m_PedestrianWeight) +
               (m_BicycleLaneOccupied * m_BicycleWeight);
    }

    
    public float CalculateSmoothedWeightedWaiting(int phaseCount)
    {
        float rawWeighted = WeightedLaneOccupied() * m_LaneOccupiedMultiplier * 
            math.pow((float)m_TurnsSinceLastRun / (float)phaseCount, m_IntervalExponent);
        
        
        float smoothed = (m_SmoothingFactor * m_WeightedWaiting) + ((1f - m_SmoothingFactor) * rawWeighted);
        return smoothed;
    }

    
    public void UpdateFlowWaitRatios(float totalFlow, float totalWait)
    {
        m_FlowRatio = (m_SmoothingFactor * m_FlowRatio) + ((1f - m_SmoothingFactor) * totalFlow);
        m_WaitRatio = (m_SmoothingFactor * m_WaitRatio) + ((1f - m_SmoothingFactor) * totalWait);
    }

   
    public readonly float GetMetric(float flow, float wait)
    {
        switch (m_ChangeMetric)
        {
            case StepChangeMetric.FirstFlow:
                return flow > 0 ? flow : float.MinValue;
            case StepChangeMetric.FirstWait:
                return wait > 0 ? wait : float.MinValue;
            case StepChangeMetric.NoFlow:
                return flow <= 0 ? 1f : float.MinValue;
            case StepChangeMetric.NoWait:
                return wait <= 0 ? 1f : float.MinValue;
            case StepChangeMetric.Default:
            default:
                return flow - wait; 
        }
    }
}