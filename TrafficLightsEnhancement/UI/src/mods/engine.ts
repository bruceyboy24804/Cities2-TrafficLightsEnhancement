import { call, trigger, bindValue, useValue } from 'cs2/api';
import { WorldPosition } from './general';
import mod from 'mod.json';

export {};

export function useEngineOn<T>(bindingName: string, initialState: T) {
  const binding = bindValue<T>(mod.id, `BINDING:${bindingName}`, initialState);
  return useValue(binding);
}

export async function useEngineCall<T>(triggerName: string, data?: string): Promise<T | undefined> {
  return await engineCall<T>(triggerName, data);
}

export async function translatePosition(worldPosition: WorldPosition) {
  const result = await call<string>(mod.id, 'TRIGGER:CallTranslatePosition', JSON.stringify(worldPosition));
  const screenPoint = {left: 0, top: 0};
  if (result) {
    const parsedObj = JSON.parse(result);
    if (parsedObj.left) {
      screenPoint.left = parsedObj.left;
    }
    if (parsedObj.top) {
      screenPoint.top = parsedObj.top;
    }
  }
  return screenPoint;
}

export async function engineCall<T>(eventName: string, data?: string): Promise<T | undefined> {
  const triggerMatch = eventName.match(/^(.+)\.TRIGGER:(.+)$/);
  if (triggerMatch) {
    const [, modId, triggerName] = triggerMatch;
    if (data) {
      trigger(modId, `TRIGGER:${triggerName}`, data);
    } else {
      trigger(modId, `TRIGGER:${triggerName}`);
    }
    return undefined;
  }
  // Fallback for simple trigger names
  if (data) {
    trigger(mod.id, `TRIGGER:${eventName}`, data);
  } else {
    trigger(mod.id, `TRIGGER:${eventName}`);
  }
  return undefined;
}

export function triggerEvent(triggerName: string, ...args: any[]) {
  trigger(mod.id, `TRIGGER:${triggerName}`, ...args);
}