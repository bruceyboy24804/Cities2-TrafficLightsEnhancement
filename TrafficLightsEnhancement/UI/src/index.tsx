import { ModRegistrar } from "cs2/modding";

import App from './mods/app';

const register: ModRegistrar = (moduleRegistry) => {
  moduleRegistry.append("GameTopLeft", App);
};

export default register;