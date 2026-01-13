import { ChangeEvent, KeyboardEvent, useContext, useEffect, useMemo, useState } from 'react';
import { Tooltip } from "cs2/ui"
import { ValueBinding, trigger } from "cs2/api";
import { LocaleContext } from '../../../context';
import { engineCall } from '../../../engine';
import { getString } from '../../../localisations';
import { MainPanelItemRange } from 'mods/general';
import mod from 'mod.json'
import Input from '../../common/input';
import Range from '../../common/range';
import Check from '../../common/icons/check';
import Edit from '../../common/icons/edit';
import ResetSettings from '../../common/icons/reset-settings';
import TitleDim from './title-dim';
import styles from './itemsStyling.module.scss';



// Props for using CS2 API bindings directly
export interface MainPanelRangeBindingProps {
  binding?: ValueBinding<number>;
  value?: number;
  triggerGroup?: string;
  triggerName?: string;
  engineEventName?: string;
  keyName?: string;
  label: string;
  valuePrefix?: string;
  valueSuffix?: string;
  min: number;
  max: number;
  step: number;
  defaultValue: number;
  enableTextField?: boolean;
  textFieldRegExp?: string;
  tooltip?: string;
  onChangeOverride?: (value: number) => void;
  onUpdateOverride?: (value: number) => void;
  className?: string;
}

// Props for using existing MainPanelItemRange data object
export interface MainPanelRangeDataProps {
  data: MainPanelItemRange;
  onChangeOverride?: (value: number) => void;
  onUpdateOverride?: (value: number) => void;
  className?: string;
}

export type MainPanelRangeProps = MainPanelRangeDataProps | MainPanelRangeBindingProps;

// Type guard to check if props use data object
function isDataProps(props: MainPanelRangeProps): props is MainPanelRangeDataProps {
  return 'data' in props;
}

export default function MainPanelRange(props: MainPanelRangeProps) {
  // Normalize props to a common shape
  const config = isDataProps(props) ? {
    label: props.data.label,
    value: props.data.value,
    valuePrefix: props.data.valuePrefix,
    valueSuffix: props.data.valueSuffix,
    min: props.data.min,
    max: props.data.max,
    step: props.data.step,
    defaultValue: props.data.defaultValue,
    enableTextField: props.data.enableTextField,
    textFieldRegExp: props.data.textFieldRegExp,
    tooltip: props.data.tooltip,
    engineEventName: props.data.engineEventName,
    key: props.data.key,
  } : {
    label: props.label,
    value: props.value ?? props.binding?.value ?? props.defaultValue,
    valuePrefix: props.valuePrefix ?? '',
    valueSuffix: props.valueSuffix ?? '',
    min: props.min,
    max: props.max,
    step: props.step,
    defaultValue: props.defaultValue,
    enableTextField: props.enableTextField,
    textFieldRegExp: props.textFieldRegExp,
    tooltip: props.tooltip,
    triggerGroup: props.triggerGroup ?? mod.id,
    triggerName: props.triggerName ? `TRIGGER:${props.triggerName}` : undefined,
    engineEventName: props.engineEventName,
    key: props.keyName,
  };
  const locale = useContext(LocaleContext);
  const [value, setValue] = useState(0);
  const [textFieldActive, setTextFieldActive] = useState(false);
  const [textFieldValue, setTextFieldValue] = useState("");
  const textFieldRegExp = useMemo(() => {
    return config.textFieldRegExp ? new RegExp(config.textFieldRegExp) : null;
  }, [config.textFieldRegExp]);
  const changeHandler = (newValue: number) => {
    setValue(newValue);
    if (props.onChangeOverride) {
      props.onChangeOverride(newValue);
      return;
    }
    // Handle CS2 API trigger with key
    if ('triggerGroup' in config && config.triggerGroup && config.triggerName) {
      if ('key' in config && config.key) {
        trigger(config.triggerGroup, config.triggerName, JSON.stringify({key: config.key, value: newValue}));
      } else {
        trigger(config.triggerGroup, config.triggerName, newValue);
      }
    }
    // Handle engineEventName with TRIGGER: format (CS2 API)
    else if ('engineEventName' in config && config.engineEventName && 'key' in config) {
      const eventName = config.engineEventName;
      const triggerMatch = eventName.match(/^(.+)\.TRIGGER:(.+)$/);
      if (triggerMatch) {
        const [, modId, triggerName] = triggerMatch;
        trigger(modId, `TRIGGER:${triggerName}`, JSON.stringify({key: config.key, value: newValue}));
      } else {
        engineCall(eventName, JSON.stringify({key: config.key, value: newValue}));
      }
    }
  };
  const updateHandler = (newValue: number) => {
    setValue(newValue);
    if (props.onUpdateOverride) {
      props.onUpdateOverride(newValue);
    }
  };

  const enableTextField = () => {
    setTextFieldValue("");
    setTextFieldActive(true);
  };
  const submitTextField = () => {
    setTextFieldActive(false);
    if (textFieldValue.length > 0) {
      const newValue = parseFloat(textFieldValue);
      if (!isNaN(newValue)) {
        changeHandler(newValue);
      }
    }
  };
  const textFieldChangeHandler = (event: ChangeEvent<HTMLInputElement>) => {
    if (textFieldRegExp !== null) {
      if (event.target.value.match(textFieldRegExp)) {
        setTextFieldValue(event.target.value);
      }
    } else {
      setTextFieldValue(event.target.value);
    }
  };
  const textFieldKeyDownHandler = (event: KeyboardEvent<HTMLInputElement>) => {
    if (event.key == "Enter") {
      submitTextField();
    }
  };
  const resetHandler = () => {
    setTextFieldActive(false);
    changeHandler(config.defaultValue);
  };
  useEffect(() => {
    setValue(config.value);
  }, [config.value]);

  const titleContent = (
    <TitleDim itemType="title" title={config.label} secondaryText={!textFieldActive ? getString(locale, config.valuePrefix) + `${Math.round(value * 100) / 100}` + getString(locale, config.valueSuffix) : ""} />
  );

  // Create a compatible data object for the Range component
  const rangeData = {
    min: config.min,
    max: config.max,
    step: config.step,
    value: value,
  };

  const containerContent = (
    <div className={styles.container}>
      <div className={styles.titleContainer}>
        {titleContent}
        {textFieldActive && <Input type="number" style={{minWidth: "3em", width: "3em"}} onChange={textFieldChangeHandler} onKeyDown={textFieldKeyDownHandler} value={textFieldValue} autoFocus />}
        {config.enableTextField && <>
          {textFieldActive && <div className={styles.iconContainer}><Check className={styles.iconStyle} onClick={submitTextField} /></div>}
          {!textFieldActive && <div className={styles.iconContainer}><Edit className={styles.iconStyle} onClick={enableTextField} /></div>}
        </>}
        <div className={styles.iconContainer}><ResetSettings className={styles.iconStyle} onClick={resetHandler} /></div>
      </div>
      <div className={styles.gap} />
      <Range data={rangeData} onChange={changeHandler} onUpdate={updateHandler} />
    </div>
  );

  return config.tooltip ? (
    <Tooltip direction="right" tooltip={config.tooltip}>
      {containerContent}
    </Tooltip>
  ) : (
    containerContent
  );
}