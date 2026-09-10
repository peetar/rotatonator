import { useState } from 'react';
import { type RotationConfig } from './configTypes';
import './ConfigPanel.css';

interface ConfigPanelProps {
  config: RotationConfig;
  onConfigChange: (config: RotationConfig) => void;
}

export function ConfigPanel({ config, onConfigChange }: ConfigPanelProps) {
  const [newHealerName, setNewHealerName] = useState('');
  const [draggedIndex, setDraggedIndex] = useState<number | null>(null);
  const [editingIndex, setEditingIndex] = useState<number | null>(null);
  const [editingValue, setEditingValue] = useState('');

  const addHealer = () => {
    const trimmed = newHealerName.trim();
    if (trimmed && !config.healers.includes(trimmed)) {
      onConfigChange({
        ...config,
        healers: [...config.healers, trimmed],
      });
      setNewHealerName('');
    }
  };

  const removeHealer = (index: number) => {
    onConfigChange({
      ...config,
      healers: config.healers.filter((_, i) => i !== index),
    });
  };

  const updateField = <K extends keyof RotationConfig>(key: K, value: RotationConfig[K]) => {
    onConfigChange({
      ...config,
      [key]: value,
    });
  };

  const copyToClipboard = async (text: string): Promise<boolean> => {
    try {
      await navigator.clipboard.writeText(text);
      return true;
    } catch {
      return false;
    }
  };

  const adjustChainInterval = async (delta: number) => {
    const newValue = Math.max(1, Math.min(30, config.chainIntervalSeconds + delta));
    updateField('chainIntervalSeconds', newValue);
    
    // Copy the set_delay command to clipboard
    const command = `/rs Rotatonator set_delay: ${newValue}`;
    await copyToClipboard(command);
  };

  const moveHealer = (fromIndex: number, toIndex: number) => {
    const newHealers = [...config.healers];
    const [movedHealer] = newHealers.splice(fromIndex, 1);
    newHealers.splice(toIndex, 0, movedHealer);
    
    onConfigChange({
      ...config,
      healers: newHealers,
    });
  };

  const handleDragStart = (index: number) => {
    setDraggedIndex(index);
  };

  const handleDragOver = (e: React.DragEvent, index: number) => {
    e.preventDefault();
    
    if (draggedIndex === null || draggedIndex === index) {
      return;
    }
    
    moveHealer(draggedIndex, index);
    setDraggedIndex(index);
  };

  const handleDragEnd = () => {
    setDraggedIndex(null);
  };

  const startEditingHealer = (index: number) => {
    setEditingIndex(index);
    setEditingValue(config.healers[index]);
  };

  const saveHealerEdit = (index: number) => {
    const trimmed = editingValue.trim();
    if (trimmed && !config.healers.some((h, i) => i !== index && h === trimmed)) {
      const newHealers = [...config.healers];
      newHealers[index] = trimmed;
      onConfigChange({
        ...config,
        healers: newHealers,
      });
    }
    setEditingIndex(null);
    setEditingValue('');
  };

  const cancelHealerEdit = () => {
    setEditingIndex(null);
    setEditingValue('');
  };

  const handleHealerEditKeyDown = (e: React.KeyboardEvent, index: number) => {
    if (e.key === 'Enter') {
      e.preventDefault();
      saveHealerEdit(index);
    } else if (e.key === 'Escape') {
      e.preventDefault();
      cancelHealerEdit();
    }
  };

  return (
    <div className="config-panel">
      {/* Chain Prefix */}
      <div className="config-section">
        <h3>Chain Settings</h3>

        <div className="config-field">
          <label>Chain Prefix:</label>
          <input
            type="text"
            value={config.chainPrefix}
            onChange={(e) => updateField('chainPrefix', e.target.value)}
            placeholder="D&D"
          />
        </div>
        <div className="config-help-text">
          Example Chain text: <span className="help-example">{config.chainPrefix || '(prefix)'} 111 CH - %t - %n</span>
        </div>


        <div className="config-field">
          <label>Chain Interval (seconds):</label>
          <div className="interval-control">
            <button 
              type="button" 
              className="interval-btn"
              onClick={() => adjustChainInterval(-1)}
              title="Decrease interval and copy command to clipboard"
            >
              −
            </button>
            <span className="interval-value">{config.chainIntervalSeconds}</span>
            <button 
              type="button" 
              className="interval-btn"
              onClick={() => adjustChainInterval(1)}
              title="Increase interval and copy command to clipboard"
            >
              +
            </button>
          </div>
        </div>
      </div>

      {/* Healers */}
      <div className="config-section">
        <h3>Healers</h3>
        <div className="healer-list">
          {config.healers.map((healer, idx) => (
            <div 
              key={idx} 
              className={`healer-item ${draggedIndex === idx ? 'dragging' : ''}`}
              draggable
              onDragStart={() => handleDragStart(idx)}
              onDragOver={(e) => handleDragOver(e, idx)}
              onDragEnd={handleDragEnd}
            >
              <span className="drag-handle" title="Drag to reorder">⋮⋮</span>
              <span className="healer-position">{idx + 1}</span>
              {editingIndex === idx ? (
                <input
                  type="text"
                  className="healer-edit-input"
                  value={editingValue}
                  onChange={(e) => setEditingValue(e.target.value)}
                  onKeyDown={(e) => handleHealerEditKeyDown(e, idx)}
                  onBlur={() => saveHealerEdit(idx)}
                  autoFocus
                />
              ) : (
                <span 
                  className="healer-name"
                  onDoubleClick={() => startEditingHealer(idx)}
                  title="Double-click to edit"
                >
                  {healer}
                </span>
              )}
              <button
                type="button"
                className="remove-btn"
                onClick={() => removeHealer(idx)}
                title="Remove healer"
              >
                ✕
              </button>
            </div>
          ))}
        </div>

        <div className="add-healer-row">
          <input
            type="text"
            value={newHealerName}
            onChange={(e) => setNewHealerName(e.target.value)}
            onKeyPress={(e) => {
              if (e.key === 'Enter') {
                addHealer();
              }
            }}
            placeholder="Enter healer name"
          />
          <button type="button" onClick={addHealer}>
            Add Healer
          </button>
        </div>
      </div>

      {/* Player Settings */}
      <div className="config-section">
        <h3>Player Settings</h3>

        <div className="config-field">
          <label>Your Character Name:</label>
          <input
            type="text"
            value={config.playerName}
            onChange={(e) => updateField('playerName', e.target.value)}
            placeholder="Your character name"
          />
        </div>
      </div>
    </div>
  );
}
