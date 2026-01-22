import { Entity } from "cs2/utils";

export interface MainPanel {
  title: string,
  image: string,
  position: ScreenPoint,
  showPanel: boolean,
  showFloatingButton: boolean,
  state: number,
  selectedEntity: { index: number, version: number },
  items: MainPanelItem[]
}

export type MainPanelItem = MainPanelItemTitle | MainPanelItemMessage | MainPanelItemDivider | MainPanelItemRadio | MainPanelItemCheckbox | MainPanelItemButton | MainPanelItemNotification | MainPanelItemRange | MainPanelItemCustomPhaseHeader | MainPanelItemCustomPhase | MainPanelItemTrafficGroup;

export interface MainPanelItemTitle {
  itemType: "title",
  title: string,
  secondaryText?: string
}

export interface MainPanelItemMessage {
  itemType: "message",
  message: string
}

export interface MainPanelItemDivider {
  itemType: "divider"
}

export interface MainPanelItemRadio {
  itemType: "radio",
  type: string,
  isChecked: boolean,
  key: string,
  value: string,
  label: string,
  engineEventName: string
}

export interface MainPanelItemCheckbox {
  itemType: "checkbox",
  type: string,
  isChecked: boolean,
  key: string,
  value: string,
  label: string,
  engineEventName: string
}

export interface MainPanelItemButton {
  itemType: "button",
  type: "button",
  key: string,
  value: string,
  label: string,
  engineEventName: string
}

export interface MainPanelItemNotification {
  itemType: "notification",
  type: "notification",
  label: string,
  notificationType: "warning" | "notice",
  key?: string,
  value?: string,
  engineEventName?: string
}

export interface MainPanelItemRange {
  itemType: "range",
  key: string,
  label: string,
  value: number,
  valuePrefix: string,
  valueSuffix: string,
  min: number,
  max: number,
  step: number,
  defaultValue: number,
  enableTextField?: boolean,
  textFieldRegExp?: string,
  engineEventName: string,
  tooltip?: string,
}

export interface MainPanelItemCustomPhaseHeader {
  itemType: "customPhaseHeader",
  trafficLightMode: number,
  phaseCount: number,
}

export interface MainPanelItemCustomPhase {
  itemType: "customPhase",
  activeIndex: number,
  activeViewingIndex: number,
  currentSignalGroup: number,
  manualSignalGroup: number,
  index: number,
  length: number,
  timer: number,
  turnsSinceLastRun: number,
  lowFlowTimer: number,
  carFlow: number,
  carLaneOccupied: number,
  publicCarLaneOccupied: number,
  trackLaneOccupied: number,
  pedestrianLaneOccupied: number,
  bicycleLaneOccupied: number,
  weightedWaiting: number,
  targetDuration: number,
  priority: number,
  minimumDuration: number,
  maximumDuration: number,
  targetDurationMultiplier: number,
  intervalExponent: number,
  linkedWithNextPhase: boolean,
  endPhasePrematurely: boolean,
  
  changeMetric: number,
  waitFlowBalance: number,
  
  trafficLightMode: number,
  smartPhaseSelection: boolean,
  
  carActive: boolean;
  publicCarActive: boolean;
  trackActive: boolean;
  pedestrianActive: boolean;
  bicycleActive: boolean;
  
  hasSignalDelays?: boolean,
  carOpenDelay?: number,
  carCloseDelay?: number,
  publicCarOpenDelay?: number,
  publicCarCloseDelay?: number,
  trackOpenDelay?: number,
  trackCloseDelay?: number,
  pedestrianOpenDelay?: number,
  pedestrianCloseDelay?: number,
  bicycleOpenDelay?: number,
  bicycleCloseDelay?: number,
  
  carWeight: number,
  publicCarWeight: number,
  trackWeight: number,
  pedestrianWeight: number,
  bicycleWeight: number,
  smoothingFactor: number,
  
  flowRatio: number,
  waitRatio: number,
}

export interface WorldPosition {
  x: number,
  y: number,
  z: number,
  key: string
}

export interface ScreenPoint {
  left: number,
  top: number
}

export interface ScreenPointMap {
  [key: string]: ScreenPoint
}

export interface CityConfiguration {
  leftHandTraffic: boolean
}

export interface CustomPhaseLane {
  type: CustomPhaseLaneType,
  left: CustomPhaseSignalState,
  straight: CustomPhaseSignalState,
  right: CustomPhaseSignalState,
  uTurn: CustomPhaseSignalState,
  all: CustomPhaseSignalState,
  leftDelay?: SignalDelay,
  straightDelay?: SignalDelay,
  rightDelay?: SignalDelay,
  uTurnDelay?: SignalDelay,
  allDelay?: SignalDelay
}

