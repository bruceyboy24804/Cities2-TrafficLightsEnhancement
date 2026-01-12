import { bindValue, type ValueBinding } from "cs2/api";
import mod from "mod.json";

export class OneWayBinding<T> {
	public id: string;
	private _binding: ValueBinding<T>;

	public get binding_id(): string {
		return `BINDING:${this.id}`;
	}

	public get binding(): ValueBinding<T> {
		return this._binding;
	}

	public constructor(id: string, fallback_value?: T, prefix: string = "BINDING:") {
		this.id = id;
		this._binding = bindValue<T>(mod.id, this.binding_id, fallback_value);
	}
}