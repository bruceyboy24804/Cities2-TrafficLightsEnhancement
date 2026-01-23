using C2VM.TrafficLightsEnhancement.Components;
using C2VM.TrafficLightsEnhancement.Extensions;
using C2VM.TrafficLightsEnhancement.Systems.Overlay;
using Colossal.Entities;
using Colossal.Mathematics;
using Game.Net;
using Game.Rendering;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace C2VM.TrafficLightsEnhancement.Systems.UI;

public partial class UISystem : ExtendedUISystemBase
{
    public void RedrawGizmo()
    {
        if (m_SelectedEntity != Entity.Null)
        {
            var overlayRenderSystem = World.GetOrCreateSystemManaged<OverlayRenderSystem>();
            var overlayBuffer = overlayRenderSystem.GetBuffer(out JobHandle dependencies);
            dependencies.Complete();
            
            int displayIndex = 16;
            if (EntityManager.TryGetComponent<CustomTrafficLights>(m_SelectedEntity, out var customTrafficLights) && customTrafficLights.m_ManualSignalGroup > 0)
            {
                displayIndex = customTrafficLights.m_ManualSignalGroup - 1;
            }
            else if (m_ActiveViewingCustomPhaseIndexBinding.Value >= 0)
            {
                displayIndex = m_ActiveViewingCustomPhaseIndexBinding.Value;
            }
            else if (m_ActiveEditingCustomPhaseIndexBinding.Value >= 0)
            {
                displayIndex = m_ActiveEditingCustomPhaseIndexBinding.Value;
            }
            else if (EntityManager.TryGetComponent<TrafficLights>(m_SelectedEntity, out var trafficLights))
            {
                displayIndex = trafficLights.m_CurrentSignalGroup - 1;
            }
            if (m_DebugDisplayGroup > 0)
            {
                displayIndex = m_DebugDisplayGroup - 1;
            }

            bool showUncovered = m_MainPanelState == MainPanelState.CustomPhase;
            DrawGizmoForEntity(ref overlayBuffer, m_SelectedEntity, displayIndex, showUncovered);
            DrawNodeOutline(ref overlayBuffer, m_SelectedEntity, new Color(0f, 0.83f, 1f, 1f)); 
            
            
            if (m_HighlightedEdge != Entity.Null && EntityManager.Exists(m_HighlightedEdge))
            {
                DrawEdgeHighlight(ref overlayBuffer, m_SelectedEntity, m_HighlightedEdge, Color.magenta);
            }

            
            if (EntityManager.HasComponent<TrafficGroupMember>(m_SelectedEntity))
            {
                var member = EntityManager.GetComponentData<TrafficGroupMember>(m_SelectedEntity);
                if (member.m_GroupEntity != Entity.Null)
                {
                    var trafficGroupSystem = World.GetOrCreateSystemManaged<TrafficGroupSystem>();
                    var groupMembers = trafficGroupSystem.GetGroupMembers(member.m_GroupEntity);
                    Entity leaderEntity = trafficGroupSystem.GetGroupLeader(member.m_GroupEntity);

                    foreach (var memberEntity in groupMembers)
                    {
                        if (memberEntity != m_SelectedEntity)
                        {
                            
                            
                            int memberDisplayIndex = displayIndex;
                            if (m_MainPanelState == MainPanelState.CustomPhase)
                            {
                                if (EntityManager.TryGetComponent<TrafficLights>(memberEntity, out var memberTrafficLights))
                                {
                                    memberDisplayIndex = memberTrafficLights.m_CurrentSignalGroup - 1;
                                }
                            }
                            
                            DrawGizmoForEntity(ref overlayBuffer, memberEntity, memberDisplayIndex);
                            
                            
                            Color outlineColor = memberEntity == leaderEntity ? Color.yellow : Color.white;
                            DrawNodeOutline(ref overlayBuffer, memberEntity, outlineColor);
                        }
                    }

                    groupMembers.Dispose();
                }
            }
        }
    }

    private void DrawGizmoForEntity(ref OverlayRenderSystem.Buffer overlayBuffer, Entity entity, int displayIndex, bool showUncovered = false)
    {
        if (EntityManager.TryGetBuffer<SubLane>(entity, true, out var subLaneBuffer))
        {
            foreach (var subLane in subLaneBuffer)
            {
                Entity subLaneEntity = subLane.m_SubLane;
                bool isPedestrian = EntityManager.TryGetComponent<PedestrianLane>(subLaneEntity, out var pedestrianLane);
                if (EntityManager.HasComponent<MasterLane>(subLaneEntity))
                {
                    continue;
                }
                if (!EntityManager.HasComponent<CarLane>(subLaneEntity) && !EntityManager.HasComponent<TrackLane>(subLaneEntity) && !isPedestrian)
                {
                    continue;
                }
                if (isPedestrian && (pedestrianLane.m_Flags & PedestrianLaneFlags.Crosswalk) == 0)
                {
                    continue;
                }
                if (EntityManager.TryGetComponent<LaneSignal>(subLaneEntity, out var laneSignal) && EntityManager.TryGetComponent<Curve>(subLaneEntity, out var curve))
                {
                    
                    if (showUncovered && laneSignal.m_GroupMask == 0)
                    {
                        overlayBuffer.DrawCurve(new Color(1f, 0.5f, 0f, 1f), curve.m_Bezier, 0.5f); 
                        continue;
                    }
                    
                    Color color = Color.green;
                    if (EntityManager.TryGetComponent<ExtraLaneSignal>(subLaneEntity, out var extraLaneSignal) && (extraLaneSignal.m_YieldGroupMask & 1 << displayIndex) != 0)
                    {
                        color = Color.blue;
                    }
                    if ((laneSignal.m_GroupMask & 1 << displayIndex) != 0)
                    {
                        overlayBuffer.DrawCurve(color, curve.m_Bezier, 0.5f);
                    }
                }
            }
        }
    }

