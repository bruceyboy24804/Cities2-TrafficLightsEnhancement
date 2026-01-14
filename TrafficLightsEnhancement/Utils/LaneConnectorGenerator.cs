using C2VM.TrafficLightsEnhancement.Components;
using Colossal.Mathematics;
using Game.Net;
using Game.Pathfind;
using Game.Prefabs;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using CarLane = Game.Net.CarLane;
using EdgeLane = Game.Net.EdgeLane;
using MasterLane = Game.Net.MasterLane;
using PedestrianLane = Game.Net.PedestrianLane;
using SecondaryLane = Game.Net.SecondaryLane;
using SlaveLane = Game.Net.SlaveLane;
using SubLane = Game.Net.SubLane;
using TrackLane = Game.Net.TrackLane;

namespace C2VM.TrafficLightsEnhancement.Utils;





public struct LaneConnectorGenerator
{
	
	
	
	public struct ConnectPosition
	{
		public Entity edge;
		public Entity subLane;
		public int laneIndex;
		public int2 carriagewayAndGroupIndex;
		public float3 position;
		public float3 direction;
		public VehicleGroup vehicleGroup;
		public ConnectorType connectorType;
		public bool isPublicOnly;
		public bool isTwoWay;
		public bool isHighway;
	}

	
	
	
	public static NativeList<ComputedLaneConnection> GenerateLaneConnections(
		Allocator allocator,
		Entity nodeEntity,
		DynamicBuffer<Game.Net.SubLane> nodeSubLaneBuffer,
		DynamicBuffer<ConnectedEdge> connectedEdgeBuffer,
		BufferLookup<Game.Net.SubLane> subLaneLookup,
		ComponentLookup<Game.Net.Edge> edgeLookup,
		ComponentLookup<EdgeGeometry> edgeGeometryLookup,
		ComponentLookup<Lane> laneLookup,
		ComponentLookup<Curve> curveLookup,
		ComponentLookup<Game.Net.CarLane> carLaneLookup,
		ComponentLookup<TrackLane> trackLaneLookup,
		ComponentLookup<PedestrianLane> pedestrianLaneLookup,
		ComponentLookup<SecondaryLane> secondaryLaneLookup,
		ComponentLookup<MasterLane> masterLaneLookup,
		ComponentLookup<EdgeLane> edgeLaneLookup,
		ComponentLookup<SlaveLane> slaveLaneLookup,
		ComponentLookup<Composition> compositionLookup,
		ComponentLookup<PrefabRef> prefabRefLookup,
		ComponentLookup<NetLaneData> netLaneDataLookup,
		ComponentLookup<CarLaneData> carLaneDataLookup,
		ComponentLookup<TrackLaneData> trackLaneDataLookup,
		BufferLookup<NetCompositionLane> compositionLanesLookup,
		ComponentLookup<NetCompositionData> compositionDataLookup)
	{
		NativeList<ComputedLaneConnection> connections = new(32, allocator);
		NativeList<ConnectPosition> sourcePositions = new(16, Allocator.Temp);
		NativeList<ConnectPosition> targetPositions = new(16, Allocator.Temp);

		
		foreach (ConnectedEdge connectedEdge in connectedEdgeBuffer)
		{
			Entity edgeEntity = connectedEdge.m_Edge;
			if (!edgeLookup.TryGetComponent(edgeEntity, out Game.Net.Edge edge))
			{
				continue;
			}

			bool isEnd = edge.m_End == nodeEntity;
			CollectEdgeConnectPositions(
				nodeEntity,
				edgeEntity,
				isEnd,
				sourcePositions,
				targetPositions,
				subLaneLookup,
				edgeLookup,
				edgeGeometryLookup,
				laneLookup,
				curveLookup,
				carLaneLookup,
				trackLaneLookup,
				secondaryLaneLookup,
				masterLaneLookup,
				edgeLaneLookup,
				slaveLaneLookup,
				compositionLookup,
				prefabRefLookup,
				netLaneDataLookup,
				carLaneDataLookup,
				trackLaneDataLookup,
				compositionLanesLookup,
				compositionDataLookup);
		}

		
		foreach (Game.Net.SubLane nodeSubLane in nodeSubLaneBuffer)
		{
			Entity nodeSubLaneEntity = nodeSubLane.m_SubLane;

			if (masterLaneLookup.HasComponent(nodeSubLaneEntity))
			{
				continue;
			}

			if (!laneLookup.TryGetComponent(nodeSubLaneEntity, out Lane nodeLane))
			{
				continue;
			}

			curveLookup.TryGetComponent(nodeSubLaneEntity, out Curve nodeCurve);

			
			ConnectPosition? sourcePos = null;
			ConnectPosition? targetPos = null;

			foreach (var pos in sourcePositions)
			{
				if (MatchesLaneNode(nodeLane.m_StartNode, pos, laneLookup, subLaneLookup))
				{
					sourcePos = pos;
					break;
				}
			}

			foreach (var pos in targetPositions)
			{
				if (MatchesLaneNode(nodeLane.m_EndNode, pos, laneLookup, subLaneLookup))
				{
					targetPos = pos;
					break;
				}
			}

			if (!sourcePos.HasValue)
			{
				continue;
			}

			
			TurnType turnType = TurnType.Straight;
			VehicleGroup vehicleGroup = sourcePos.Value.vehicleGroup;
			bool isPublicOnly = sourcePos.Value.isPublicOnly;
			bool isUnsafe = false;

			if (carLaneLookup.TryGetComponent(nodeSubLaneEntity, out CarLane nodeCarLane))
			{
				turnType = GetTurnTypeFromCarLane(nodeCarLane);
				isUnsafe = (nodeCarLane.m_Flags & CarLaneFlags.Unsafe) != 0;
			}
			else if (trackLaneLookup.TryGetComponent(nodeSubLaneEntity, out TrackLane nodeTrackLane))
			{
				turnType = GetTurnTypeFromTrackLane(nodeTrackLane);
			}
			else if (pedestrianLaneLookup.TryGetComponent(nodeSubLaneEntity, out PedestrianLane pedLane))
			{
				if ((pedLane.m_Flags & PedestrianLaneFlags.Crosswalk) != 0)
				{
					vehicleGroup = VehicleGroup.Pedestrian;
					isUnsafe = (pedLane.m_Flags & PedestrianLaneFlags.Unsafe) != 0;
				}
			}

			ComputedLaneConnection connection = new()
			{
				m_SourceEdge = sourcePos.Value.edge,
				m_TargetEdge = targetPos?.edge ?? Entity.Null,
				m_SourceSubLane = sourcePos.Value.subLane,
				m_TargetSubLane = targetPos?.subLane ?? Entity.Null,
				m_NodeSubLane = nodeSubLaneEntity,
				m_SourceLaneIndex = sourcePos.Value.laneIndex,
				m_TargetLaneIndex = targetPos?.laneIndex ?? -1,
				m_CarriagewayAndGroupIndexMap = new int4(
					sourcePos.Value.carriagewayAndGroupIndex.x,
					sourcePos.Value.carriagewayAndGroupIndex.y,
					targetPos?.carriagewayAndGroupIndex.x ?? -1,
					targetPos?.carriagewayAndGroupIndex.y ?? -1),
				m_LanePositionMap = new float3x2(sourcePos.Value.position, targetPos?.position ?? float3.zero),
				m_SourceDirection = sourcePos.Value.direction,
				m_TargetDirection = targetPos?.direction ?? float3.zero,
				m_TurnType = turnType,
				m_VehicleGroup = vehicleGroup,
				m_IsPublicOnly = isPublicOnly,
				m_IsUnsafe = isUnsafe,
				m_IsTwoWay = sourcePos.Value.isTwoWay
			};

			connections.Add(connection);
		}

		sourcePositions.Dispose();
		targetPositions.Dispose();

		return connections;
	}

