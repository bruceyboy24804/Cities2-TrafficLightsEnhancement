import React, { CSSProperties, useState, useRef, useEffect, useCallback } from "react";
import Button from 'mods/components/common/button';
import Checkbox from 'mods/components/common/checkbox';
import { TextInput } from 'mods/components/common/TextInput';
import classNames from "classnames";
import { ReactNode } from "react";
import { trigger, useValue } from "cs2/api";
import { Dropdown, DropdownItem, DropdownToggle, PanelFoldout, Tooltip } from "cs2/ui";
import Divider from "mods/components/main-panel/items/divider"
import { MainPanelItem, MainPanelItemTrafficGroup, GroupMemberInfo, MemberPhaseData, EdgeInfo, EdgeGroupMask, CustomPhaseSignalState, CustomPhaseLaneType, CustomPhaseLaneDirection, PatternInfo } from 'mods/general';
import { 
	callCreateTrafficGroup, 
	callDeleteTrafficGroup, 
	callEnterAddMemberMode,
	callEnterSelectMemberMode,
	callExitSelectMemberMode,
	callRemoveJunctionFromGroup, 
	callSelectJunction, 
	callSetTrafficGroupName, 
	callCalculateSignalDelays,
	callSetMainPanelState,
	callCopyPhasesToJunction,
	callUpdateMemberPattern,
	edgeInfo
} from '../../../../../bindings';
import MainPanelRange from 'mods/components/main-panel/items/range';
import Scrollable, { ScrollableRef } from 'mods/components/common/scrollable';
import Row from '../../../main-panel/items/row';
import styles from './index.module.scss';
import GroupItem from "../GroupItemComponent/group-item";
import MainPanelRadio from "../../../main-panel/items/radio";
import { MainPanelItemButton, MainPanelItemTitle } from "mods/general";
import Title from "mods/components/main-panel/items/title";
import TitleDim from "mods/components/main-panel/items/title-dim";
import { Entity } from "cs2/bindings";
import { FocusDisabled } from "cs2/input";

enum MainPanelState {
	Hidden = 0,
	Empty = 1,
	Main = 2,
	CustomPhase = 3,
	TrafficGroups = 4,
}

const ItemContainerStyle: CSSProperties = {
	display: "flex",
	flexDirection: "column",
	flex: 1,
};
const focusEntity = (e: Entity) => {
  trigger('C2VM.TrafficLightsEnhancement', 'GoTo', e);
};
const ItemTitle = (props: {title: string, secondaryText?: string, tooltip?: React.ReactNode, dim?: boolean}) => {
	const item:  MainPanelItemTitle = {
		itemType: "title",
		... props
	};
	return (
		<Row data={item}>
			{props.dim && <TitleDim {... item} />}
			{! props.dim && <Title {...item} />}
		</Row>
	);
};

const CreateGroupButton = () => {
	const clickHandler = () => {
		callCreateTrafficGroup(JSON.stringify({ name: "New Group" }));
	};
	return (
		<Row hoverEffect={true}>
			<Button label="CreateGroup" onClick={clickHandler} />
		</Row>
	);
};

const RemoveFromGroupButton = () => {
	const clickHandler = () => {
		callRemoveJunctionFromGroup("{}");
	};
	return (
		
			<Row hoverEffect={true}>				
					<Button label="Remove From Group" onClick={clickHandler} tooltip="To remove member from a group: click a member from a group then click this button" />					
			</Row>
	);
};

const CustomPhaseEditorButton = () => {
	const clickHandler = () => {
		callSetMainPanelState(JSON.stringify({
			key: "state",
			value: String(MainPanelState.CustomPhase)
		}));
	};
	return (
		<Row hoverEffect={true}>
			<Button label="Custom Phase Editor" onClick={clickHandler} />
		</Row>
	);
};

const BackButton = ({ previousState }: { previousState?: number }) => {
	// If previousState is undefined, default to Empty (which allows selecting a junction)
	const targetState = previousState !== undefined ? previousState : MainPanelState.Empty;
	const data: MainPanelItemButton = {
		itemType: "button",
		type: "button",
		key: "state",
		value: targetState.toString(),
		label: "Back",
		engineEventName: "C2VM.TrafficLightsEnhancement.TRIGGER:CallSetMainPanelState"
	};
	return (
		<Row data={data}><Button {... data} /></Row>
	);
};

