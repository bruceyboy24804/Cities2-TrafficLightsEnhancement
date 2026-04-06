import {useValue} from 'cs2/api';
import {useLocalization} from 'cs2/l10n';
import Title from './items/title';
import Message from './items/message';
import Divider from './items/divider';
import Range from './items/range';
import Row from './items/row';
import Notification from './items/notification';
import Button from '../../components/common/button';
import Checkbox from '../../components/common/checkbox';
import Radio from '../../components/common/radio';
import Scrollable from '../../components/common/scrollable';
import { MainPanelMainData, MainPanelEmptyData, MainPanelItemRange } from 'mods/general';
import { MainPanelState } from '../../constants';
import styles from './mainPanel.module.scss';
import {
    affectedEntities,
    setPattern,
    toggleOption,
    setPedestrianDuration,
    savePanel,
    exitPanel,
    setPanelState,
    resetLaneDirectionTool,
    exitAddMemberMode,
    finishAddMemberMode,
} from '../../../bindings';
import {migrationModalVisible} from '../migration-issues/migrationModalState';

interface AddMemberMember {
    index: number;
    version: number;
    isLeader: boolean;
}

interface AddMemberData {
    isAddingMember: boolean;
    targetGroupName: string;
    memberCount: number;
    members: AddMemberMember[];
}

