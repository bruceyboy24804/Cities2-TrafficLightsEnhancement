import { useContext, useEffect, useState } from "react";
import { useSortable } from "@dnd-kit/sortable";
import { callRemoveCustomPhase, callSetActiveCustomPhaseIndex } from "bindings";
import { Icon } from "cs2/ui";

import { LocaleContext } from "../../../context";
import { getString } from "../../../localisations";

import Check from "../../common/icons/check";
import Delete from "../../common/icons/delete";
import Tune from "../../common/icons/tune";
import Visibility from "../../common/icons/visibility";
import VisibilityOff from "../../common/icons/visibility-off";
import gripVerticalSrc from "../../common/icons/grip-vertical.svg";
import Row from "../../main-panel/items/row";
import ItemDivider from "./item-divider";
import { MainPanelItemCustomPhase } from "mods/general";
import styles from "./modules/item.module.scss";
import classNames from "classnames";

const ActiveDot = () => <div className={styles.activeDot}>•</div>;

export default function Item(props: { data: MainPanelItemCustomPhase }) {
	const locale = useContext(LocaleContext);
	const [isActiveLabel, setIsActiveLabel] = useState(false);
	const [showEditor, setShowEditor] = useState(false);

	const {
		attributes,
		listeners,
		setNodeRef,
		transform,
		transition,
		isDragging,
	} = useSortable({ id: `phase-${props.data.index}` });

	const style = {
		transform: transform ? `translate3d(${transform.x}px, ${transform.y}px, 0)` : undefined,
		transition,
	};

	useEffect(() => {
		if (props.data.activeViewingIndex >= 0) {
			setIsActiveLabel(props.data.activeViewingIndex == props.data.index);
		} else if (props.data.activeIndex >= 0) {
			setIsActiveLabel(props.data.activeIndex == props.data.index);
		} else {
			setIsActiveLabel(props.data.index + 1 == props.data.currentSignalGroup);
		}
		setShowEditor(props.data.activeIndex == props.data.index);
	}, [
		props.data.activeViewingIndex,
		props.data.activeIndex,
		props.data.index,
		props.data.currentSignalGroup
	]);

	return (
		<div ref={setNodeRef} style={style} className={classNames(styles.itemContainer, { [styles.dragging]: isDragging, [styles.dim]: isActiveLabel })} data-dim={isActiveLabel}>
			<div>
				<Row style={{ padding: "0.25em" }} disableEngineCall={true}>
					<div className={styles.dragHandle} {...attributes} {...listeners}>
						<Icon src={gripVerticalSrc} tinted className={styles.dragIcon} />
					</div>
					<div className={styles.label}>
						{getString(locale, "Phase") + " #" + (props.data.index + 1)}
						{props.data.activeIndex < 0 &&
							props.data.index + 1 === props.data.currentSignalGroup && <ActiveDot />}
					</div>
					<div className={styles.iconBarContainer}>
						{!showEditor && (
							<>
								{props.data.activeViewingIndex == props.data.index && (
									<div className={styles.iconContainer}>
										<VisibilityOff
											className={styles.iconStyle}
											onClick={() =>
												callSetActiveCustomPhaseIndex(
													JSON.stringify({
														key: "ActiveViewingCustomPhaseIndex",
														value: -1
													})
												)
											}
										/>
									</div>
								)}
								{props.data.activeViewingIndex != props.data.index && (
									<div className={styles.iconContainer}>
										<Visibility
											className={styles.iconStyle}
											onClick={() =>
												callSetActiveCustomPhaseIndex(
													JSON.stringify({
														key: "ActiveViewingCustomPhaseIndex",
														value: props.data.index
													})
												)
											}
										/>
									</div>
								)}
								<div className={styles.iconContainer}>
									<Tune
										className={styles.iconStyle}
										onClick={() =>
											callSetActiveCustomPhaseIndex(
												JSON.stringify({
													key: "ActiveEditingCustomPhaseIndex",
													value: props.data.index
												})
											)
										}
									/>
								</div>
							</>
						)}
						{showEditor && (
							<>
								<div className={styles.iconContainer}>
									<Delete
										className={styles.iconStyle}
										onClick={() =>
											callRemoveCustomPhase(JSON.stringify({ index: props.data.index }))
										}
									/>
								</div>
								<div className={styles.iconContainer}>
									<Check
										className={styles.iconStyle}
										onClick={() =>
											callSetActiveCustomPhaseIndex(
												JSON.stringify({
													key: "ActiveEditingCustomPhaseIndex",
													value: -1
												})
											)
										}
									/>
								</div>
							</>
						)}
					</div>
				</Row>
				{props.data.index + 1 < props.data.length && (
					<ItemDivider index={props.data.index} linked={props.data.linkedWithNextPhase} />
				)}
			</div>
		</div>
	);
}