// Define hierarchy viewport type matching K45HierarchyMenu
export type TrafficGroupViewport = {
	displayName: ReactNode;
	icon?:  string;
	tooltip?: string;
	level: number;
	selectable?:  boolean;
	selected?: boolean;
	expandable?: boolean;
	expanded?:  boolean;
	memberIndex:  number;
	memberVersion: number;
	isLeader?: boolean;
	currentPattern?: number;
	availablePatterns?: PatternInfo[];
	hasTrainTrack?: boolean;
};

type PropsTrafficGroupMenu = {
	viewport: TrafficGroupViewport[];
	onSelect?:  (viewportIndex: number, selected: boolean) => any;
	onSetExpanded?:  (viewportIndex: number, expanded: boolean) => any;
	currentTreeOnlyMode?: boolean;
};

// Pattern value for CustomPhases (matches backend CustomTrafficLights.Patterns.CustomPhase)
const CUSTOM_PHASE_PATTERN = 5;

// Pattern selection component using MainPanelRadio
const MemberPatternSelector = ({ 
	memberIndex, 
	memberVersion, 
	currentPattern, 
	availablePatterns,
	currentGroup 
}: { 
	memberIndex: number; 
	memberVersion: number; 
	currentPattern: number; 
	availablePatterns: PatternInfo[];
	currentGroup?: MainPanelItemTrafficGroup | undefined;
}) => {
	const handlePatternChange = (patternValue: number) => {
		// For CustomPhases, just update pattern without navigating
		callUpdateMemberPattern(JSON.stringify({
			junctionIndex: memberIndex,
			junctionVersion: memberVersion,
			patternValue: patternValue
		}));
	};

	// Get the base pattern (lower 16 bits)
	const basePattern = currentPattern & 0xFFFF;
	const isCustomPhaseSelected = basePattern === CUSTOM_PHASE_PATTERN;

	return (
		<>
			{availablePatterns
				.filter(pattern => pattern.value !== CUSTOM_PHASE_PATTERN)
				.map((pattern) => (
					<Row key={pattern.value} hoverEffect={true}>
						<MainPanelRadio
							keyName="Pattern"
							value={String(pattern.value)}
							isChecked={basePattern === pattern.value}
							label={pattern.name}
							triggerName="CallUpdateMemberPattern"
							onClickOverride={() => handlePatternChange(pattern.value)}
						/>
					</Row>
				))}
			<Row hoverEffect={true}>
				<MainPanelRadio
					keyName="Pattern"
					value={String(CUSTOM_PHASE_PATTERN)}
					isChecked={isCustomPhaseSelected}
					label="Custom Phase"
					triggerName="CallUpdateMemberPattern"
					onClickOverride={() => handlePatternChange(CUSTOM_PHASE_PATTERN)}
				/>
			</Row>
			{currentGroup && currentGroup.members && (() => {
				const currentMember = currentGroup.members.find(m => m.isCurrentJunction);
				if (currentMember && currentMember.currentPattern !== undefined && ((currentMember.currentPattern & 0xFFFF) === CUSTOM_PHASE_PATTERN)) {
					return <CustomPhaseEditorButton />;
				}
				return null;
			})()}
		</>
	);
};

// Member foldout component using PanelFoldout
const MemberFoldout = ({
	member,
	onMemberClick,
	currentGroup
}: {
	member: GroupMemberInfo;
	onMemberClick: (member: GroupMemberInfo) => void;
	currentGroup?: MainPanelItemTrafficGroup | undefined;
}) => {
	const headerContent = (
		<div 
			className={styles.memberHeader}
			onClick={(e) => { e.stopPropagation(); onMemberClick(member); }}
			style={{ cursor: 'pointer' }}
		>
			{member.isLeader && <span className={styles.leaderBadge}>(Leader) </span>}
			Intersection {member.index}
			{member.isCurrentJunction && <span className={styles.youBadge}> (You)</span>}
			{/*{member.phases && member.phases.length > 0 && 
				<span className={styles.phaseCount}> [{member.phases.length} phases]</span>
			}*/}
		</div>
	);

	return (
		<PanelFoldout 
			header={headerContent}
			initialExpanded={false}
			disableFocus={true}
		>
			<div className={styles.sectionTitle}>Traffic Signal</div>
			<MemberPatternSelector
				memberIndex={member.index}
				memberVersion={member.version}
				currentPattern={member.currentPattern || 0}
				availablePatterns={member.availablePatterns || []}
				currentGroup={currentGroup}
			/>
		</PanelFoldout>
	);
};