	private static void CollectEdgeConnectPositions(
		Entity nodeEntity,
		Entity edgeEntity,
		bool isEnd,
		NativeList<ConnectPosition> sourcePositions,
		NativeList<ConnectPosition> targetPositions,
		BufferLookup<Game.Net.SubLane> subLaneLookup,
		ComponentLookup<Game.Net.Edge> edgeLookup,
		ComponentLookup<EdgeGeometry> edgeGeometryLookup,
		ComponentLookup<Lane> laneLookup,
		ComponentLookup<Curve> curveLookup,
		ComponentLookup<Game.Net.CarLane> carLaneLookup,
		ComponentLookup<TrackLane> trackLaneLookup,
		ComponentLookup<SecondaryLane> secondaryLaneLookup,
		ComponentLookup<MasterLane> masterLaneLookup,
		ComponentLookup<EdgeLane> edgeLaneLookup,
		ComponentLookup<SlaveLane> slaveLaneLookup,
		ComponentLookup<Composition> compositionLookup,
		ComponentLookup<PrefabRef> prefabRefLookup,
		ComponentLookup<NetLaneData> netLaneDataLookup,
		ComponentLookup<CarLaneData> carLaneDataLookup,
		ComponentLookup<TrackLaneData> trackLaneDataLookup,
		BufferLookup<NetCompositionLane> compositionLanesLookup,
		ComponentLookup<NetCompositionData> compositionDataLookup)
	{
		if (!subLaneLookup.TryGetBuffer(edgeEntity, out DynamicBuffer<Game.Net.SubLane> edgeSubLanes))
		{
			return;
		}

		if (!compositionLookup.TryGetComponent(edgeEntity, out Composition composition))
		{
			return;
		}

		if (!compositionDataLookup.TryGetComponent(composition.m_Edge, out NetCompositionData compositionData))
		{
			return;
		}

		if (!compositionLanesLookup.TryGetBuffer(composition.m_Edge, out DynamicBuffer<NetCompositionLane> compositionLanes))
		{
			return;
		}

		edgeGeometryLookup.TryGetComponent(edgeEntity, out EdgeGeometry edgeGeometry);

		float rhs = math.select(0f, 1f, isEnd);

		foreach (Game.Net.SubLane edgeSubLane in edgeSubLanes)
		{
			Entity subLaneEntity = edgeSubLane.m_SubLane;

			if (!edgeLaneLookup.TryGetComponent(subLaneEntity, out EdgeLane edgeLane))
			{
				continue;
			}

			if (secondaryLaneLookup.HasComponent(subLaneEntity))
			{
				continue;
			}

			bool2 x = edgeLane.m_EdgeDelta == rhs;
			if (!math.any(x))
			{
				continue;
			}

			bool y = x.y;

			if (!curveLookup.TryGetComponent(subLaneEntity, out Curve curve))
			{
				continue;
			}

			Bezier4x3 bezier = curve.m_Bezier;
			if (y)
			{
				bezier = MathUtils.Invert(bezier);
			}

			if (!prefabRefLookup.TryGetComponent(subLaneEntity, out PrefabRef prefabRef))
			{
				continue;
			}

			if (!netLaneDataLookup.TryGetComponent(prefabRef.m_Prefab, out NetLaneData netLaneData))
			{
				continue;
			}

			
			if ((netLaneData.m_Flags & (LaneFlags.Utility | LaneFlags.Parking | LaneFlags.ParkingLeft | LaneFlags.ParkingRight)) != 0)
			{
				continue;
			}

			
			int compositionLaneIndex = FindCompositionLaneIndex(
				subLaneEntity, isEnd, compositionLanes, compositionData,
				netLaneData, edgeGeometry, bezier,
				slaveLaneLookup, masterLaneLookup,
				trackLaneDataLookup, prefabRef);

			if (compositionLaneIndex < 0 || compositionLaneIndex >= compositionLanes.Length)
			{
				continue;
			}

			NetCompositionLane compositionLane = compositionLanes[compositionLaneIndex];
			compositionLane.m_Position.x = math.select(-compositionLane.m_Position.x, compositionLane.m_Position.x, isEnd);

			
			float3 tangent = MathUtils.StartTangent(bezier);
			tangent = -MathUtils.Normalize(tangent, tangent.xz);
			tangent.y = math.clamp(tangent.y, -1f, 1f);

			
			VehicleGroup vehicleGroup = GetVehicleGroup(netLaneData, prefabRef, carLaneDataLookup, trackLaneDataLookup);
			bool isPublicOnly = false;
			bool isHighway = false;

			if (carLaneLookup.TryGetComponent(subLaneEntity, out Game.Net.CarLane carLane))
			{
				isPublicOnly = (carLane.m_Flags & CarLaneFlags.PublicOnly) != 0;
				isHighway = (carLane.m_Flags & CarLaneFlags.Highway) != 0;
			}

			bool isTwoWay = (netLaneData.m_Flags & LaneFlags.Twoway) != 0;

			laneLookup.TryGetComponent(subLaneEntity, out Lane lane);
			int laneIndex = y ? (lane.m_EndNode.GetLaneIndex() & 0xFF) : (lane.m_StartNode.GetLaneIndex() & 0xFF);

			ConnectPosition connectPosition = new()
			{
				edge = edgeEntity,
				subLane = subLaneEntity,
				laneIndex = laneIndex,
				carriagewayAndGroupIndex = new int2(compositionLane.m_Carriageway, compositionLane.m_Group),
				position = bezier.a,
				direction = tangent,
				vehicleGroup = vehicleGroup | (isHighway ? VehicleGroup.Highway : VehicleGroup.None),
				connectorType = isTwoWay ? ConnectorType.TwoWay : (y ? ConnectorType.Source : ConnectorType.Target),
				isPublicOnly = isPublicOnly,
				isTwoWay = isTwoWay,
				isHighway = isHighway
			};

			if (isTwoWay)
			{
				sourcePositions.Add(connectPosition);
				targetPositions.Add(connectPosition);
			}
			else if (y)
			{
				sourcePositions.Add(connectPosition);
			}
			else
			{
				targetPositions.Add(connectPosition);
			}
		}
	}

