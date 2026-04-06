import { useLocalization } from 'cs2/l10n';
import { Tooltip } from "cs2/ui"
import { trigger } from "cs2/api";
import mod from 'mod.json'
import styles from './itemsStyling.module.scss';

export interface MainPanelRadioProps {
  keyName: string;
  value: string;
  isChecked: boolean;
  label: string;
  triggerGroup?: string;
  triggerName: string;
  tooltip?: string;
  onClickOverride?: () => void;
  className?: string;
}

export default function MainPanelRadio(props: MainPanelRadioProps) {
  const { translate } = useLocalization();
  const triggerGroup = props.triggerGroup ?? mod.id;
  const triggerName = `TRIGGER:${props.triggerName}`;

  const clickHandler = () => {
    if (props.onClickOverride) {
      props.onClickOverride();
      return;
    }
    trigger(triggerGroup, triggerName, JSON.stringify({key: props.keyName, value: props.value}));
  };

  const content = (
    <div className={props.className}>
      <div className={styles.container} onClick={clickHandler}>
        <div className={styles.titleContainer}>
          <div className={styles.circle}>
            <div className={styles.bullet} style={{opacity: props.isChecked ? 1 : 0}} />
          </div>
          <div className={styles.label}>{translate(`UI.LABEL[C2VM.TrafficLightsEnhancement.${props.label}]`) ?? props.label}</div>
        </div>
      </div>
    </div>
    
  );

  return props.tooltip ? (
    <Tooltip direction="right" tooltip={props.tooltip}>
      {content}
    </Tooltip>
  ) : content;
}
