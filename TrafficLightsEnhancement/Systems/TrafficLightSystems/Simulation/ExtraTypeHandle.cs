using System.Runtime.CompilerServices;
using C2VM.TrafficLightsEnhancement.Components;
using Game.Net;
using Unity.Collections;
using Unity.Entities;

namespace C2VM.TrafficLightsEnhancement.Systems.TrafficLightSystems.Simulation;

public struct ExtraTypeHandle
{
    public ComponentTypeHandle<CustomTrafficLights> m_CustomTrafficLights;

    [ReadOnly]
    public ComponentLookup<ExtraLaneSignal> m_ExtraLaneSignal;

    public BufferTypeHandle<CustomPhaseData> m_CustomPhaseData;

    [ReadOnly]
    public BufferTypeHandle<EdgeGroupMask> m_EdgeGroupMask;

    [ReadOnly]
    public ComponentLookup<LaneFlow> m_LaneFlow;

    public ComponentLookup<LaneFlowHistory> m_LaneFlowHistory;

    [ReadOnly]
    public ComponentLookup<MasterLane> m_MasterLane;

    [ReadOnly]
    public ComponentLookup<CarLane> m_CarLane;

    [ReadOnly]
    public ComponentLookup<TrackLane> m_TrackLane;

    [ReadOnly]
    public ComponentLookup<PedestrianLane> m_PedestrianLane;
    
    public ComponentLookup<SecondaryLane> m_SecondaryLane;

    [ReadOnly]
    public ComponentLookup<TrafficGroupMember> m_TrafficGroupMember;

    [ReadOnly]
    public ComponentLookup<TrafficGroup> m_TrafficGroup;

    [ReadOnly]
    public BufferLookup<CustomPhaseData> m_CustomPhaseDataLookup;

    [ReadOnly]
    public ComponentLookup<Game.Net.TrafficLights> m_TrafficLightsLookup;

    [ReadOnly]
    public ComponentLookup<CustomTrafficLights> m_CustomTrafficLightsLookup;

    [ReadOnly]
    public BufferLookup<EdgeGroupMask> m_EdgeGroupMaskLookup;

    [ReadOnly]
    public BufferLookup<SignalDelayData> m_SignalDelayLookup;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AssignHandles(ref SystemState state)
    {
        m_CustomTrafficLights = state.GetComponentTypeHandle<CustomTrafficLights>(isReadOnly: false);
        m_ExtraLaneSignal = state.GetComponentLookup<ExtraLaneSignal>(isReadOnly: true);
        m_CustomPhaseData = state.GetBufferTypeHandle<CustomPhaseData>(isReadOnly: false);
        m_EdgeGroupMask = state.GetBufferTypeHandle<EdgeGroupMask>(isReadOnly: true);
        m_CustomPhaseDataLookup = state.GetBufferLookup<CustomPhaseData>(isReadOnly: true);
        m_LaneFlow = state.GetComponentLookup<LaneFlow>(isReadOnly: true);
        m_LaneFlowHistory = state.GetComponentLookup<LaneFlowHistory>(isReadOnly: false);
        m_MasterLane = state.GetComponentLookup<MasterLane>(isReadOnly: true);
        m_CarLane = state.GetComponentLookup<CarLane>(isReadOnly: true);
        m_TrackLane = state.GetComponentLookup<TrackLane>(isReadOnly: true);
        m_PedestrianLane = state.GetComponentLookup<PedestrianLane>(isReadOnly: true);
        m_SecondaryLane = state.GetComponentLookup<SecondaryLane>(isReadOnly: true);
        m_TrafficGroupMember = state.GetComponentLookup<TrafficGroupMember>(isReadOnly: true);
        m_TrafficGroup = state.GetComponentLookup<TrafficGroup>(isReadOnly: true);
        m_CustomPhaseDataLookup = state.GetBufferLookup<CustomPhaseData>(isReadOnly: true);
        m_TrafficLightsLookup = state.GetComponentLookup<Game.Net.TrafficLights>(isReadOnly: true);
        m_CustomTrafficLightsLookup = state.GetComponentLookup<CustomTrafficLights>(isReadOnly: true);
        m_EdgeGroupMaskLookup = state.GetBufferLookup<EdgeGroupMask>(isReadOnly: true);
        m_SignalDelayLookup = state.GetBufferLookup<SignalDelayData>(isReadOnly: true);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ExtraTypeHandle Update(ref SystemState state)
    {
        m_CustomTrafficLights.Update(ref state);
        m_ExtraLaneSignal.Update(ref state);
        m_CustomPhaseData.Update(ref state);
        m_EdgeGroupMask.Update(ref state);
        m_LaneFlow.Update(ref state);
        m_LaneFlowHistory.Update(ref state);
        m_MasterLane.Update(ref state);
        m_CarLane.Update(ref state);
        m_TrackLane.Update(ref state);
        m_PedestrianLane.Update(ref state);
        m_SecondaryLane.Update(ref state);
        m_TrafficGroupMember.Update(ref state);
        m_TrafficGroup.Update(ref state);
        m_CustomPhaseDataLookup.Update(ref state);
        m_TrafficLightsLookup.Update(ref state);
        m_CustomTrafficLightsLookup.Update(ref state);
        m_EdgeGroupMaskLookup.Update(ref state);
        m_SignalDelayLookup.Update(ref state);
        return this;
    }
}