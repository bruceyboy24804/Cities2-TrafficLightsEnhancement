using C2VM.TrafficLightsEnhancement.Components;
using Colossal.Entities;
using Colossal.Mathematics;
using Game;
using Game.Net;
using Game.Rendering;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace C2VM.TrafficLightsEnhancement.Systems.Overlay;

public partial class TrafficLightsOverlaySystem : GameSystemBase
{
	private OverlayRenderSystem m_OverlayRenderSystem;
	private UI.UISystem m_UISystem;
	private TrafficGroupSystem m_TrafficGroupSystem;
	
	private BufferLookup<ConnectedEdge> m_ConnectedEdgeLookup;
	private ComponentLookup<Edge> m_EdgeLookup;
	private ComponentLookup<EdgeGeometry> m_EdgeGeometryLookup;
	private ComponentLookup<StartNodeGeometry> m_StartNodeGeometryLookup;
	private ComponentLookup<EndNodeGeometry> m_EndNodeGeometryLookup;

	protected override void OnCreate()
	{
		base.OnCreate();
		m_OverlayRenderSystem = World.GetOrCreateSystemManaged<OverlayRenderSystem>();
		m_UISystem = World.GetOrCreateSystemManaged<UI.UISystem>();
		m_TrafficGroupSystem = World.GetOrCreateSystemManaged<TrafficGroupSystem>();
		
		// Initialize lookups
		m_ConnectedEdgeLookup = GetBufferLookup<ConnectedEdge>(true);
		m_EdgeLookup = GetComponentLookup<Edge>(true);
		m_EdgeGeometryLookup = GetComponentLookup<EdgeGeometry>(true);
		m_StartNodeGeometryLookup = GetComponentLookup<StartNodeGeometry>(true);
		m_EndNodeGeometryLookup = GetComponentLookup<EndNodeGeometry>(true);
	}

	protected override void OnUpdate()
	{
		Entity selectedEntity = m_UISystem.m_SelectedEntity;
		if (selectedEntity == Entity.Null || m_UISystem.m_MainPanelState == UI.UISystem.MainPanelState.Hidden)
		{
			return;
		}

		m_ConnectedEdgeLookup.Update(this);
		m_EdgeLookup.Update(this);
		m_EdgeGeometryLookup.Update(this);
		m_StartNodeGeometryLookup.Update(this);
		m_EndNodeGeometryLookup.Update(this);

		int displayIndex = GetDisplayIndex(selectedEntity);

		OverlayRenderSystem.Buffer overlayBuffer = m_OverlayRenderSystem.GetBuffer(out JobHandle dependencies);
		dependencies.Complete();

		DrawGizmoForEntity(ref overlayBuffer, selectedEntity, displayIndex);
		
		Entity highlightedEdge = m_UISystem.GetHighlightedEdge();
		if (highlightedEdge != Entity.Null && EntityManager.Exists(highlightedEdge))
		{
			OverlayRenderingHelpers.DrawEdgeHighlight(
				selectedEntity,
				highlightedEdge,
				ref m_ConnectedEdgeLookup,
				ref m_StartNodeGeometryLookup,
				ref m_EndNodeGeometryLookup,
				ref m_EdgeLookup,
				ref overlayBuffer,
				Color.magenta,
				1.5f);
		}
		
		OverlayRenderingHelpers.DrawNodeOutline(
			selectedEntity,
			ref m_ConnectedEdgeLookup,
			ref m_StartNodeGeometryLookup,
			ref m_EndNodeGeometryLookup,
			ref m_EdgeLookup,
			ref m_EdgeGeometryLookup,
			ref overlayBuffer,
			new Color(Color.red.r, Color.red.g, Color.red.b, 1f),
			0.5f);

		if (EntityManager.HasComponent<TrafficGroupMember>(selectedEntity))
		{
			var member = EntityManager.GetComponentData<TrafficGroupMember>(selectedEntity);
			if (member.m_GroupEntity != Entity.Null)
			{
				var groupMembers = m_TrafficGroupSystem.GetGroupMembers(member.m_GroupEntity);
				Entity leaderEntity = m_TrafficGroupSystem.GetGroupLeader(member.m_GroupEntity);

				foreach (var memberEntity in groupMembers)
				{
					if (memberEntity != selectedEntity)
					{
						DrawGizmoForEntity(ref overlayBuffer, memberEntity, displayIndex);
						Color outlineColor = memberEntity == leaderEntity 
							? new Color(243/255f, 255/255f, 22/255f, 0.9f) 
							: Color.white;
						OverlayRenderingHelpers.DrawNodeOutline(
							memberEntity,
							ref m_ConnectedEdgeLookup,
							ref m_StartNodeGeometryLookup,
							ref m_EndNodeGeometryLookup,
							ref m_EdgeLookup,
							ref m_EdgeGeometryLookup,
							ref overlayBuffer,
							outlineColor,
							0.5f);
					}
				}

				groupMembers.Dispose();
			}
		}
	}

	private int GetDisplayIndex(Entity entity)
	{
		int displayIndex = 16;
		
		if (EntityManager.TryGetComponent<CustomTrafficLights>(entity, out var customTrafficLights) && customTrafficLights.m_ManualSignalGroup > 0)
		{
			displayIndex = customTrafficLights.m_ManualSignalGroup - 1;
		}
		else if (m_UISystem.GetActiveViewingCustomPhaseIndex() >= 0)
		{
			displayIndex = m_UISystem.GetActiveViewingCustomPhaseIndex();
		}
		else if (m_UISystem.GetActiveEditingCustomPhaseIndex() >= 0)
		{
			displayIndex = m_UISystem.GetActiveEditingCustomPhaseIndex();
		}
		else if (EntityManager.TryGetComponent<TrafficLights>(entity, out var trafficLights))
		{
			displayIndex = trafficLights.m_CurrentSignalGroup - 1;
		}
		
		int debugDisplayGroup = m_UISystem.GetDebugDisplayGroup();
		if (debugDisplayGroup > 0)
		{
			displayIndex = debugDisplayGroup - 1;
		}
		
		return displayIndex;
	}

	private void DrawGizmoForEntity(ref OverlayRenderSystem.Buffer overlayBuffer, Entity entity, int displayIndex)
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


}
