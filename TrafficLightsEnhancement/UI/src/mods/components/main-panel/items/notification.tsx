import styled from 'styled-components';

import { useLocalization } from 'cs2/l10n';
import { engineCall } from '../../../engine';
import { MainPanelItemNotification } from 'mods/general';

const Notice = styled.div`
  border-radius: 3rem;
  padding: 8rem;
  display: flex;
  width: 100%;
  background-color: rgba(75, 200, 240, 0.5);
`;

const Warning = styled.div`
  border-radius: 3rem;
  padding: 8rem;
  display: flex;
  width: 100%;
  background-color: rgba(200, 0, 0, 0.5);
`;

const Image = styled.img`
  width: 20rem;
  height: 20rem;
  margin-right: 10rem;
`;

const Label = styled.div`
  color: var(--textColor);
  flex: 1;
`;

export default function Notification(props: { data: MainPanelItemNotification }) {
  const { translate } = useLocalization();
  const clickHandler = () => {
    if (props.data.engineEventName && props.data.engineEventName.length > 0) {
      engineCall(props.data.engineEventName, JSON.stringify(props.data));
    }
  };
  return (
    <>
      {props.data.notificationType == "warning" &&
      <Warning onClick={clickHandler}>
        <Image src="Media/Game/Icons/AdvisorNotifications.svg" />
        <Label>{translate(`UI.LABEL[C2VM.TrafficLightsEnhancement.${props.data.label}]`) ?? props.data.label}</Label>
      </Warning>}
      {props.data.notificationType == "notice" &&
      <Notice onClick={clickHandler}>
        <Image src="Media/Game/Icons/AdvisorNotifications.svg" />
        <Label>{translate(`UI.LABEL[C2VM.TrafficLightsEnhancement.${props.data.label}]`) ?? props.data.label}</Label>
      </Notice>}
    </>
  );
}