export const TrafficGroupMenu = ({ viewport, onSelect, onSetExpanded, currentTreeOnlyMode }:  PropsTrafficGroupMenu) => {
	// Simple flat list - no filtering needed for traffic group members
	const targetViewport = viewport.map((item, idx) => ({ idx, item }));
	
	// Find the last non-root item for the 'last' class
	let lastNonRootIndex = -1;
	for (let i = targetViewport.length - 1; i >= 0; i--) {
		if (targetViewport[i].item.level > 0) {
			lastNonRootIndex = i;
			break;
		}
	}
	
	return <div className={styles.k45_hierarchyTree}>{targetViewport.map((x, index) =>
		<React.Fragment key={x.idx}>
			<div 
				className={classNames(
					styles.k45_hierarchy_item, 
					x.item.expanded && styles.expanded, 
					x.item.selected && styles.selected,
					index === lastNonRootIndex && styles.last
				)} 
				style={{ paddingLeft: (4 + 10 * x.item.level) + "rem" }}
			>
				<div 
					className={classNames(
						styles.k45_hierarchy_connector, 
						x.item.isLeader && styles.root
					)} 
					onClick={() => x.item.selectable && onSelect?.(x.idx, !x.item.selected)} 
				/>
				<div 
					className={classNames(
						styles.k45_hierarchy_icon, 
						x.item.expanded && styles.expanded, 
						x.item.selected && styles.selected
					)} 
					onClick={() => x.item.selectable && onSelect?.(x.idx, !x.item.selected)} 
					style={{ backgroundImage: x.item.icon ? `url(${x.item.icon})` : 'none' }}
				/>
				<div 
					className={classNames(
						styles.k45_hierarchy_title, 
						x.item.expanded && styles.expanded, 
						x.item.selected && styles.selected
					)} 
					onClick={() => x.item.selectable && onSelect?.(x.idx, !x.item.selected)} 
				>
					{x.item.displayName}
				</div>
				{x.item.expandable && (
					<div 
						onClick={(e) => { e.stopPropagation(); onSetExpanded?.(x.idx, !x.item.expanded); }} 
						className={classNames(styles.expandButton, x.item.expanded && styles.expanded)}
					>
						{x.item.expanded ? '▼' : '▶'}
					</div>
				)}
			</div>
			{x.item.expanded && x.item.availablePatterns && x.item.availablePatterns.length > 0 && (
				<div className={styles.memberExpandedContent} style={{ paddingLeft: (4 + 10 * (x.item.level + 1)) + "rem" }}>
					<div className={styles.expandedSectionTitle}>Traffic Signal</div>
					<MemberPatternSelector 
						memberIndex={x.item.memberIndex}
						memberVersion={x.item.memberVersion}
						currentPattern={x.item.currentPattern || 0}
						availablePatterns={x.item.availablePatterns}
						currentGroup={undefined}
					/>
				</div>
			)}
		</React.Fragment>
	)}</div>
};

const AddMemberButton = ({ currentGroup }: { currentGroup: MainPanelItemTrafficGroup | undefined }) => {
	const clickHandler = () => {
		if (currentGroup) {
			callEnterAddMemberMode(JSON.stringify({
				groupIndex: currentGroup.groupIndex,
				groupVersion: currentGroup.groupVersion
			}));
		}
	};
	
	return (
		<Row hoverEffect={true}>
			<Button label="Add Member" onClick={clickHandler} />
		</Row>
	);
};

const SelectMemberInWorldButton = ({ currentGroup }: { currentGroup: MainPanelItemTrafficGroup | undefined }) => {
	const clickHandler = () => {
		if (currentGroup) {
			callEnterSelectMemberMode(JSON.stringify({
				groupIndex: currentGroup.groupIndex,
				groupVersion: currentGroup.groupVersion
			}));
		}
	};
	
	return (
		<Row hoverEffect={true}>
			<Tooltip tooltip="Click to select a group member in the world. Only existing members can be selected (orange highlight)." direction="up">
				<Button label="Select Member In World" onClick={clickHandler} />
			</Tooltip>
		</Row>
	);
};