export default function Content(props: { mainData?: MainPanelMainData | null, emptyData?: MainPanelEmptyData | null, addMemberData?: AddMemberData }) {
    const { mainData, emptyData, addMemberData } = props;
    const { translate } = useLocalization();
    const migrationEntities = useValue(affectedEntities.binding) as {index: number, version: number}[];
    const hasMigrationIssues = migrationEntities && migrationEntities.length > 0;

    const handleShowMigrationModal = () => {
        migrationModalVisible.update(true);
    };

    if (mainData) {
        const pedestrianRangeData: MainPanelItemRange = {
            itemType: "range",
            key: "CustomPedestrianDurationMultiplier",
            label: "CustomPedestrianDurationMultiplier",
            value: mainData.pedestrianDurationMultiplier,
            valuePrefix: "",
            valueSuffix: "CustomPedestrianDurationMultiplierSuffix",
            min: 0.5,
            max: 10,
            step: 0.5,
            defaultValue: 1,
            enableTextField: false,
        };

        return (
            <div className={styles.contentContainer}>
                <Scrollable style={{flex: 1}} contentStyle={{flex: 1}} trackStyle={{marginLeft: "0.25em"}}>
                    {!mainData.isGroupMember && (
                        <>
                            <Title itemType="title" title="TrafficSignal" />
                            {mainData.availablePatterns.map(p => {
                                const isChecked = (mainData.selectedPattern & 0xFFFF) === p.value;
                                return (
                                    <Row key={p.value} hoverEffect={true} className={styles.hover}
                                        onClick={() => setPattern(p.value)}>
                                        <Radio isChecked={isChecked} />
                                        <div className={styles.contentLabel}>{translate(`UI.LABEL[C2VM.TrafficLightsEnhancement.${p.name}]`) ?? p.name}</div>
                                    </Row>
                                );
                            })}
                            {mainData.showOptions && (
                                <>
                                    <Divider />
                                    <Title itemType="title" title="Options" />
                                    {mainData.options.map(opt => (
                                        <Row key={opt.key} hoverEffect={true}
                                            onClick={() => toggleOption(parseInt(opt.key))}>
                                            <Checkbox isChecked={opt.isChecked} />
                                            <div className={styles.contentLabel}>{translate(`UI.LABEL[C2VM.TrafficLightsEnhancement.${opt.label}]`) ?? opt.label}</div>
                                        </Row>
                                    ))}
                                    {mainData.showPedestrianDuration && (
                                        <>
                                            <Divider />
                                            <Title itemType="title" title="Adjustments" />
                                            <Range data={pedestrianRangeData} onChangeOverride={(v) => setPedestrianDuration(v)} />
                                        </>
                                    )}
                                </>
                            )}
                        </>
                    )}
                    {mainData.isGroupMember && (
                        <Message itemType="message" message="EditPhasesFromGroupMenu" />
                    )}
                    <Divider />
                    {mainData.hasLaneDirectionTool && (
                        <>
                            <Title itemType="title" title="LaneDirectionTool" />
                            <Divider />
                        </>
                    )}
                    {hasMigrationIssues && (
                        <div
                            className={styles.migrationNotice}
                            onClick={handleShowMigrationModal}
                            style={{cursor: 'pointer'}}
                        >
                            <span className={styles.migrationIcon}>⚠</span>
                            <span>{`${migrationEntities.length} intersections with migration issues`}</span>
                        </div>
                    )}
                    {mainData.hasUnsavedChanges && (
                        <>
                            <Divider />
                            <Notification data={{itemType: "notification", type: "notification", label: "PleaseSave", notificationType: "warning"}} />
                        </>
                    )}
                    {mainData.isCustomPhaseMode && (
                        <Row hoverEffect={true}
                            onClick={() => setPanelState(MainPanelState.CustomPhase)}>
                            <Button label="CustomPhaseEditor" />
                        </Row>
                    )}
                    {mainData.hasLaneDirectionTool && (
                        <Row hoverEffect={true}
                            onClick={() => resetLaneDirectionTool()}>
                            <Button label="Reset" />
                        </Row>
                    )}
                    <Row hoverEffect={true}
                        onClick={() => setPanelState(MainPanelState.TrafficGroups)}>
                        <Button label="TrafficGroups" />
                    </Row>
                    {mainData.isGroupMember ? (
                        <Row hoverEffect={true} onClick={() => exitPanel()}>
                            <Button label="Exit" />
                        </Row>
                    ) : (
                        <Row hoverEffect={true} onClick={() => savePanel()}>
                            <Button label="Save" />
                        </Row>
                    )}
                </Scrollable>
            </div>
        );
    }

    if (emptyData) {
        const isAddingMember = emptyData.isAddingMember;
        const showMemberList = addMemberData?.isAddingMember && addMemberData.members && addMemberData.members.length > 0;

        return (
            <div className={styles.contentContainer}>
                <Scrollable style={{flex: 1}} contentStyle={{flex: 1}} trackStyle={{marginLeft: "0.25em"}}>
                    {isAddingMember ? (
                        <Message itemType="message" message={`AddingMemberTo:${emptyData.targetGroupName}`} />
                    ) : (
                        <Message itemType="message" message="PleaseSelectJunction" />
                    )}
                    <Divider />
                    {showMemberList && addMemberData && (
                        <div className={styles.memberListContainer}>
                            <div className={styles.memberListTitle}>Members ({addMemberData.members.length})</div>
                            {addMemberData.members
                                .sort((a, b) => {
                                    if (a.isLeader && !b.isLeader) return -1;
                                    if (!a.isLeader && b.isLeader) return 1;
                                    return a.index - b.index;
                                })
                                .map((member) => (
                                    <div key={`${member.index}-${member.version}`} className={styles.memberListItem}>
                                        Intersection {member.index} {member.isLeader &&
                                        <span className={styles.leaderBadge}>(Leader)</span>}
                                    </div>
                                ))}
                            <Divider />
                        </div>
                    )}
                    {hasMigrationIssues && (
                        <div
                            className={styles.migrationNotice}
                            onClick={handleShowMigrationModal}
                            style={{cursor: 'pointer'}}
                        >
                            <span className={styles.migrationIcon}>⚠</span>
                            <span>{`${migrationEntities.length} intersections with migration issues`}</span>
                        </div>
                    )}
                    {isAddingMember ? (
                        <div className={styles.buttonRow}>
                            <Row hoverEffect={true} onClick={() => exitAddMemberMode()}>
                                <Button label="Cancel" />
                            </Row>
                            <Row hoverEffect={true} onClick={() => finishAddMemberMode()}>
                                <Button label="Finish" />
                            </Row>
                        </div>
                    ) : (
                        <Row hoverEffect={true}
                            onClick={() => setPanelState(MainPanelState.TrafficGroups)}>
                            <Button label="TrafficGroups" />
                        </Row>
                    )}
                </Scrollable>
            </div>
        );
    }

    return null;
}