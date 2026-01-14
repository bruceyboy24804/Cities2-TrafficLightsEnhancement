using System.Runtime.CompilerServices;
using C2VM.TrafficLightsEnhancement.Components;
using Game.Net;
using Game.Prefabs;
using Unity.Collections;
using Unity.Entities;

namespace C2VM.TrafficLightsEnhancement.Systems.UI;

public struct TypeHandle
{
    [ReadOnly]
    public BufferLookup<Game.Net.SubLane> m_SubLane;

    [ReadOnly]
    public BufferLookup<ConnectedEdge> m_ConnectedEdge;

    [ReadOnly]
    public BufferLookup<EdgeGroupMask> m_EdgeGroupMask;

    [ReadOnly]
    public BufferLookup<SubLaneGroupMask> m_SubLaneGroupMask;

    [ReadOnly]
    public BufferLookup<LaneOverlap> m_LaneOverlap;

    [ReadOnly]
    public ComponentLookup<Edge> m_Edge;

    [ReadOnly]
    public ComponentLookup<EdgeGeometry> m_EdgeGeometry;

    [ReadOnly]
    public ComponentLookup<Lane> m_Lane;

    [ReadOnly]
    public ComponentLookup<Game.Net.PedestrianLane> m_PedestrianLane;

    [ReadOnly]
    public ComponentLookup<MasterLane> m_MasterLane;

    [ReadOnly]
    public ComponentLookup<Game.Net.TrackLane> m_TrackLane;

    [ReadOnly]
    public ComponentLookup<Game.Net.CarLane> m_CarLane;

    [ReadOnly]
    public ComponentLookup<Curve> m_Curve;

    [ReadOnly]
    public ComponentLookup<TrainTrack> m_TrainTrack;

    [ReadOnly]
    public ComponentLookup<Game.Net.SecondaryLane> m_SecondaryLane;

    [ReadOnly]
    public ComponentLookup<Game.Net.EdgeLane> m_EdgeLane;

    [ReadOnly]
    public ComponentLookup<Game.Net.SlaveLane> m_SlaveLane;

    [ReadOnly]
    public ComponentLookup<Composition> m_Composition;

    [ReadOnly]
    public ComponentLookup<PrefabRef> m_PrefabRef;

    [ReadOnly]
    public ComponentLookup<NetLaneData> m_NetLaneData;

    [ReadOnly]
    public ComponentLookup<CarLaneData> m_CarLaneData;

    [ReadOnly]
    public ComponentLookup<TrackLaneData> m_TrackLaneData;

    [ReadOnly]
    public BufferLookup<NetCompositionLane> m_NetCompositionLane;

    [ReadOnly]
    public ComponentLookup<NetCompositionData> m_NetCompositionData;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AssignHandles(ref SystemState state)
    {
        m_SubLane = state.GetBufferLookup<Game.Net.SubLane>(true);
        m_ConnectedEdge = state.GetBufferLookup<ConnectedEdge>(true);
        m_EdgeGroupMask = state.GetBufferLookup<EdgeGroupMask>(true);
        m_SubLaneGroupMask = state.GetBufferLookup<SubLaneGroupMask>(true);
        m_LaneOverlap = state.GetBufferLookup<LaneOverlap>(true);
        m_Edge = state.GetComponentLookup<Edge>(true);
        m_EdgeGeometry = state.GetComponentLookup<EdgeGeometry>(true);
        m_Lane = state.GetComponentLookup<Lane>(true);
        m_PedestrianLane = state.GetComponentLookup<Game.Net.PedestrianLane>(true);
        m_MasterLane = state.GetComponentLookup<MasterLane>(true);
        m_TrackLane = state.GetComponentLookup<Game.Net.TrackLane>(true);
        m_CarLane = state.GetComponentLookup<Game.Net.CarLane>(true);
        m_Curve = state.GetComponentLookup<Curve>(true);
        m_TrainTrack = state.GetComponentLookup<TrainTrack>(true);
        m_SecondaryLane = state.GetComponentLookup<Game.Net.SecondaryLane>(true);
        m_EdgeLane = state.GetComponentLookup<Game.Net.EdgeLane>(true);
        m_SlaveLane = state.GetComponentLookup<Game.Net.SlaveLane>(true);
        m_Composition = state.GetComponentLookup<Composition>(true);
        m_PrefabRef = state.GetComponentLookup<PrefabRef>(true);
        m_NetLaneData = state.GetComponentLookup<NetLaneData>(true);
        m_CarLaneData = state.GetComponentLookup<CarLaneData>(true);
        m_TrackLaneData = state.GetComponentLookup<TrackLaneData>(true);
        m_NetCompositionLane = state.GetBufferLookup<NetCompositionLane>(true);
        m_NetCompositionData = state.GetComponentLookup<NetCompositionData>(true);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Update(SystemBase system)
    {
        m_SubLane.Update(system);
        m_ConnectedEdge.Update(system);
        m_EdgeGroupMask.Update(system);
        m_SubLaneGroupMask.Update(system);
        m_LaneOverlap.Update(system);
        m_Edge.Update(system);
        m_EdgeGeometry.Update(system);
        m_Lane.Update(system);
        m_PedestrianLane.Update(system);
        m_MasterLane.Update(system);
        m_TrackLane.Update(system);
        m_CarLane.Update(system);
        m_Curve.Update(system);
        m_TrainTrack.Update(system);
        m_SecondaryLane.Update(system);
        m_EdgeLane.Update(system);
        m_SlaveLane.Update(system);
        m_Composition.Update(system);
        m_PrefabRef.Update(system);
        m_NetLaneData.Update(system);
        m_CarLaneData.Update(system);
        m_TrackLaneData.Update(system);
        m_NetCompositionLane.Update(system);
        m_NetCompositionData.Update(system);
    }
}