	private static int FindCompositionLaneIndex(
		Entity subLaneEntity,
		bool isEnd,
		DynamicBuffer<NetCompositionLane> compositionLanes,
		NetCompositionData compositionData,
		NetLaneData netLaneData,
		EdgeGeometry edgeGeometry,
		Bezier4x3 bezier,
		ComponentLookup<SlaveLane> slaveLaneLookup,
		ComponentLookup<MasterLane> masterLaneLookup,
		ComponentLookup<TrackLaneData> trackLaneDataLookup,
		PrefabRef prefabRef)
	{
		int bestIndex = -1;
		float bestDistance = float.MaxValue;

		LaneFlags laneFlags3 = netLaneData.m_Flags & (LaneFlags.Road | LaneFlags.Track | LaneFlags.Underground);
		LaneFlags laneFlags4 = LaneFlags.Invert | LaneFlags.Slave | LaneFlags.Road | LaneFlags.Track | LaneFlags.Underground;

		if (slaveLaneLookup.HasComponent(subLaneEntity))
		{
			laneFlags3 |= LaneFlags.Slave;
		}

		if (masterLaneLookup.HasComponent(subLaneEntity))
		{
			laneFlags3 |= LaneFlags.Master;
			laneFlags3 &= ~LaneFlags.Track;
			laneFlags4 &= ~LaneFlags.Track;
		}

		Bezier4x3 startGeometry = isEnd
			? new Bezier4x3(
				MathUtils.Invert(edgeGeometry.m_End.m_Right).a,
				MathUtils.Invert(edgeGeometry.m_End.m_Right).a,
				MathUtils.Invert(edgeGeometry.m_End.m_Left).a,
				MathUtils.Invert(edgeGeometry.m_End.m_Left).a)
			: new Bezier4x3(
				edgeGeometry.m_Start.m_Right.a,
				edgeGeometry.m_Start.m_Right.a,
				edgeGeometry.m_Start.m_Left.a,
				edgeGeometry.m_Start.m_Left.a);

		for (int i = 0; i < compositionLanes.Length; i++)
		{
			NetCompositionLane compositionLane = compositionLanes[i];

			if ((compositionLane.m_Flags & laneFlags4) != laneFlags3)
			{
				continue;
			}

			float adjustedPosX = math.select(-compositionLane.m_Position.x, compositionLane.m_Position.x, isEnd);
			float normalizedPos = adjustedPosX / math.max(1f, compositionData.m_Width) + 0.5f;

			if (MathUtils.Intersect(
				new Line2(startGeometry.a.xz, startGeometry.d.xz),
				new Line2(bezier.a.xz, bezier.b.xz),
				out float2 t))
			{
				float distance = math.abs(normalizedPos - t.x);
				if (distance < bestDistance)
				{
					bestIndex = i;
					bestDistance = distance;
				}
			}
		}

		return bestIndex;
	}