const CopyPhasesToMemberButton = ({ 
	displayedGroup, 
	currentJunctionIndex, 
	currentJunctionVersion 
}: { 
	displayedGroup: MainPanelItemTrafficGroup;
	currentJunctionIndex: number;
	currentJunctionVersion: number;
}) => {
	const copyToAllMembers = () => {
		if (!displayedGroup.members) return;
		
		// Copy phases from current junction to all other members
		displayedGroup.members.forEach(member => {
			if (member.index !== currentJunctionIndex || member.version !== currentJunctionVersion) {
				callCopyPhasesToJunction(JSON.stringify({
					sourceIndex: currentJunctionIndex,
					sourceVersion: currentJunctionVersion,
					targetIndex: member.index,
					targetVersion: member.version
				}));
			}
		});
	};
	
	return (
		<Row hoverEffect={true}>
			<Button label="Copy Phases to All Members" onClick={copyToAllMembers} />
		</Row>
	);
};





function SetBit(input: number, index: number, value: number): number {
	return ((input & (~(1 << index))) | (value << index));
}

function getSignalState(goMask: number, yieldMask: number, phaseIndex: number): CustomPhaseSignalState {
	const isGo = (goMask & (1 << phaseIndex)) !== 0;
	const isYield = (yieldMask & (1 << phaseIndex)) !== 0;
	if (isYield) return "yield";
	if (isGo) return "go";
	return "stop";
}

const SignalButton = ({ 
	state, 
	direction, 
	onClick 
}: { 
	state: CustomPhaseSignalState; 
	direction: string;
	onClick: () => void;
}) => {
	const colorMap: Record<CustomPhaseSignalState, string> = {
		"stop": "#d32f2f",
		"go": "#388e3c", 
		"yield": "#f9a825",
		"none": "#666"
	};
	
	const directionSymbols: Record<string, string> = {
		"left": "←",
		"straight": "↑",
		"right": "→",
		"uTurn": "↶",
		"all": "●"
	};
	
	if (state === "none") return null;
	
	return (
		<div 
			className={styles.signalButton}
			style={{ backgroundColor: colorMap[state] }}
			onClick={onClick}
			title={`${direction}: ${state} (click to cycle)`}
		>
			{directionSymbols[direction] || "●"}
		</div>
	);
};

