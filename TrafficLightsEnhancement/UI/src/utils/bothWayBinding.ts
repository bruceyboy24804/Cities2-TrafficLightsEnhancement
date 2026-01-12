import { bindValue, trigger, type ValueBinding } from "cs2/api";
import mod from "mod.json";

export class TwoWayBinding<T> {
	public id: string;
	private _binding: ValueBinding<T>;

	public get binding_id(): string {
		return `BINDING:${this.id}`;
	}

	public get trigger_id(): string {
		return `TRIGGER:${this.id}`;
	}

	public get binding(): ValueBinding<T> {
		return this._binding;
	}

	public constructor(id: string, fallback_value?: T) {
		this.id = id;
		this._binding = bindValue<T>(mod.id, this.binding_id, fallback_value);
	}

	public set(value: T): void {
		trigger(mod.id, this.trigger_id, value);
	}
}