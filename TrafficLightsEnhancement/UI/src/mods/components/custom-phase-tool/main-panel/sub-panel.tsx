import {useContext, useState} from "react";

import {callHighlightEdge, callSetMainPanelState, callUpdateCustomPhaseData, callApplyPhaseTemplate} from "bindings";

import {PanelFoldout} from "cs2/ui";

import {LocaleContext} from "../../../context";
import {getString} from "../../../localisations";

import Button from "../../common/button";
import Divider from "../../main-panel/items/divider";
import MainPanelRange from "../../main-panel/items/range";
import Row from "../../main-panel/items/row";
import Title from "../../main-panel/items/title";
import TitleDim from "../../main-panel/items/title-dim";
import {EdgeInfo, MainPanelItemCustomPhase, MainPanelItemTitle} from "mods/general";
import {MainPanelState} from "../../../constants";

import styles from "./modules/sub-panel.module.scss";
import MainPanelRadio from "mods/components/main-panel/items/radio";
import MainPanelCheckbox from "mods/components/main-panel/items/checkbox";
import PresetManager from "../../common/preset-manager/preset-manager";

const PHASE_TEMPLATES = [
    { id: 0, name: "Default", description: "Standard timing" },
    { id: 1, name: "Quick Cycle", description: "Short, responsive" },
    { id: 2, name: "Heavy Traffic", description: "Long, steady flow" },
    { id: 3, name: "Pedestrian Friendly", description: "Balanced for peds" },
    { id: 4, name: "Rail Priority", description: "Rail-first switching" },
    { id: 5, name: "Night Mode", description: "Very short, skips empty" },
];
export const ItemTitle = (props: {
    title: string,
    secondaryText?: string,
    tooltip?: React.ReactNode,
    dim?: boolean
}) => {
    const item: MainPanelItemTitle = {
        itemType: "title",
        ...props
    };
    return (
        <Row data={item}>
            {props.dim && <TitleDim {...item} />}
            {!props.dim && <Title {...item} />}
        </Row>
    );
};

const EndPhaseButton = (props: { index: number, disabled?: boolean }) => {
    const clickHandler = () => {
        if (!props.disabled) {
            callUpdateCustomPhaseData(JSON.stringify({key: "EndPhasePrematurely", index: props.index}));
        }
    };
    return (
        <Row hoverEffect={!props.disabled}>
            <Button
                label={props.disabled ? "PhaseEndRequested" : "EndPhasePrematurely"}
                disabled={props.disabled}
                onClick={clickHandler}
            />
        </Row>
    );
};

export function TrafficLightModeSelector(props: { trafficLightMode: number }) {
    const locale = useContext(LocaleContext);
    return (
        <PanelFoldout header={<div className={styles.foldoutHeader}>{getString(locale, "TrafficLightMode")}</div>}
                      initialExpanded={true}>
                        <MainPanelRadio
                        keyName="TrafficLightMode"
                        value="0"
                        isChecked={props.trafficLightMode === 0}
                        label="Dynamic"
                        triggerName="CallUpdateCustomPhaseData"
            />
            <MainPanelRadio
                keyName="TrafficLightMode"
                value="1"
                isChecked={props.trafficLightMode === 1}
                label="Fixed Timed"
                triggerName="CallUpdateCustomPhaseData"
            />
        </PanelFoldout>
    );
}