export type CustomPhaseLaneType = "carLane" | "publicCarLane" | "trackLane" | "bicycleLane" | "pedestrianLaneStopLine" | "pedestrianLaneNonStopLine";

export type CustomPhaseLaneDirection = "left" | "straight" | "right" | "uTurn" | "all";

export type CustomPhaseSignalState = "stop" | "go" | "yield" | "none";

export interface SignalDelay {
  openDelay: number;
  closeDelay: number;
}

export interface GroupMaskSignal {
  m_GoGroupMask: number,
  m_YieldGroupMask: number,
  m_OpenDelay?: number,
  m_CloseDelay?: number
}

export interface GroupMaskTurn {
  m_Left: GroupMaskSignal,
  m_Straight: GroupMaskSignal,
  m_Right: GroupMaskSignal,
  m_UTurn: GroupMaskSignal
}

export interface EdgeGroupMask {
  m_Edge: Entity,
  m_Position: WorldPosition,
  m_Options: number,
  m_Car: GroupMaskTurn,
  m_PublicCar: GroupMaskTurn,
  m_Track: GroupMaskTurn,
  m_Pedestrian: GroupMaskSignal,
  m_Bicycle: GroupMaskSignal
}

export interface EdgeInfo {
  m_Node: Entity,
  m_Edge: Entity,
  m_Position: WorldPosition,
  m_CarLaneLeftCount: number,
  m_CarLaneStraightCount: number,
  m_CarLaneRightCount: number,
  m_CarLaneUTurnCount: number,
  m_PublicCarLaneLeftCount: number,
  m_PublicCarLaneStraightCount: number,
  m_PublicCarLaneRightCount: number,
  m_PublicCarLaneUTurnCount: number,
  m_TrackLaneLeftCount: number,
  m_TrackLaneStraightCount: number,
  m_TrackLaneRightCount: number,
  m_TrainTrackCount: number,
  m_PedestrianLaneStopLineCount: number,
  m_PedestrianLaneNonStopLineCount: number,
  m_BicycleLaneCount: number,
  m_SubLaneInfoList: SubLaneInfo[],
  m_EdgeGroupMask: EdgeGroupMask,
  m_OpenDelay?: number,
  m_CloseDelay?: number
}

export interface SubLaneGroupMask {
  m_SubLane: Entity,
  m_Position: WorldPosition,
  m_Options: number,
  m_Car: GroupMaskTurn,
  m_Track: GroupMaskTurn,
  m_Pedestrian: GroupMaskSignal
}

export interface SubLaneInfo {
  m_SubLane: Entity,
  m_Position: WorldPosition,
  m_CarLaneLeftCount: number,
  m_CarLaneStraightCount: number,
  m_CarLaneRightCount: number,
  m_CarLaneUTurnCount: number,
  m_TrackLaneLeftCount: number,
  m_TrackLaneStraightCount: number,
  m_TrackLaneRightCount: number,
   m_BicycleLaneCount: number,
  m_PedestrianLaneCount: number,
  m_SubLaneGroupMask: SubLaneGroupMask
}

export interface ToolTooltipMessage {
  image: string,
  message: string
}

export interface MemberPhaseData {
  index: number;
  minimumDuration: number;
  maximumDuration: number;
}

export interface PatternInfo {
  name: string;
  value: number;
}

export interface GroupMemberInfo {
  entity: Entity;
  index: number;
  version: number;
  isLeader: boolean;
  distanceToLeader: number;
  phaseOffset: number;
  signalDelay: number;
  isCurrentJunction: boolean;
  phases?: MemberPhaseData[];
  phaseCount?: number;
  currentPattern?: number;
  availablePatterns?: PatternInfo[];
  hasTrainTrack?: boolean;
}

export interface MainPanelItemTrafficGroup {
  itemType: "trafficGroup",
  groupIndex: number,
  groupVersion: number,
  name: string,
  memberCount: number,
  isCoordinated: boolean,
  isCurrentJunctionInGroup: boolean,
  greenWaveEnabled: boolean,
  greenWaveSpeed: number,
  greenWaveOffset: number,
  leaderIndex?: number,
  leaderVersion?: number,
  currentJunctionIndex?: number,
  currentJunctionVersion: number;
  members?: GroupMemberInfo[];
  distanceToLeader?: number,
  phaseOffset?: number,
  signalDelay?: number,
  isCurrentJunctionLeader?: boolean,
  previousState?: number,
  cycleLength?: number,
}