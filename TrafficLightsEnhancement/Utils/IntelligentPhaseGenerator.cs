using C2VM.TrafficLightsEnhancement.Components;
using Game.Net;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace C2VM.TrafficLightsEnhancement.Utils;





public struct IntelligentPhaseGenerator
{
	
	
	
	public struct PhaseGenerationResult
	{
		public NativeList<CustomPhaseData> Phases;
		public NativeList<EdgeGroupMask> EdgeGroupMasks;
		public int PhaseCount;
		public bool Success;
	}

	
	
	
	public static PhaseGenerationResult GenerateIntelligentPhases(
		Allocator allocator,
		Entity nodeEntity,
		NativeList<ComputedLaneConnection> laneConnections,
		DynamicBuffer<ConnectedEdge> connectedEdges,
		ComponentLookup<Edge> edgeLookup,
		ComponentLookup<EdgeGeometry> edgeGeometryLookup,
		bool leftHandTraffic)
	{
		PhaseGenerationResult result = new()
		{
			Phases = new NativeList<CustomPhaseData>(8, allocator),
			EdgeGroupMasks = new NativeList<EdgeGroupMask>(8, allocator),
			PhaseCount = 0,
			Success = false
		};

		if (laneConnections.Length == 0 || connectedEdges.Length < 2)
		{
			return result;
		}

		
		NativeHashMap<Entity, NativeList<int>> connectionsByEdge = new(connectedEdges.Length, Allocator.Temp);
		for (int i = 0; i < laneConnections.Length; i++)
		{
			Entity sourceEdge = laneConnections[i].m_SourceEdge;
			if (!connectionsByEdge.TryGetValue(sourceEdge, out NativeList<int> indices))
			{
				indices = new NativeList<int>(8, Allocator.Temp);
				connectionsByEdge[sourceEdge] = indices;
			}
			indices.Add(i);
		}

		
		NativeList<NativeList<int>> phaseGroups = LaneConnectorGenerator.FindNonConflictingGroups(laneConnections, Allocator.Temp);

		
		int maxPhases = math.min(phaseGroups.Length, 16);

		
		for (int phaseIndex = 0; phaseIndex < maxPhases; phaseIndex++)
		{
			NativeList<int> connectionIndices = phaseGroups[phaseIndex];
			if (connectionIndices.Length == 0)
			{
				continue;
			}

			
			CustomPhaseData phaseData = new()
			{
				m_MinimumDuration = 10,
				m_TargetDuration = 30,
				m_MaximumDuration = 60,
				m_Priority = 1,
				m_TargetDurationMultiplier = 1f
			};
			result.Phases.Add(phaseData);

			ushort phaseBit = (ushort)(1 << phaseIndex);

			
			foreach (int connIndex in connectionIndices)
			{
				ComputedLaneConnection conn = laneConnections[connIndex];
				Entity sourceEdge = conn.m_SourceEdge;

				
				int edgeMaskIndex = FindEdgeGroupMaskIndex(result.EdgeGroupMasks, sourceEdge);
				EdgeGroupMask edgeMask;

				if (edgeMaskIndex >= 0)
				{
					edgeMask = result.EdgeGroupMasks[edgeMaskIndex];
				}
				else
				{
					float3 edgePosition = NodeUtils.GetEdgePosition(nodeEntity, sourceEdge, edgeLookup, edgeGeometryLookup);
					edgeMask = new EdgeGroupMask(sourceEdge, edgePosition);
					edgeMaskIndex = result.EdgeGroupMasks.Length;
					result.EdgeGroupMasks.Add(edgeMask);
				}

				
				SetSignalMasks(ref edgeMask, conn, phaseBit, leftHandTraffic);
				result.EdgeGroupMasks[edgeMaskIndex] = edgeMask;
			}

			result.PhaseCount++;
		}

		
		EnsureAllEdgesHaveMasks(ref result, nodeEntity, connectedEdges, edgeLookup, edgeGeometryLookup);

		
		foreach (var group in phaseGroups)
		{
			group.Dispose();
		}
		phaseGroups.Dispose();

		foreach (var kvp in connectionsByEdge)
		{
			kvp.Value.Dispose();
		}
		connectionsByEdge.Dispose();

		result.Success = result.PhaseCount > 0;
		return result;
	}

	
	
	
	public static PhaseGenerationResult GenerateSplitPhasingWithCarriageways(
		Allocator allocator,
		Entity nodeEntity,
		NativeList<ComputedLaneConnection> laneConnections,
		DynamicBuffer<ConnectedEdge> connectedEdges,
		ComponentLookup<Edge> edgeLookup,
		ComponentLookup<EdgeGeometry> edgeGeometryLookup,
		bool leftHandTraffic)
	{
		PhaseGenerationResult result = new()
		{
			Phases = new NativeList<CustomPhaseData>(8, allocator),
			EdgeGroupMasks = new NativeList<EdgeGroupMask>(8, allocator),
			PhaseCount = 0,
			Success = false
		};

		if (connectedEdges.Length < 2)
		{
			return result;
		}

		
		NativeHashMap<int2, NativeList<int>> carriageways = LaneConnectorGenerator.GroupConnectionsByCarriageway(laneConnections, Allocator.Temp);

		
		int phaseIndex = 0;
		foreach (ConnectedEdge connectedEdge in connectedEdges)
		{
			if (phaseIndex >= 16)
			{
				break;
			}

			Entity edgeEntity = connectedEdge.m_Edge;
			float3 edgePosition = NodeUtils.GetEdgePosition(nodeEntity, edgeEntity, edgeLookup, edgeGeometryLookup);

			
			CustomPhaseData phaseData = new()
			{
				m_MinimumDuration = 10,
				m_TargetDuration = 30,
				m_MaximumDuration = 60,
				m_Priority = 1,
				m_TargetDurationMultiplier = 1f
			};
			result.Phases.Add(phaseData);

			ushort phaseBit = (ushort)(1 << phaseIndex);

			
			EdgeGroupMask edgeMask = new(edgeEntity, edgePosition);

			
			edgeMask.m_Car.m_Left.m_GoGroupMask = phaseBit;
			edgeMask.m_Car.m_Straight.m_GoGroupMask = phaseBit;
			edgeMask.m_Car.m_Right.m_GoGroupMask = phaseBit;
			edgeMask.m_Car.m_UTurn.m_GoGroupMask = phaseBit;

			edgeMask.m_PublicCar.m_Left.m_GoGroupMask = phaseBit;
			edgeMask.m_PublicCar.m_Straight.m_GoGroupMask = phaseBit;
			edgeMask.m_PublicCar.m_Right.m_GoGroupMask = phaseBit;
			edgeMask.m_PublicCar.m_UTurn.m_GoGroupMask = phaseBit;

			edgeMask.m_Track.m_Left.m_GoGroupMask = phaseBit;
			edgeMask.m_Track.m_Straight.m_GoGroupMask = phaseBit;
			edgeMask.m_Track.m_Right.m_GoGroupMask = phaseBit;

			edgeMask.m_Pedestrian.m_GoGroupMask = phaseBit;
			edgeMask.m_Bicycle.m_GoGroupMask = phaseBit;

			result.EdgeGroupMasks.Add(edgeMask);
			phaseIndex++;
		}

		result.PhaseCount = phaseIndex;

		
		foreach (var kvp in carriageways)
		{
			kvp.Value.Dispose();
		}
		carriageways.Dispose();

		result.Success = result.PhaseCount > 0;
		return result;
	}

	
	
	
	public static PhaseGenerationResult GenerateProtectedTurnPhases(
		Allocator allocator,
		Entity nodeEntity,
		NativeList<ComputedLaneConnection> laneConnections,
		DynamicBuffer<ConnectedEdge> connectedEdges,
		ComponentLookup<Edge> edgeLookup,
		ComponentLookup<EdgeGeometry> edgeGeometryLookup,
		bool leftHandTraffic)
	{
		PhaseGenerationResult result = new()
		{
			Phases = new NativeList<CustomPhaseData>(8, allocator),
			EdgeGroupMasks = new NativeList<EdgeGroupMask>(8, allocator),
			PhaseCount = 0,
			Success = false
		};

		if (connectedEdges.Length != 4)
		{
			
			return GenerateIntelligentPhases(allocator, nodeEntity, laneConnections, connectedEdges, edgeLookup, edgeGeometryLookup, leftHandTraffic);
		}

		
		NativeList<EdgePositionInfo> sortedEdges = SortEdgesByPosition(nodeEntity, connectedEdges, edgeLookup, edgeGeometryLookup, Allocator.Temp);

		
		
		
		

		
		for (int i = 0; i < 4; i++)
		{
			CustomPhaseData phaseData = new()
			{
				m_MinimumDuration = (ushort)(i % 2 == 0 ? 10 : 8),
				m_TargetDuration = (ushort)(i % 2 == 0 ? 30 : 15),
				m_MaximumDuration = (ushort)(i % 2 == 0 ? 60 : 30),
				m_Priority = 1,
				m_TargetDurationMultiplier = 1f
			};
			result.Phases.Add(phaseData);
		}

		
		for (int i = 0; i < sortedEdges.Length && i < 4; i++)
		{
			Entity edgeEntity = sortedEdges[i].Edge;
			float3 edgePosition = sortedEdges[i].Position;

			EdgeGroupMask edgeMask = new(edgeEntity, edgePosition);

			bool isNorthSouth = (i == 0 || i == 2);
			ushort straightPhase = isNorthSouth ? (ushort)1 : (ushort)4;
			ushort turnPhase = isNorthSouth ? (ushort)2 : (ushort)8;

			
			edgeMask.m_Car.m_Straight.m_GoGroupMask = straightPhase;
			edgeMask.m_PublicCar.m_Straight.m_GoGroupMask = straightPhase;
			edgeMask.m_Track.m_Straight.m_GoGroupMask = straightPhase;

			
			
			if (leftHandTraffic)
			{
				edgeMask.m_Car.m_Left.m_GoGroupMask = straightPhase;
				edgeMask.m_PublicCar.m_Left.m_GoGroupMask = straightPhase;
				edgeMask.m_Car.m_Right.m_GoGroupMask = turnPhase;
				edgeMask.m_PublicCar.m_Right.m_GoGroupMask = turnPhase;
			}
			else
			{
				edgeMask.m_Car.m_Right.m_GoGroupMask = straightPhase;
				edgeMask.m_PublicCar.m_Right.m_GoGroupMask = straightPhase;
				edgeMask.m_Car.m_Left.m_GoGroupMask = turnPhase;
				edgeMask.m_PublicCar.m_Left.m_GoGroupMask = turnPhase;
			}

			
			edgeMask.m_Pedestrian.m_GoGroupMask = straightPhase;
			edgeMask.m_Bicycle.m_GoGroupMask = straightPhase;

			result.EdgeGroupMasks.Add(edgeMask);
		}

		result.PhaseCount = 4;
		result.Success = true;

		sortedEdges.Dispose();
		return result;
	}