const EdgeFoldout = ({
                         edge,
                         phaseIndex,
                         isHighlighted,
                         onHighlight
                     }: {
    edge: EdgeInfo;
    phaseIndex: number;
    isHighlighted: boolean;
    onHighlight: (edgeIndex: number, edgeVersion: number) => void;
}) => {
    const edgeName = `Edge ${edge.m_Edge.index}`;

    const handleHeaderClick = () => {
        onHighlight(edge.m_Edge.index, edge.m_Edge.version);
    };

    return (
        <PanelFoldout
            header={
                <div
                    className={styles.foldoutHeader}
                    onClick={handleHeaderClick}
                    style={{
                        cursor: 'pointer',
                        color: isHighlighted ? 'var(--accentColorNormal)' : undefined
                    }}
                >
                    {edgeName}
                </div>
            }
            initialExpanded={false}
        >
            <MainPanelRange className={styles.hover} data={{
                itemType: "range",
                key: JSON.stringify({
                    edgeIndex: edge.m_Edge.index,
                    edgeVersion: edge.m_Edge.version,
                    phaseIndex: phaseIndex,
                    field: "openDelay"
                }),
                label: "Start Delay",
                value: edge.m_OpenDelay ?? 0,
                valuePrefix: "",
                valueSuffix: "",
                min: 0,
                max: 30,
                step: 1,
                defaultValue: 0,
                enableTextField: true,
                textFieldRegExp: "^\\d{0,3}$",
                engineEventName: "C2VM.TrafficLightsEnhancement.TRIGGER:CallUpdateEdgeDelay",
                tooltip: "Delay (in ticks) before this edge's signals turn green after the phase starts."
            }}/>
            <MainPanelRange className={styles.hover} data={{
                itemType: "range",
                key: JSON.stringify({
                    edgeIndex: edge.m_Edge.index,
                    edgeVersion: edge.m_Edge.version,
                    phaseIndex: phaseIndex,
                    field: "closeDelay"
                }),
                label: "End Early",
                value: edge.m_CloseDelay ?? 0,
                valuePrefix: "",
                valueSuffix: "",
                min: 0,
                max: 30,
                step: 1,
                defaultValue: 0,
                enableTextField: true,
                textFieldRegExp: "^\\d{0,3}$",
                engineEventName: "C2VM.TrafficLightsEnhancement.TRIGGER:CallUpdateEdgeDelay",
                tooltip: "Time (in ticks) before the phase ends when this edge's signals turn red."
            }}/>
        </PanelFoldout>
    );
};