    private void DrawEdgeHighlight(ref OverlayRenderSystem.Buffer overlayBuffer, Entity node, Entity targetEdge, Color color, float lineWidth = 1.5f)
    {
        if (!EntityManager.TryGetBuffer<ConnectedEdge>(node, true, out var connectedEdges))
        {
            return;
        }

        foreach (var connectedEdge in connectedEdges)
        {
            if (connectedEdge.m_Edge != targetEdge)
            {
                continue;
            }
            
            if (!EntityManager.TryGetComponent<Edge>(connectedEdge.m_Edge, out var edge))
            {
                continue;
            }

            bool isEnd = node == edge.m_End;

            if (EntityManager.TryGetComponent<StartNodeGeometry>(connectedEdge.m_Edge, out var startNodeGeometry) &&
                EntityManager.TryGetComponent<EndNodeGeometry>(connectedEdge.m_Edge, out var endNodeGeometry))
            {
                EdgeNodeGeometry nodeGeometry = isEnd ? endNodeGeometry.m_Geometry : startNodeGeometry.m_Geometry;
                overlayBuffer.DrawLine(color, new Line3.Segment(nodeGeometry.m_Left.m_Left.a, nodeGeometry.m_Right.m_Right.a), lineWidth);
            }
            break;
        }
    }

    private void DrawNodeOutline(ref OverlayRenderSystem.Buffer overlayBuffer, Entity node, Color color, float lineWidth = 0.5f)
    {
        if (!EntityManager.TryGetBuffer<ConnectedEdge>(node, true, out var connectedEdges))
        {
            return;
        }

        foreach (var connectedEdge in connectedEdges)
        {
            if (!EntityManager.TryGetComponent<Edge>(connectedEdge.m_Edge, out var edge))
            {
                continue;
            }
            if (!EntityManager.TryGetComponent<EdgeGeometry>(connectedEdge.m_Edge, out var edgeGeometry))
            {
                continue;
            }

            bool isEnd = node == edge.m_End;
            Segment edgeSegment = isEnd ? edgeGeometry.m_End : edgeGeometry.m_Start;

            float3 leftPoint = isEnd ? edgeSegment.m_Left.d : edgeSegment.m_Left.a;
            float3 rightPoint = isEnd ? edgeSegment.m_Right.d : edgeSegment.m_Right.a;
            overlayBuffer.DrawLine(color, new Line3.Segment(leftPoint, rightPoint), lineWidth);

            if (EntityManager.TryGetComponent<StartNodeGeometry>(connectedEdge.m_Edge, out var startNodeGeometry) &&
                EntityManager.TryGetComponent<EndNodeGeometry>(connectedEdge.m_Edge, out var endNodeGeometry))
            {
                EdgeNodeGeometry nodeGeometry = isEnd ? endNodeGeometry.m_Geometry : startNodeGeometry.m_Geometry;
                overlayBuffer.DrawCurve(color, nodeGeometry.m_Left.m_Left, lineWidth);
                overlayBuffer.DrawCurve(color, nodeGeometry.m_Right.m_Right, lineWidth);
                
                if (nodeGeometry.m_MiddleRadius > 0)
                {
                    overlayBuffer.DrawCurve(color, nodeGeometry.m_Left.m_Right, lineWidth);
                    overlayBuffer.DrawCurve(color, nodeGeometry.m_Right.m_Left, lineWidth);
                }
            }
        }
    }

    public void RedrawIcon()
    {
        m_RenderSystem.ClearIconList();
        if (m_MainPanelState == MainPanelState.Empty)
        {
            var entityQuery = EntityManager.CreateEntityQuery(ComponentType.ReadOnly<Node>(), ComponentType.ReadOnly<CustomTrafficLights>());
            var nodeArray = entityQuery.ToComponentDataArray<Node>(Allocator.Temp);
            var customTrafficLightsArray = entityQuery.ToComponentDataArray<CustomTrafficLights>(Allocator.Temp);
            for (int i = 0; i < nodeArray.Length; i++)
            {
                var node = nodeArray[i];
                var customTrafficLights = customTrafficLightsArray[i];
                RenderSystem.Icon icon = RenderSystem.Icon.TrafficLight;
                if (customTrafficLights.GetPatternOnly() == CustomTrafficLights.Patterns.CustomPhase)
                {
                    icon = RenderSystem.Icon.TrafficLightWrench;
                }
                m_RenderSystem.AddIcon(node.m_Position, icon);
            }
            nodeArray.Dispose();
            customTrafficLightsArray.Dispose();
        }
    }
}