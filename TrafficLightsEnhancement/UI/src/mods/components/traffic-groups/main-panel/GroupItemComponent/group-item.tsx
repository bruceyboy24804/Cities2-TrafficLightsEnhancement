import React from "react";
import styled from "styled-components";

import { callAddJunctionToGroup, callDeleteTrafficGroup } from "bindings";
import { MainPanelItemTrafficGroup } from "mods/general";

import Delete from "../../../common/icons/delete";
import Tune from "../../../common/icons/tune";
import Row from "../../../main-panel/items/row";
import styles from "./group-item.module.scss";
import { Tooltip } from "cs2/ui";


const ActiveDot = () => <span className={styles.activeDot}>•</span>;

export default function GroupItem(props: { 
	data: MainPanelItemTrafficGroup;
	isViewing?: boolean;
	onView?: (groupIndex: number, groupVersion: number) => void;
}) {
	const { data, isViewing, onView } = props;

	const clickHandler = () => {
		if (!data.isCurrentJunctionInGroup) {
			callAddJunctionToGroup(JSON.stringify({
				groupIndex: data.groupIndex,
				groupVersion: data.groupVersion
			}));
		} else {
			onView?.(data.groupIndex, data.groupVersion);
		}
	};

	const deleteHandler = (e: React.MouseEvent) => {
		e.stopPropagation();
		callDeleteTrafficGroup(JSON.stringify({
			groupIndex: data.groupIndex,
			groupVersion: data.groupVersion
		}));
	};

	const viewHandler = (e: React.MouseEvent) => {
		e.stopPropagation();
		onView?.(data.groupIndex, data.groupVersion);
	};

	const displayName = data.name || "Unnamed Group";

	return (
		<Tooltip tooltip={`${displayName} (${data.memberCount})`}>
		<div onClick={clickHandler} style={{ cursor: "pointer" }} className={styles.hover}>
			<Row style={{ padding: "0.25em" }} hoverEffect={true}>
				<div className={data.isCurrentJunctionInGroup ? styles.labelWrapper : `${styles.labelWrapper} ${styles.dim}`}>
					{`${displayName} (${data.memberCount})`}{data.isCurrentJunctionInGroup && <ActiveDot />}
				</div>
				<div className={styles.iconBarContainer}>
					<div className={styles.iconContainer} onClick={viewHandler}>
						<Tune className={styles.iconStyle} style={isViewing ? {color: "var(--accentColorNormal)"} : undefined} />
					</div>
					<div className={styles.iconContainer} onClick={deleteHandler}>
						<Delete className={styles.iconStyle} />
					</div>
				</div>
			</Row>
		</div>
		</Tooltip>
	);
}