	private static VehicleGroup GetVehicleGroup(
		NetLaneData netLaneData,
		PrefabRef prefabRef,
		ComponentLookup<CarLaneData> carLaneDataLookup,
		ComponentLookup<TrackLaneData> trackLaneDataLookup)
	{
		VehicleGroup group = VehicleGroup.None;

		if ((netLaneData.m_Flags & LaneFlags.Road) != 0)
		{
			if (carLaneDataLookup.TryGetComponent(prefabRef.m_Prefab, out CarLaneData carLaneData))
			{
				if ((carLaneData.m_RoadTypes & RoadTypes.Car) != 0)
				{
					group |= VehicleGroup.Car;
				}
				if ((carLaneData.m_RoadTypes & RoadTypes.Bicycle) != 0)
				{
					group |= VehicleGroup.Bike;
				}
			}
			else
			{
				group |= VehicleGroup.Car;
			}
		}

		if ((netLaneData.m_Flags & LaneFlags.Track) != 0)
		{
			if (trackLaneDataLookup.TryGetComponent(prefabRef.m_Prefab, out TrackLaneData trackLaneData))
			{
				if ((trackLaneData.m_TrackTypes & TrackTypes.Train) != 0)
				{
					group |= VehicleGroup.Train;
				}
				if ((trackLaneData.m_TrackTypes & TrackTypes.Tram) != 0)
				{
					group |= VehicleGroup.Tram;
				}
				if ((trackLaneData.m_TrackTypes & TrackTypes.Subway) != 0)
				{
					group |= VehicleGroup.Subway;
				}
			}
		}

		if ((netLaneData.m_Flags & LaneFlags.Pedestrian) != 0)
		{
			group |= VehicleGroup.Pedestrian;
		}

		return group;
	}