export default function SubPanel(props: {
    data: MainPanelItemCustomPhase | null;
    edges?: EdgeInfo[];
    phaseIndex?: number;
    statisticsOnly?: boolean;
}) {
    const locale = useContext(LocaleContext);
    const data = props.data;
    const [highlightedEdge, setHighlightedEdge] = useState<{ index: number, version: number } | null>(null);
    const handleBackToGroups = () => {
        callSetMainPanelState(JSON.stringify({
            key: "state",
            value: String(MainPanelState.TrafficGroups)
        }));
    };

    const handleEdgeHighlight = (edgeIndex: number, edgeVersion: number) => {
        const newHighlight = {index: edgeIndex, version: edgeVersion};
        const isSameEdge = highlightedEdge?.index === edgeIndex && highlightedEdge?.version === edgeVersion;

        if (isSameEdge) {
            setHighlightedEdge(null);
            callHighlightEdge(JSON.stringify({edgeIndex: -1, edgeVersion: -1}));
        } else {
            setHighlightedEdge(newHighlight);
            callHighlightEdge(JSON.stringify({edgeIndex, edgeVersion}));
        }
    };

    if (!data) {
        return <></>;
    }

    return (
        <>
            {!props.statisticsOnly && <>
                <PanelFoldout
                    header={<div className={styles.foldoutHeader}>{getString(locale, "TrafficLightMode")}</div>}
                    initialExpanded={true}>
                    <MainPanelRadio
                        keyName="TrafficLightMode"
                        value="0"
                        isChecked={data.trafficLightMode === 0}
                        label="Dynamic"
                        triggerName="CallUpdateCustomPhaseData"
                        tooltip="Dynamic phase mode that adjusts timing based on traffic conditions."
                        className={styles.hover}
                    />
                    <MainPanelRadio
                        keyName="TrafficLightMode"
                        value="1"
                        isChecked={data.trafficLightMode === 1}
                        label="Fixed Timed"
                        triggerName="CallUpdateCustomPhaseData"
                        tooltip="Fixed timing mode with preset phase durations."
                        className={styles.hover}
                    />
                    {data.trafficLightMode === 1 && (
                        <MainPanelCheckbox
                            keyName="SmartPhaseSelection"
                            isChecked={data.smartPhaseSelection}
                            label="Smart Phase Selection"
                            triggerName="CallUpdateCustomPhaseData"
                            tooltip="Enable intelligent phase selection based on traffic conditions. Disable for simple sequential phases (1→2→3→4→1...)."
                            className={styles.hover}
                        />
                    )}
                </PanelFoldout>

                <Divider/>
                <PanelFoldout
                    header={<div className={styles.foldoutHeader}>Timing Template</div>}
                    initialExpanded={false}>
                    <PresetManager
                        builtInTemplates={PHASE_TEMPLATES}
                        onApplyBuiltIn={(templateId) => {
                            callApplyPhaseTemplate(JSON.stringify({ templateId }));
                        }}
                    />
                </PanelFoldout>

                <Divider/>
                <PanelFoldout
                    header={<div className={styles.foldoutHeader}>{getString(locale, "PhaseChangeMode")}</div>}
                    initialExpanded={false}>
                    <MainPanelRadio
                        keyName="ChangeMetric"
                        value="0"
                        isChecked={data.changeMetric === 0}
                        label="Auto"
                        triggerName="CallUpdateCustomPhaseData"
                        tooltip="Automatically balances traffic flow and waiting time to decide when to change phase."
                        className={styles.hover}
                    />
                    <MainPanelRadio
                        keyName="ChangeMetric"
                        value="1"
                        isChecked={data.changeMetric === 1}
                        label="On Flow Drop"
                        triggerName="CallUpdateCustomPhaseData"
                        tooltip="Changes phase when traffic flow decreases. Keeps traffic moving smoothly."
                        className={styles.hover}
                    />
                    <MainPanelRadio
                        keyName="ChangeMetric"
                        value="2"
                        isChecked={data.changeMetric === 2}
                        label="On Wait Increase"
                        triggerName="CallUpdateCustomPhaseData"
                        tooltip="Changes phase when waiting traffic increases. Reduces wait times."
                        className={styles.hover}
                    />
                    <MainPanelRadio
                        keyName="ChangeMetric"
                        value="3"
                        isChecked={data.changeMetric === 3}
                        label="When Empty"
                        triggerName="CallUpdateCustomPhaseData"
                        tooltip="Changes phase only when current lanes are empty. Maximizes throughput per phase."
                        className={styles.hover}
                    />
                    <MainPanelRadio
                        keyName="ChangeMetric"
                        value="4"
                        isChecked={data.changeMetric === 4}
                        label="When No Demand"
                        triggerName="CallUpdateCustomPhaseData"
                        tooltip="Changes phase only when other lanes have waiting traffic. Avoids unnecessary changes."
                        className={styles.hover}
                    />
                    <MainPanelRange className={styles.hover} data={{
                        itemType: "range",
                        key: "WaitFlowBalance",
                        label: "Wait Sensitivity",
                        value: data.waitFlowBalance,
                        valuePrefix: "",
                        valueSuffix: "",
                        min: 0.1,
                        max: 10,
                        step: 0.1,
                        defaultValue: 1,
                        enableTextField: true,
                        textFieldRegExp: "^\\d{0,4}(\\.\\d{0,2})?$",
                        engineEventName: "C2VM.TrafficLightsEnhancement.TRIGGER:CallUpdateCustomPhaseData",
                        tooltip: "How much to prioritize waiting traffic. Higher = change phases sooner when cars are waiting."
                    }}/>
                </PanelFoldout>
            </>}

            {!props.statisticsOnly &&
                <>
                    <Divider/>
                    <PanelFoldout header={<div className={styles.foldoutHeader}>Adjustments</div>}
                                  initialExpanded={false}>
                        <MainPanelRange className={styles.hover} data={{
                            itemType: "range",
                            key: "MinimumDuration",
                            label: "Minimum Duration",
                            value: data.minimumDuration,
                            valuePrefix: "",
                            valueSuffix: "s",
                            min: 0,
                            max: 30,
                            step: 1,
                            defaultValue: 2,
                            enableTextField: true,
                            textFieldRegExp: "^\\d{0,4}$",
                            engineEventName: "C2VM.TrafficLightsEnhancement.TRIGGER:CallUpdateCustomPhaseData",
                            tooltip: "Sets the minimum time a traffic light phase must stay active before it can change, regardless of traffic conditions. "
                        }}/>
                        <MainPanelRange className={styles.hover} data={{
                            itemType: "range",
                            key: "MaximumDuration",
                            label: "Maximum Duration",
                            value: data.maximumDuration,
                            valuePrefix: "",
                            valueSuffix: "s",
                            min: 5,
                            max: 300,
                            step: 5,
                            defaultValue: 300,
                            enableTextField: true,
                            textFieldRegExp: "^\\d{0,4}$",
                            engineEventName: "C2VM.TrafficLightsEnhancement.TRIGGER:CallUpdateCustomPhaseData",
                            tooltip: "Sets the maximum time a traffic light phase can remain active. This prevents a phase from staying green too long when there's no traffic waiting."
                        }}/>
                        {data.trafficLightMode === 0 && <>
                            <MainPanelRange className={styles.hover} data={{
                                itemType: "range",
                                key: "TargetDurationMultiplier",
                                label: "Target Duration",
                                value: data.targetDurationMultiplier,
                                valuePrefix: "",
                                valueSuffix: "CustomPedestrianDurationMultiplierSuffix",
                                min: 0.1,
                                max: 10,
                                step: 0.1,
                                defaultValue: 1,
                                enableTextField: true,
                                textFieldRegExp: "^\\d{0,4}(\\.\\d{0,2})?$",
                                engineEventName: "C2VM.TrafficLightsEnhancement.TRIGGER:CallUpdateCustomPhaseData",
                                tooltip: "Scales the calculated target duration for each phase. The target duration is calculated as: 10f * (AverageCarFlow + TrackLaneOccupied * 0.5) * TargetDurationMultiplier. Higher values make phases last longer."
                            }}/>
                            <MainPanelRange className={styles.hover} data={{
                                itemType: "range",
                                key: "IntervalExponent",
                                label: "Interval Exponent",
                                value: data.intervalExponent,
                                valuePrefix: "",
                                valueSuffix: "",
                                min: 0.1,
                                max: 10,
                                step: 0.1,
                                defaultValue: 2,
                                enableTextField: true,
                                textFieldRegExp: "^\\d{0,4}(\\.\\d{0,2})?$",
                                engineEventName: "C2VM.TrafficLightsEnhancement.TRIGGER:CallUpdateCustomPhaseData",
                                tooltip: "Controls how aggressively the system prioritizes phases that haven't run recently. Used in the weighted waiting formula as an exponent - higher values make phases that haven't run for a long time get much higher priority, ensuring fair rotation."
                            }}/>
                        </>}
                    </PanelFoldout>
                    {data.trafficLightMode === 0 && <>
                        <Divider/>
                        <PanelFoldout
                            header={<div className={styles.foldoutHeader}>{getString(locale, "VehicleWeights")}</div>}
                            initialExpanded={false}>
                            <MainPanelRange
                                className={styles.hover}
                                keyName="CarWeight"
                                label="Car Weight"
                                value={data.carWeight}
                                valueSuffix="x"
                                min={0.1}
                                max={10}
                                step={0.1}
                                defaultValue={1}
                                enableTextField
                                textFieldRegExp="^\d{0,2}(\.\d{0,1})?$"
                                triggerName="CallUpdateCustomPhaseData"
                                tooltip="Weight multiplier for car lanes when calculating phase priority. Higher values give more priority to phases with waiting cars."
                            />
                            <MainPanelRange
                                className={styles.hover}
                                keyName="PublicCarWeight"
                                label="Bus Weight"
                                value={data.publicCarWeight}
                                valueSuffix="x"
                                min={0.1}
                                max={10}
                                step={0.1}
                                defaultValue={2}
                                enableTextField
                                textFieldRegExp="^\d{0,2}(\.\d{0,1})?$"
                                triggerName="CallUpdateCustomPhaseData"
                                tooltip="Weight multiplier for public transport (bus) lanes. Higher values prioritize buses over regular traffic."
                            />
                            <MainPanelRange
                                className={styles.hover}
                                keyName="TrackWeight"
                                label="Track Weight"
                                value={data.trackWeight}
                                valueSuffix="x"
                                min={0.1}
                                max={10}
                                step={0.1}
                                defaultValue={3}
                                enableTextField
                                textFieldRegExp="^\d{0,2}(\.\d{0,1})?$"
                                triggerName="CallUpdateCustomPhaseData"
                                tooltip="Weight multiplier for tram/train tracks. Higher values give highest priority to rail vehicles."
                            />
                            <MainPanelRange
                                className={styles.hover}
                                keyName="PedestrianWeight"
                                label="Pedestrian Weight"
                                value={data.pedestrianWeight}
                                valueSuffix="x"
                                min={0.1}
                                max={10}
                                step={0.1}
                                defaultValue={1}
                                enableTextField
                                textFieldRegExp="^\d{0,2}(\.\d{0,1})?$"
                                triggerName="CallUpdateCustomPhaseData"
                                tooltip="Weight multiplier for pedestrian crossings."
                            />
                            <MainPanelRange
                                className={styles.hover}
                                keyName="SmoothingFactor"
                                label="Smoothing Factor"
                                value={data.smoothingFactor}
                                min={0}
                                max={1}
                                step={0.1}
                                defaultValue={0.5}
                                enableTextField
                                textFieldRegExp="^(0(\.\d{0,1})?|1(\.0)?)$"
                                triggerName="CallUpdateCustomPhaseData"
                                tooltip="How much to blend current calculations with previous values. 0 = no smoothing (instant changes), 1 = full smoothing (very gradual changes)."
                            />
                        </PanelFoldout>
                    </>}
                    <Divider/>
                </>}

            <PanelFoldout header={<div className={styles.foldoutHeader}>Statistics</div>} initialExpanded={true}>
                <ItemTitle title="Timer"
                           secondaryText={`${data.timer} / ${Math.round(Math.min(Math.max(data.targetDuration, data.minimumDuration), data.maximumDuration))}`}
                           dim={true}/>
                <ItemTitle title="Priority" secondaryText={`${data.priority}`} dim={true}/>
                <ItemTitle title="Turns Since Last Run" secondaryText={`${data.turnsSinceLastRun}`} dim={true}/>
                <Divider/>
                <ItemTitle title="Flow" secondaryText={`${Round(data.carFlow)}`} dim={true}
                           tooltip="Average car flow through this phase"/>
                <ItemTitle title="Flow Ratio" secondaryText={`${Round(data.flowRatio)}`} dim={true}
                           tooltip="Smoothed flow ratio for phase decisions"/>
                <ItemTitle title="Wait Ratio" secondaryText={`${Round(data.waitRatio)}`} dim={true}
                           tooltip="Smoothed wait ratio for phase decisions"/>
                <ItemTitle title="Weighted Waiting" secondaryText={`${Round(data.weightedWaiting)}`} dim={true}
                           tooltip="Combined waiting metric used for phase priority"/>
                <Divider/>
                <ItemTitle title="Cars Waiting" secondaryText={`${data.carLaneOccupied}`} dim={true}/>
                <ItemTitle title="Buses Waiting" secondaryText={`${data.publicCarLaneOccupied}`} dim={true}/>
                <ItemTitle title="Trams Waiting" secondaryText={`${data.trackLaneOccupied}`} dim={true}/>
                <ItemTitle title="Pedestrians Waiting" secondaryText={`${data.pedestrianLaneOccupied}`} dim={true}/>
            </PanelFoldout>
            {!props.statisticsOnly && <>
                {props.edges && props.edges.length > 0 && props.phaseIndex !== undefined && <>
                    <Divider/>
                    <PanelFoldout header={<div className={styles.foldoutHeader}>Signal Delays</div>}
                                initialExpanded={false}>
                        {props.edges.map((edge, idx) => (
                            <EdgeFoldout
                                key={`${edge.m_Edge.index}-${edge.m_Edge.version}`}
                                edge={edge}
                                phaseIndex={props.phaseIndex!}
                                isHighlighted={highlightedEdge?.index === edge.m_Edge.index && highlightedEdge?.version === edge.m_Edge.version}
                                onHighlight={handleEdgeHighlight}
                            />
                        ))}
                    </PanelFoldout>
                </>}
            </>}
            {data.activeIndex < 0 && data.manualSignalGroup <= 0 && data.currentSignalGroup == data.index + 1 &&
                <EndPhaseButton index={data.index} disabled={data.endPhasePrematurely}/>}
        </>)
}


function Round(num: number): number {
    return Math.round(num * 100) / 100;
}