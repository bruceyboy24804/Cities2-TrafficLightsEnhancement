import { trigger } from "cs2/api";
import mod from "mod.json";

class TriggerBuilder {
	public constructor(
		private mod_id: string,
		private prefix = "TRIGGER:"
	) {
	}

	create<Args extends any[]>(name: string) {
		const full = `${this.prefix}${name}`;
		return (...args: Args) => {
			trigger(this.mod_id, full, ...(args as unknown as any[]));
		};
	}
}

const singleton = new TriggerBuilder(mod.id);

export default singleton;