	private static bool MatchesLaneNode(
		Game.Pathfind.PathNode pathNode,
		ConnectPosition connectPosition,
		ComponentLookup<Lane> laneLookup,
		BufferLookup<Game.Net.SubLane> subLaneLookup)
	{
		if (!subLaneLookup.TryGetBuffer(connectPosition.edge, out DynamicBuffer<Game.Net.SubLane> edgeSubLanes))
		{
			return false;
		}

		foreach (Game.Net.SubLane edgeSubLane in edgeSubLanes)
		{
			if (edgeSubLane.m_SubLane != connectPosition.subLane)
			{
				continue;
			}

			if (laneLookup.TryGetComponent(edgeSubLane.m_SubLane, out Lane edgeLane))
			{
				if (pathNode.Equals(edgeLane.m_EndNode) || pathNode.Equals(edgeLane.m_StartNode))
				{
					return true;
				}
			}
		}

		return false;
	}

	public static TurnType GetTurnTypeFromCarLane(Game.Net.CarLane carLane)
	{
		if ((carLane.m_Flags & (CarLaneFlags.UTurnLeft | CarLaneFlags.UTurnRight)) != 0)
		{
			return TurnType.UTurn;
		}
		if ((carLane.m_Flags & CarLaneFlags.TurnLeft) != 0)
		{
			return TurnType.Left;
		}
		if ((carLane.m_Flags & CarLaneFlags.GentleTurnLeft) != 0)
		{
			return TurnType.GentleLeft;
		}
		if ((carLane.m_Flags & CarLaneFlags.TurnRight) != 0)
		{
			return TurnType.Right;
		}
		if ((carLane.m_Flags & CarLaneFlags.GentleTurnRight) != 0)
		{
			return TurnType.GentleRight;
		}
		return TurnType.Straight;
	}

	public static TurnType GetTurnTypeFromTrackLane(TrackLane trackLane)
	{
		if ((trackLane.m_Flags & TrackLaneFlags.TurnLeft) != 0)
		{
			return TurnType.Left;
		}
		if ((trackLane.m_Flags & TrackLaneFlags.TurnRight) != 0)
		{
			return TurnType.Right;
		}
		return TurnType.Straight;
	}

	
	
	
	
	public static NativeHashMap<int2, NativeList<int>> GroupConnectionsByCarriageway(
		NativeList<ComputedLaneConnection> connections,
		Allocator allocator)
	{
		NativeHashMap<int2, NativeList<int>> groups = new(8, allocator);

		for (int i = 0; i < connections.Length; i++)
		{
			var connection = connections[i];
			int2 key = new(connection.m_CarriagewayAndGroupIndexMap.x, connection.m_CarriagewayAndGroupIndexMap.y);

			if (!groups.TryGetValue(key, out NativeList<int> indices))
			{
				indices = new NativeList<int>(8, allocator);
				groups[key] = indices;
			}
			indices.Add(i);
		}

		return groups;
	}

	
	
	
	public static NativeList<NativeList<int>> FindNonConflictingGroups(
		NativeList<ComputedLaneConnection> connections,
		Allocator allocator)
	{
		NativeList<NativeList<int>> groups = new(4, allocator);

		NativeArray<bool> assigned = new(connections.Length, Allocator.Temp);
		for (int i = 0; i < assigned.Length; i++)
		{
			assigned[i] = false;
		}

		while (true)
		{
			NativeList<int> currentGroup = new(8, allocator);

			for (int i = 0; i < connections.Length; i++)
			{
				if (assigned[i])
				{
					continue;
				}

				bool hasConflict = false;
				for (int j = 0; j < currentGroup.Length; j++)
				{
					if (connections[i].ConflictsWith(connections[currentGroup[j]]))
					{
						hasConflict = true;
						break;
					}
				}

				if (!hasConflict)
				{
					currentGroup.Add(i);
					assigned[i] = true;
				}
			}

			if (currentGroup.Length == 0)
			{
				currentGroup.Dispose();
				break;
			}

			groups.Add(currentGroup);
		}

		assigned.Dispose();
		return groups;
	}
}