	private struct EdgePositionInfo
	{
		public Entity Edge;
		public float3 Position;
		public float Angle;
	}

	private static NativeList<EdgePositionInfo> SortEdgesByPosition(
		Entity nodeEntity,
		DynamicBuffer<ConnectedEdge> connectedEdges,
		ComponentLookup<Edge> edgeLookup,
		ComponentLookup<EdgeGeometry> edgeGeometryLookup,
		Allocator allocator)
	{
		NativeList<EdgePositionInfo> edges = new(connectedEdges.Length, allocator);

		float3 nodeCenter = float3.zero;
		int edgeCount = 0;

		
		foreach (ConnectedEdge connectedEdge in connectedEdges)
		{
			float3 edgePos = NodeUtils.GetEdgePosition(nodeEntity, connectedEdge.m_Edge, edgeLookup, edgeGeometryLookup);
			nodeCenter += edgePos;
			edgeCount++;
		}
		if (edgeCount > 0)
		{
			nodeCenter /= edgeCount;
		}

		
		foreach (ConnectedEdge connectedEdge in connectedEdges)
		{
			float3 edgePos = NodeUtils.GetEdgePosition(nodeEntity, connectedEdge.m_Edge, edgeLookup, edgeGeometryLookup);
			float3 dir = edgePos - nodeCenter;
			float angle = math.atan2(dir.x, dir.z);

			edges.Add(new EdgePositionInfo
			{
				Edge = connectedEdge.m_Edge,
				Position = edgePos,
				Angle = angle
			});
		}

		
		for (int i = 0; i < edges.Length - 1; i++)
		{
			for (int j = i + 1; j < edges.Length; j++)
			{
				if (edges[j].Angle < edges[i].Angle)
				{
					var temp = edges[i];
					edges[i] = edges[j];
					edges[j] = temp;
				}
			}
		}

		return edges;
	}

