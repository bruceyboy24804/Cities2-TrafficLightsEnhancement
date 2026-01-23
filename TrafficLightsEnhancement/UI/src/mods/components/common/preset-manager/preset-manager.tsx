import {useState} from "react";
import {useValue} from "cs2/api";
import {Button, Dropdown, DropdownToggle, Tooltip} from "cs2/ui";

import {getModule} from "cs2/modding";
import {Theme} from "cs2/bindings";

import {
	callApplyUserPreset,
	callDeleteUserPreset,
	callSaveUserPreset,
	callUpdateUserPreset,
	userPresets
} from "bindings";

import styles from "./preset-manager.module.scss";
import {TextInput} from "../TextInput";

const DropdownStyle: Theme | any = getModule("game-ui/menu/themes/dropdown.module.scss", "classes");

export interface UserPreset {
    Id: string;
    Name: string;
    MinDuration: number;
    MaxDuration: number;
    WaitFlowBalance: number;
    ChangeMetric: number;
    TargetDurationMultiplier: number;
}

export interface BuiltInTemplate {
    id: number;
    name: string;
    description: string;
}

interface PresetManagerProps {
    builtInTemplates: BuiltInTemplate[];
    onApplyBuiltIn: (templateId: number) => void;
}

export const PresetManager = ({builtInTemplates, onApplyBuiltIn}: PresetManagerProps) => {
    const userPresetsValue = useValue(userPresets.binding);
    const [isCreating, setIsCreating] = useState(false);
    const [isEditing, setIsEditing] = useState(false);
    const [editingPresetId, setEditingPresetId] = useState("");
    const [presetName, setPresetName] = useState("");
    const [selectedPreset, setSelectedPreset] = useState<{id: string | number, name: string, isBuiltIn: boolean} | null>(null);
    const {DropdownItem} = require("cs2/ui");
    const presets: UserPreset[] = (() => {
        try {
            const parsed = JSON.parse(userPresetsValue);
            return Array.isArray(parsed) ? parsed : [];
        } catch {
            return [];
        }
    })();

    const handleSaveNew = () => {
        if (presetName.trim().length > 0) {
            callSaveUserPreset(JSON.stringify({name: presetName.trim()}));
            setPresetName("");
            setIsCreating(false);
        }
    };

    const handleUpdateName = () => {
        if (presetName.trim().length > 0 && editingPresetId) {
            callUpdateUserPreset(JSON.stringify({presetId: editingPresetId, name: presetName.trim()}));
            setPresetName("");
            setEditingPresetId("");
            setIsEditing(false);
        }
    };

    const handleCancel = () => {
        setPresetName("");
        setEditingPresetId("");
        setIsCreating(false);
        setIsEditing(false);
    };

    const handleStartEdit = (preset: UserPreset) => {
        setEditingPresetId(preset.Id);
        setPresetName(preset.Name);
        setIsEditing(true);
        setIsCreating(false);
    };

    const handleDelete = (presetId: string) => {
        callDeleteUserPreset(JSON.stringify({presetId}));
    };

    const handleApply = (presetId: string, presetName: string) => {
        callApplyUserPreset(JSON.stringify({presetId}));
        setSelectedPreset({ id: presetId, name: presetName, isBuiltIn: false });
    };

    const handleApplyBuiltIn = (templateId: number) => {
        const template = builtInTemplates.find(t => t.id === templateId);
        if (template) {
            onApplyBuiltIn(templateId);
            setSelectedPreset({ id: templateId, name: template.name, isBuiltIn: true });
        }
    };

    const dropdownContent = [
        ...builtInTemplates.map((template) => (
            <DropdownItem
                key={`builtin-${template.id}`}
                theme={DropdownStyle}
                value={template.id}
                closeOnSelect={true}
                selected={selectedPreset?.id === template.id && selectedPreset?.isBuiltIn}
                onChange={() => handleApplyBuiltIn(template.id)}
            >
                <div className={styles.dropdownItemContent}>
                    <span className={styles.templateName}>{template.name}</span>
                    <span className={styles.templateDesc}>{template.description}</span>
                </div>
            </DropdownItem>
        )),
        ...(presets.length > 0 ? [
            <div key="separator" className={styles.separator}>User Presets</div>
        ] : []),
        ...presets.map((preset) => (
            <div key={`user-${preset.Id}`} className={styles.presetRow}>
                <DropdownItem
                    theme={DropdownStyle}
                    value={preset.Id}
                    closeOnSelect={true}
                    selected={selectedPreset?.id === preset.Id && !selectedPreset?.isBuiltIn}
                    onChange={() => handleApply(preset.Id, preset.Name)}
                    className={styles.presetItem}
                >
                    {preset.Name}
                </DropdownItem>
                <div className={styles.presetActions}>
                    <Tooltip tooltip="Edit">
                        <Button
                            variant="text"
                            className={styles.buttonStyle}
                            onClick={(e) => {
                                e.stopPropagation();
                                handleStartEdit(preset);
                            }}
                        >
                            Edit
                        </Button>
                    </Tooltip>
                    <Tooltip tooltip="Delete">
                        <Button
                            variant="text"
                            className={styles.buttonStyle}
                            onClick={(e) => {
                                e.stopPropagation();
                                handleDelete(preset.Id);
                            }}
                        >
                            Delete
                        </Button>
                    </Tooltip>
                </div>
            </div>
        ))
    ];

    if (isCreating || isEditing) {
        return (
            <div className={styles.container}>
                <div className={styles.header}>
                    {isEditing ? "Edit Preset" : "New Preset"}
                </div>
                <div className={styles.inputRow}>
                    <TextInput
                        placeholder="Enter preset name..."
                        value={presetName}
                        onChange={(val) => setPresetName(val)}
                    />
                </div>
                <div className={styles.buttonRow}>
                    <Button variant="flat" onSelect={handleCancel} className={styles.buttonStyle} >
                        Cancel
                    </Button>
                    <Button
                        variant="primary"
                        onSelect={isEditing ? handleUpdateName : handleSaveNew}
                        disabled={presetName.trim().length === 0}
                        className={styles.buttonStyle}
                    >
                        {isEditing ? "Update" : "Save"}
                    </Button>
                </div>
            </div>
        );
    }

    return (
        <div className={styles.container}>
            <Dropdown theme={DropdownStyle} content={dropdownContent}>
                <DropdownToggle>
                    {selectedPreset ? `${selectedPreset.name}` : 'Apply Template...'}
                </DropdownToggle>
            </Dropdown>
            <div className={styles.buttonRow}>
                <Button variant="flat" onSelect={() => setIsCreating(true)} className={styles.buttonStyle}>
                    Save Current as Preset
                </Button>
            </div>
        </div>
    );
};

export default PresetManager;
