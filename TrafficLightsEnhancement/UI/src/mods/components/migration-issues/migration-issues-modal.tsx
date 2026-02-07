import { useCallback } from "react";
import { useValue } from "cs2/api";
import { affectedEntities, callRemoveAffectedEntity, callNavigateToEntity } from "../../../bindings";
import { migrationModalVisible } from "./migrationModalState";
import styles from "./migration-issues-modal.module.scss";
import { Button, Icon, Scrollable, Tooltip } from "cs2/ui";

interface Entity {
	index: number;
	version: number;
}
const plusSrc = "coui://uil/Standard/Plus.svg";
const minusSrc = "coui://uil/Standard/Minus.svg";
export const MigrationIssuesModal = () => {
	const isOpen = useValue(migrationModalVisible);
	const entities = useValue(affectedEntities.binding) as Entity[];

	const handleNavigate = useCallback((entity: Entity) => {
		callNavigateToEntity(entity);
	}, []);

	const handleRemove = useCallback((index: number) => {
		callRemoveAffectedEntity(index);
	}, []);

	const handleDismissAll = useCallback(() => {
		callRemoveAffectedEntity(-1);
		migrationModalVisible.update(false);
	}, []);

	const handleClose = useCallback(() => {
		migrationModalVisible.update(false);
	}, []);

	if (!isOpen || !entities || entities.length === 0) {
		return null;
	}

	return (
		<div className={styles.panel}>
			<div className={styles.header}>
				<div className={styles.headerIcon}>
					<Icon src="Media/Game/Icons/TrafficLights.svg" className={styles.icon} />
				</div>
				<div className={styles.headerTitle}>
					<span className={styles.title}>Data Migration Issues</span>
					<span className={styles.subtitle}>Traffic Lights Enhancement</span>
				</div>
				<div className={styles.headerActions}>
					<Button variant="round" className={styles.headerButton} onSelect={handleClose}>
						<Icon src='Media/Glyphs/Close.svg' tinted />
					</Button>
				</div>
			</div>

			<div className={styles.content}>
						<div className={styles.sectionHeader}>
							<span className={styles.sectionTitle}>Affected Intersections</span>
							<span className={styles.badge}>{entities.length}</span>
						</div>
						<p cohinline="cohinline" className={styles.sectionDescription}>
								Use the list below to quickly access affected intersections. 
								These intersections have been reset to default: 
								you will need to reconfigure these intersections manually
							</p>
						<div className={styles.listContainer}>
							<Scrollable trackVisibility="scrollable" smooth vertical >
								{entities.map((entity, index) => (
									<div className={styles.listItem} key={`entity-${index}`}>
										<div className={styles.itemInfo}>
											<span className={styles.itemLabel}>{`Intersection #${entity.index}`}</span>
										</div>
										<div className={styles.itemActions}>
											<Tooltip tooltip="Navigate to intersection">
												<Button 
													variant="icon"
													className={styles.navigateButton}
													onSelect={() => handleNavigate(entity)}
												>
													<Icon src="Media/Game/Icons/MapMarker.svg" />
												</Button>
											</Tooltip>
											<Tooltip tooltip="Remove from list">
												<Button 
													variant="icon"
													className={styles.removeButton}
													onSelect={() => handleRemove(index)}
												>
													<Icon src={'Media/Glyphs/Close.svg'} tinted />
												</Button>
											</Tooltip>
										</div>
									</div>
								))}
							</Scrollable>
						</div>
			</div>

			<div className={styles.footer}>
				<Button 
					variant="flat" 
					className={styles.dismissButton} 
					onSelect={handleDismissAll}
				>
					Dismiss All
				</Button>
			</div>
		</div>
	);
};

export default MigrationIssuesModal;
