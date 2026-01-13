import { useContext } from 'react';
import { engineCall } from '../../engine';
import { LocaleContext } from '../../context';
import { getString } from '../../localisations';
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
import { MainPanelItem } from 'mods/general';
import styles from './mainPanel.module.scss';

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

export default function Content(props: {items: MainPanelItem[], addMemberData?: AddMemberData}) {
  const locale = useContext(LocaleContext);
  const buttonItems = props.items.filter(item => item.itemType === "button");
  const nonButtonItems = props.items.filter(item => item.itemType !== "button");
  const isAddingMember = props.addMemberData?.isAddingMember && props.addMemberData.members && props.addMemberData.members.length > 0;
  
  return (
    <div className={styles.contentContainer}>
      <Scrollable style={{flex: 1}} contentStyle={{flex: 1}} trackStyle={{marginLeft: "0.25em"}}>
        {nonButtonItems.map((item, idx) => {
          if (item.itemType == "title") {
            return <Row key={idx} data={item}><Title {...item} /></Row>;
          }
          if (item.itemType == "message") {
            return <Row key={idx} data={item}><Message {...item} /></Row>;
          }
          if (item.itemType == "divider") {
            return <Divider key={idx} />;
          }
          if (item.itemType == "radio") {
            return (
              <Row key={idx} data={item} hoverEffect={true} className={styles.hover}>
                <Radio {...item} />
                <div className={styles.contentLabel}>{getString(locale, item.label)}</div>
              </Row>
            );
          }
          if (item.itemType == "checkbox") {
            return (
              <Row key={idx} data={item} hoverEffect={true}>
                <Checkbox {...item} />
                <div className={styles.contentLabel}>{getString(locale, item.label)}</div>
              </Row>
            );
          }
          if (item.itemType == "notification") {
            return <Notification key={idx} data={item} />;
          }
          if (item.itemType == "range") {
            return <Range key={idx} data={item} />;
          }
          return <></>;
        })}
        {isAddingMember && props.addMemberData && (
          <div className={styles.memberListContainer}>
            <div className={styles.memberListTitle}>Members ({props.addMemberData.members.length})</div>
            {props.addMemberData.members
              .sort((a, b) => {
                // Leader first, then by index
                if (a.isLeader && !b.isLeader) return -1;
                if (!a.isLeader && b.isLeader) return 1;
                return a.index - b.index;
              })
              .map((member) => (
                <div key={`${member.index}-${member.version}`} className={styles.memberListItem}>
                  Intersection {member.index} {member.isLeader && <span className={styles.leaderBadge}>(Leader)</span>}
                </div>
              ))}
              <Divider />
          </div>
        )}
        {buttonItems.length > 0 && (
          <>
            {isAddingMember ? (
              <div className={styles.buttonRow}>
                {buttonItems.map((item, idx) => {
                  const { key: itemKey, ...rest } = item as any;
                  return (
                    <Button 
                      key={idx} 
                      {...rest} 
                      onClick={() => {
                        if ("engineEventName" in item && item.engineEventName) {
                          engineCall(item.engineEventName, JSON.stringify(item));
                        }
                      }}
                    />
                  );
                })}
              </div>
            ) : (
              buttonItems.map((item, idx) => {
                const { key: itemKey, engineEventName, ...rest } = item as any;
                return (
                  <Row key={idx} data={item}>
                    <Button {...rest} />
                  </Row>
                );
              })
            )}
          </>
        )}
      </Scrollable>
    </div>
  );
}