const MemberSignalEditor = ({ 
	member,
	memberEdges,
	phaseIndex,
	onSignalUpdate
}: { 
	member: GroupMemberInfo;
	memberEdges: EdgeInfo[];
	phaseIndex: number;
	onSignalUpdate: (junctionIndex: number, junctionVersion: number, edgeGroupMask: EdgeGroupMask) => void;
}) => {
	if (memberEdges.length === 0) {
		return <div className={styles.infoText} style={{marginLeft: "2rem", fontSize: "0.9em"}}>No edge data available</div>;
	}

	const handleSignalClick = (edge: EdgeInfo, laneType: string, direction: string) => {
		const newGroupMask: EdgeGroupMask = JSON.parse(JSON.stringify(edge.m_EdgeGroupMask));
		
		const getNextState = (current: CustomPhaseSignalState, allowYield: boolean): CustomPhaseSignalState => {
			if (allowYield) {
				return current === "stop" ? "go" : (current === "go" ? "yield" : "stop");
			}
			return current === "stop" ? "go" : "stop";
		};

		const updateTurnMask = (turn: any, dir: string, allowYield: boolean) => {
			const dirMap: Record<string, string> = { left: "m_Left", straight: "m_Straight", right: "m_Right", uTurn: "m_UTurn" };
			const key = dirMap[dir];
			if (!key || !turn[key]) return;
			
			const current = getSignalState(turn[key].m_GoGroupMask, turn[key].m_YieldGroupMask, phaseIndex);
			const next = getNextState(current, allowYield);
			
			turn[key].m_GoGroupMask = SetBit(turn[key].m_GoGroupMask, phaseIndex, next !== "stop" ? 1 : 0);
			turn[key].m_YieldGroupMask = SetBit(turn[key].m_YieldGroupMask, phaseIndex, next === "yield" ? 1 : 0);
		};

		if (laneType === "car") {
			updateTurnMask(newGroupMask.m_Car, direction, true);
		} else if (laneType === "publicCar") {
			updateTurnMask(newGroupMask.m_PublicCar, direction, true);
		} else if (laneType === "track") {
			updateTurnMask(newGroupMask.m_Track, direction, false);
		} else if (laneType === "pedestrian") {
			const current = getSignalState(newGroupMask.m_Pedestrian.m_GoGroupMask, 0, phaseIndex);
			const next = current === "stop" ? "go" : "stop";
			newGroupMask.m_Pedestrian.m_GoGroupMask = SetBit(newGroupMask.m_Pedestrian.m_GoGroupMask, phaseIndex, next === "go" ? 1 : 0);
		} else if (laneType === "bicycle") {
			const current = getSignalState(newGroupMask.m_Bicycle.m_GoGroupMask, 0, phaseIndex);
			const next = current === "stop" ? "go" : "stop";
			newGroupMask.m_Bicycle.m_GoGroupMask = SetBit(newGroupMask.m_Bicycle.m_GoGroupMask, phaseIndex, next === "go" ? 1 : 0);
		}

		onSignalUpdate(member.index, member.version, newGroupMask);
	};

	const renderEdgeSignals = (edge: EdgeInfo, edgeIdx: number) => {
		const hasCarLanes = edge.m_CarLaneLeftCount + edge.m_CarLaneStraightCount + edge.m_CarLaneRightCount + edge.m_CarLaneUTurnCount > 0;
		const hasPublicCarLanes = edge.m_PublicCarLaneLeftCount + edge.m_PublicCarLaneStraightCount + edge.m_PublicCarLaneRightCount + edge.m_PublicCarLaneUTurnCount > 0;
		const hasTrackLanes = edge.m_TrackLaneLeftCount + edge.m_TrackLaneStraightCount + edge.m_TrackLaneRightCount > 0;
		const hasPedestrianLanes = edge.m_PedestrianLaneStopLineCount + edge.m_PedestrianLaneNonStopLineCount > 0;
		const hasBicycleLanes = (edge.m_BicycleLaneCount ?? 0) > 0;

		if (!hasCarLanes && !hasPublicCarLanes && !hasTrackLanes && !hasPedestrianLanes && !hasBicycleLanes) {
			return null;
		}

		return (
			<div key={edgeIdx} className={styles.edgeSignalRow}>
				<div className={styles.edgeLabel}>Edge {edgeIdx + 1}</div>
				<div className={styles.signalGroup}>
					{hasCarLanes && (
						<div className={styles.laneTypeGroup}>
							<span className={styles.laneTypeLabel}>🚗</span>
							{edge.m_CarLaneLeftCount > 0 && (
								<SignalButton 
									state={getSignalState(edge.m_EdgeGroupMask.m_Car.m_Left.m_GoGroupMask, edge.m_EdgeGroupMask.m_Car.m_Left.m_YieldGroupMask, phaseIndex)}
									direction="left"
									onClick={() => handleSignalClick(edge, "car", "left")}
								/>
							)}
							{edge.m_CarLaneStraightCount > 0 && (
								<SignalButton 
									state={getSignalState(edge.m_EdgeGroupMask.m_Car.m_Straight.m_GoGroupMask, edge.m_EdgeGroupMask.m_Car.m_Straight.m_YieldGroupMask, phaseIndex)}
									direction="straight"
									onClick={() => handleSignalClick(edge, "car", "straight")}
								/>
							)}
							{edge.m_CarLaneRightCount > 0 && (
								<SignalButton 
									state={getSignalState(edge.m_EdgeGroupMask.m_Car.m_Right.m_GoGroupMask, edge.m_EdgeGroupMask.m_Car.m_Right.m_YieldGroupMask, phaseIndex)}
									direction="right"
									onClick={() => handleSignalClick(edge, "car", "right")}
								/>
							)}
							{edge.m_CarLaneUTurnCount > 0 && (
								<SignalButton 
									state={getSignalState(edge.m_EdgeGroupMask.m_Car.m_UTurn.m_GoGroupMask, edge.m_EdgeGroupMask.m_Car.m_UTurn.m_YieldGroupMask, phaseIndex)}
									direction="uTurn"
									onClick={() => handleSignalClick(edge, "car", "uTurn")}
								/>
							)}
						</div>
					)}
					{hasPedestrianLanes && (
						<div className={styles.laneTypeGroup}>
							<span className={styles.laneTypeLabel}>🚶</span>
							<SignalButton 
								state={getSignalState(edge.m_EdgeGroupMask.m_Pedestrian.m_GoGroupMask, 0, phaseIndex)}
								direction="all"
								onClick={() => handleSignalClick(edge, "pedestrian", "all")}
							/>
						</div>
					)}
				</div>
			</div>
		);
	};

	return (
		<div className={styles.memberSignalEditor}>
			<div className={styles.signalEditorHeader}>
				<span>Phase {phaseIndex + 1} Signals</span>
				<span className={styles.signalLegend}>
					<span style={{color: "#388e3c"}}>●Go</span>
					<span style={{color: "#f9a825"}}>●Yield</span>
					<span style={{color: "#d32f2f"}}>●Stop</span>
				</span>
			</div>
			{memberEdges.map((edge, idx) => renderEdgeSignals(edge, idx))}
		</div>
	);
};