	private static int FindEdgeGroupMaskIndex(NativeList<EdgeGroupMask> masks, Entity edge)
	{
		for (int i = 0; i < masks.Length; i++)
		{
			if (masks[i].m_Edge == edge)
			{
				return i;
			}
		}
		return -1;
	}

	private static void SetSignalMasks(ref EdgeGroupMask edgeMask, ComputedLaneConnection conn, ushort phaseBit, bool leftHandTraffic)
	{
		bool isPublicOnly = conn.m_IsPublicOnly;
		bool isUnsafe = conn.m_IsUnsafe;

		ref GroupMask.Turn carTurn = ref (isPublicOnly ? ref edgeMask.m_PublicCar : ref edgeMask.m_Car);

		switch (conn.m_TurnType)
		{
			case TurnType.Left:
			case TurnType.GentleLeft:
				carTurn.m_Left.m_GoGroupMask |= phaseBit;
				if (isUnsafe)
				{
					carTurn.m_Left.m_YieldGroupMask |= phaseBit;
				}
				break;

			case TurnType.Right:
			case TurnType.GentleRight:
				carTurn.m_Right.m_GoGroupMask |= phaseBit;
				if (isUnsafe)
				{
					carTurn.m_Right.m_YieldGroupMask |= phaseBit;
				}
				break;

			case TurnType.UTurn:
				carTurn.m_UTurn.m_GoGroupMask |= phaseBit;
				if (isUnsafe)
				{
					carTurn.m_UTurn.m_YieldGroupMask |= phaseBit;
				}
				break;

			case TurnType.Straight:
			default:
				carTurn.m_Straight.m_GoGroupMask |= phaseBit;
				if (isUnsafe)
				{
					carTurn.m_Straight.m_YieldGroupMask |= phaseBit;
				}
				break;
		}

		
		if ((conn.m_VehicleGroup & VehicleGroup.TrackGroup) != 0)
		{
			switch (conn.m_TurnType)
			{
				case TurnType.Left:
				case TurnType.GentleLeft:
					edgeMask.m_Track.m_Left.m_GoGroupMask |= phaseBit;
					break;
				case TurnType.Right:
				case TurnType.GentleRight:
					edgeMask.m_Track.m_Right.m_GoGroupMask |= phaseBit;
					break;
				default:
					edgeMask.m_Track.m_Straight.m_GoGroupMask |= phaseBit;
					break;
			}
		}

		
		if ((conn.m_VehicleGroup & VehicleGroup.Pedestrian) != 0)
		{
			edgeMask.m_Pedestrian.m_GoGroupMask |= phaseBit;
		}

		
		if ((conn.m_VehicleGroup & VehicleGroup.Bike) != 0)
		{
			edgeMask.m_Bicycle.m_GoGroupMask |= phaseBit;
		}
	}

	private static void EnsureAllEdgesHaveMasks(
		ref PhaseGenerationResult result,
		Entity nodeEntity,
		DynamicBuffer<ConnectedEdge> connectedEdges,
		ComponentLookup<Edge> edgeLookup,
		ComponentLookup<EdgeGeometry> edgeGeometryLookup)
	{
		foreach (ConnectedEdge connectedEdge in connectedEdges)
		{
			Entity edgeEntity = connectedEdge.m_Edge;
			bool found = false;

			for (int i = 0; i < result.EdgeGroupMasks.Length; i++)
			{
				if (result.EdgeGroupMasks[i].m_Edge == edgeEntity)
				{
					found = true;
					break;
				}
			}

			if (!found)
			{
				float3 edgePosition = NodeUtils.GetEdgePosition(nodeEntity, edgeEntity, edgeLookup, edgeGeometryLookup);
				result.EdgeGroupMasks.Add(new EdgeGroupMask(edgeEntity, edgePosition));
			}
		}
	}
}