export default function TrafficGroupsMainPanel(props: { items: MainPanelItem[] }) {
	const groups = props.items.filter(item => item.itemType === "trafficGroup") as MainPanelItemTrafficGroup[];
	const currentGroup = groups.find(g => g.isCurrentJunctionInGroup);
	
	// Get edge info for all group members
	const edgeInfoList = useValue(edgeInfo.binding);
	
	// Track which group is being viewed (for viewing groups without being a member)
	const [viewingGroupId, setViewingGroupId] = useState<{index: number, version: number} | null>(null);
	
	// Track which members are expanded for inline phase editing
	const [expandedMembers, setExpandedMembers] = useState<Set<string>>(new Set());
	
	// Track which phase is being edited for signal states (per member)
	const [editingPhaseIndex, setEditingPhaseIndex] = useState<Record<string, number>>({});
	
	// The displayed group is either the one being viewed or the current junction's group
	const viewingGroup = viewingGroupId 
		? groups.find(g => g.groupIndex === viewingGroupId.index && g.groupVersion === viewingGroupId.version)
		: null;
	const displayedGroup = viewingGroup || currentGroup;
	
	const previousState = groups.length > 0 ? groups[0].previousState : undefined;

	const rightPanelScrollRef = useRef<ScrollableRef>(null);
	const savedScrollPosition = useRef<number>(0);

	useEffect(() => {
		if (rightPanelScrollRef.current && savedScrollPosition.current >= 0) {
			const timer = setTimeout(() => {
				if (rightPanelScrollRef.current) {
					rightPanelScrollRef.current.setScrollPosition(savedScrollPosition.current);
					savedScrollPosition.current = -1; 
				}
			}, 50);
			
			return () => clearTimeout(timer);
		}
	}, [displayedGroup]);

	// Auto-select first member and show group panel when first member is added
	useEffect(() => {
		if (displayedGroup && 
			displayedGroup.members && 
			displayedGroup.members.length === 1 && 
			displayedGroup.members[0].isCurrentJunction &&
			!viewingGroupId) {
			
			const firstMember = displayedGroup.members[0];
			
			// Auto-select the first member
			const entity: Entity = { index: firstMember.index, version: firstMember.version };
			focusEntity(entity);
			
			const jsonData = JSON.stringify({
				index: firstMember.index,
				version: firstMember.version,
				stayOnTrafficGroups: true
			});
			callSelectJunction(jsonData);
			
			// Auto-show the group panel
			setViewingGroupId({
				index: displayedGroup.groupIndex,
				version: displayedGroup.groupVersion
			});
		}
	}, [displayedGroup?.members?.length, displayedGroup?.groupIndex, displayedGroup?.groupVersion, viewingGroupId]);

	const handleViewGroup = (groupIndex: number, groupVersion: number) => {
		// Toggle viewing - if already viewing this group, stop viewing
		if (viewingGroupId?.index === groupIndex && viewingGroupId?.version === groupVersion) {
			setViewingGroupId(null);
		} else {
			setViewingGroupId({ index: groupIndex, version: groupVersion });
		}
	};

	const handleNameChange = (name: string) => {
		if (displayedGroup) {
			callSetTrafficGroupName(JSON.stringify({
				groupIndex: displayedGroup.groupIndex,
				groupVersion: displayedGroup.groupVersion,
				name: name
			}));
		}
	};

	const handleMemberClick = (member: GroupMemberInfo) => {
		if (rightPanelScrollRef.current) {
			const currentScroll = rightPanelScrollRef.current.getScrollPosition();
			savedScrollPosition.current = currentScroll;
		}
		
		// Focus camera on the member entity
		const entity: Entity = { index: member.index, version: member.version };
		focusEntity(entity);
		
		const jsonData = JSON.stringify({
			index: member.index,
			version: member.version,
			stayOnTrafficGroups: true  // Flag to stay on Traffic Groups screen
		});
		callSelectJunction(jsonData);
	};

	return (
		<div className={styles.container}>
			<div className={styles.leftPanelContainer}>
				<Scrollable style={{ flex: 1 }} contentStyle={ItemContainerStyle}>
					{groups.map(group => (
						<GroupItem 
							key={`${group.groupIndex}-${group.groupVersion}`} 
							data={group}
							isViewing={viewingGroupId?.index === group.groupIndex && viewingGroupId?.version === group.groupVersion}
							onView={handleViewGroup}
						/>
					))}
				</Scrollable>
				<Divider />
				<CreateGroupButton />
				<BackButton previousState={previousState} />
			</div>
			<div className={styles.rightPanelContainer}>
				<Scrollable ref={rightPanelScrollRef} style={{ flex: 1 }} contentStyle={{ flex: 1 }} trackStyle={{ marginLeft: "0.25em" }}>
					{displayedGroup ? (
						<>
							<div className={styles.sectionTitle}>Group Info</div>
							<div className={styles.statRow}>
								<div className={styles.statLabel}>Name</div>
								<div className={styles.nameInputContainer}>
									<TextInput
										value={displayedGroup.name}
										onChange={handleNameChange}
										placeholder="Group Name"
									/>
								</div>
							</div>
							<div className={styles.statRow}>
								<div className={styles.statLabel}>Members</div>
								<div className={styles.statValue}>{displayedGroup.memberCount}</div>
							</div>
							
						
							<Divider />
							<div className={styles.sectionTitle}>Group Members</div>
							
							{displayedGroup.members && displayedGroup.members.length > 0 ? (
								<>
									
									{displayedGroup.members.map((member) => (
										<FocusDisabled>
											<Tooltip tooltip="Click member name to select the junction and click the foldout to show its options" direction="right">
												<MemberFoldout
													key={`${member.index}-${member.version}`}
													member={member}
													onMemberClick={handleMemberClick}
													currentGroup={displayedGroup}
												/>
											</Tooltip>
										</FocusDisabled>
										
									))}
								</>
							) : (
								<div className={styles.infoText}>No members in this group</div>
							)}

							<AddMemberButton currentGroup={displayedGroup} />
							<SelectMemberInWorldButton currentGroup={displayedGroup} />
							{currentGroup && <RemoveFromGroupButton />}
							{displayedGroup.isCurrentJunctionInGroup && displayedGroup.members && displayedGroup.members.length > 1 && (() => {
								const currentMember = displayedGroup.members.find(m => m.isCurrentJunction);
								if (currentMember) {
									return (
										<>
											<CopyPhasesToMemberButton 
												displayedGroup={displayedGroup}
												currentJunctionIndex={currentMember.index}
												currentJunctionVersion={currentMember.version}
											/>
										</>
									);
								}
								return null;
							})()}
							<Divider />
							<div className={styles.sectionTitle}>Group Settings</div>
							
									<Row hoverEffect={true} className={styles.hover} data={{
									itemType: "checkbox",
									type: "",
									isChecked: displayedGroup.isCoordinated,
									key: "Coordinated",
									value: "0",
									label: "",
									engineEventName: "C2VM.TrafficLightsEnhancement.TRIGGER:CallSetCoordinated",
								}}>
									<Checkbox isChecked={displayedGroup.isCoordinated} />
									<div className={styles.dimLabel}>Enable Coordination</div>
								</Row>
								<Row hoverEffect={true} className={styles.hover} data={{
								itemType: "checkbox",
								type: "",
								isChecked: displayedGroup.greenWaveEnabled,
								key: "GreenWaveEnabled",
								value: "0",
								label: "",
								engineEventName: "C2VM.TrafficLightsEnhancement.TRIGGER:CallSetGreenWaveEnabled"
								}}>
									<Checkbox isChecked={displayedGroup.greenWaveEnabled} />
									<div className={styles.dimLabel}>Enable Green Wave</div>
								</Row>
							    
							

							

							{displayedGroup.greenWaveEnabled && (
								<>
									<MainPanelRange data={{
										itemType: "range",
										key: "GreenWaveSpeed",
										label: "Speed",
										value: displayedGroup.greenWaveSpeed,
										valuePrefix: "",
										valueSuffix: " u/s",
										min: 10,
										max: 100,
										step: 5,
										defaultValue: 50,
										enableTextField: true,
										textFieldRegExp: "^\\d{0,3}$",
										engineEventName: "C2VM.TrafficLightsEnhancement.TRIGGER:CallSetGreenWaveSpeed",
										tooltip: "Sets the target speed for the green wave coordination.  Higher speeds mean traffic lights will change earlier to accommodate faster-moving vehicles."
									}} />

									<MainPanelRange data={{
										itemType: "range",
										key: "GreenWaveOffset",
										label: "Offset",
										value: displayedGroup.greenWaveOffset,
										valuePrefix: "",
										valueSuffix: "s",
										min: -30,
										max: 30,
										step: 1,
										defaultValue: 0,
										enableTextField: true,
										textFieldRegExp: "^-? \\d{0,2}$",
										engineEventName: "C2VM.TrafficLightsEnhancement.TRIGGER:CallSetGreenWaveOffset",
										tooltip: "Adjusts the timing offset for the green wave.  Positive values delay the wave, negative values advance it.  Use this to fine-tune coordination."
									}} />

									{displayedGroup.isCurrentJunctionInGroup && (
										<MainPanelRange data={{
											itemType: "range",
											key: "SignalDelay",
											label: "Signal Delay",
											value: displayedGroup.signalDelay || 0,
											valuePrefix:  "",
											valueSuffix:  " ticks",
											min: -60,
											max: 60,
											step: 1,
											defaultValue: 0,
											enableTextField: true,
											textFieldRegExp: "^-?\\d{0,2}$",
											engineEventName:  "C2VM.TrafficLightsEnhancement.TRIGGER:CallSetSignalDelay",
											tooltip: "Manual signal delay for this junction. Set to 0 to use automatic green wave calculation.  Positive values delay the signal, negative values advance it."
										}} />
									)}

									<Row hoverEffect={true} data={{
										itemType: "button",
										type: "button",
										key: "CalculateSignalDelays",
										value: "",
										label: "Calculate Signal Delays",
										engineEventName:  "C2VM.TrafficLightsEnhancement.TRIGGER:CallCalculateSignalDelays"
									}}>
										<Button label="Calculate Signal Delays" onClick={() => {
											if (displayedGroup) {
												callCalculateSignalDelays(JSON.stringify({
													groupIndex: displayedGroup.groupIndex,
													groupVersion: displayedGroup.groupVersion
												}));
											}
										}} />
									</Row>
								</>
							)}
							
							<Divider />
						
							<ItemTitle title="Statistics" />
							<ItemTitle title="Group ID" secondaryText={`${displayedGroup.groupIndex}:${displayedGroup.groupVersion}`} dim={true} />
							<ItemTitle title="Coordinated" secondaryText={displayedGroup.isCoordinated ? "Yes" : "No"} dim={true} />
							<ItemTitle title="Green Wave" secondaryText={displayedGroup.greenWaveEnabled ? "Enabled" : "Disabled"} dim={true} />
							{displayedGroup.greenWaveEnabled && (
								<>
									<ItemTitle title="Speed" secondaryText={`${displayedGroup.greenWaveSpeed} u/s`} dim={true} />
									<ItemTitle title="Offset" secondaryText={`${displayedGroup.greenWaveOffset}s`} dim={true} />
								</>
							)}
							{displayedGroup.isCurrentJunctionInGroup && (
								<>
									<ItemTitle title="Junction Role" secondaryText={displayedGroup.isCurrentJunctionLeader ? "Leader" : "Follower"} dim={true} />
									{!displayedGroup.isCurrentJunctionLeader && displayedGroup.distanceToLeader !== undefined && (
										<ItemTitle title="Distance to Leader" secondaryText={`${displayedGroup.distanceToLeader.toFixed(1)}m`} dim={true} />
									)}
									{displayedGroup.phaseOffset !== undefined && displayedGroup.phaseOffset !== 0 && (
										<ItemTitle title="Phase Offset" secondaryText={`${displayedGroup.phaseOffset} ticks`} dim={true} />
									)}
								</>
							)}
						</>
					) : (
						<div className={styles.infoText}>Select a group to add this junction, or create a new group. </div>
					)}
				</Scrollable>
			</div>
		</div>
	);
}