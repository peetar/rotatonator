import { useEffect, useMemo, useRef, useState } from 'react'
import './App.css'
import {
  isFileSystemAccessSupported,
  pickLocalLogFile,
  readNewLogText,
} from './features/log-access/fileSystemAccess'
import { LogParser, type LogEntry } from './features/log-parsing/logParser'
import { PositionHelper } from './features/log-parsing/positionHelper'
import { ConfigPanel } from './features/config/ConfigPanel'
import { AudioConfigPanel } from './features/config/AudioConfigPanel'
import { type RotationConfig } from './features/config/configTypes'
import { loadConfig, saveConfig } from './features/config/configStorage'
import { RotationManager } from './features/rotation/rotationManager'
import { RotationFeedback } from './features/rotation/RotationFeedback'

function App() {
  const [config, setConfig] = useState<RotationConfig>(() => loadConfig())
  const [fileHandle, setFileHandle] = useState<FileSystemFileHandle | null>(null)
  const [status, setStatus] = useState('No log file selected')

  // Create a single RotationManager instance that persists across renders
  const rotationManagerRef = useRef<RotationManager>(new RotationManager(config))
  const fileOffsetRef = useRef(0)

  // Save config to localStorage and update rotation manager config
  useEffect(() => {
    saveConfig(config)
    // Update the existing rotation manager's config
    rotationManagerRef.current.updateConfig(config)
  }, [config])

  const rotationManager = rotationManagerRef.current

  const parser = useMemo(() => new LogParser(config.chainPrefix), [config.chainPrefix])

  const fsSupported = useMemo(() => isFileSystemAccessSupported(), [])

  const onPickLogFile = async () => {
    try {
      const handle = await pickLocalLogFile()
      const file = await handle.getFile()
      setFileHandle(handle)
      fileOffsetRef.current = file.size
      setStatus(`Selected: ${handle.name} - Watching new log lines only`)
    } catch {
      setStatus('Log file selection canceled or failed')
    }
  }

  // Core function to read and process log updates
  const readLogUpdates = async (handle: FileSystemFileHandle) => {
    try {
      const result = await readNewLogText(handle, fileOffsetRef.current)
      fileOffsetRef.current = result.newOffset

      // Parse the new lines for CH messages
      const entries = parser.parseLogText(result.text)

      // Process config commands
      entries.forEach((entry: LogEntry) => {
        if (entry.configCommand) {
          const cmd = entry.configCommand;
          // Use functional update to ensure we have the latest state and preserve all other settings
          setConfig(prevConfig => {
            const newConfig = {
              ...prevConfig,
              chainIntervalSeconds: cmd.chainIntervalSeconds,
            };
            
            // Only update healers if specified in command
            if (cmd.healers && cmd.healers.length > 0) {
              newConfig.healers = cmd.healers;
            }
            
            return newConfig;
          });
          
          const statusParts = [];
          if (cmd.healers) {
            statusParts.push(`${cmd.healers.length} healers`);
          }
          statusParts.push(`delay: ${cmd.chainIntervalSeconds}s`);
          setStatus(`Config updated from chat: ${statusParts.join(', ')}`);
        }
      });

      // Wire parsed CH messages to rotation manager
      entries.forEach((entry: LogEntry) => {
        if (entry.detectedAsChMessage && entry.chData) {
          // Use healer name from log if available (from "says" format)
          // Only fallback to position lookup if no name was extracted
          let healerName = entry.chData.healer;
          const originalHealerName = healerName; // For debugging
          
          if (!healerName && entry.chData.position > 0 && entry.chData.position <= config.healers.length) {
            // Position is 1-based, array is 0-based
            healerName = config.healers[entry.chData.position - 1];
            console.log(`[Rotatonator] Healer name not in log, using position ${entry.chData.position} from config: ${healerName}`);
          }
          
          if (!healerName) {
            healerName = `Position ${entry.chData.position}`;
          }
          
          if (originalHealerName) {
            console.log(`[Rotatonator] Using healer name from log: ${originalHealerName}`);
          }
          
          rotationManager.onHealCast(healerName, entry.chData.targetName, entry.timestamp);
        }
      })

      const chCount = entries.filter((e: LogEntry) => e.detectedAsChMessage).length
      if (chCount > 0) {
        setStatus(`Detected ${chCount} CH message(s)`)
      }
    } catch (error) {
      console.error('Error reading log updates:', error)
      // Don't update status on every failed poll to avoid spam
    }
  }

  const copyToClipboard = async (text: string): Promise<boolean> => {
    try {
      await navigator.clipboard.writeText(text)
      return true
    } catch {
      return false
    }
  }

  const playBeep = () => {
    try {
      const audioContext = new (window.AudioContext || (window as any).webkitAudioContext)()
      const oscillator = audioContext.createOscillator()
      const gainNode = audioContext.createGain()

      oscillator.frequency.value = 800
      oscillator.type = 'sine'
      gainNode.gain.value = 0.2

      oscillator.connect(gainNode)
      gainNode.connect(audioContext.destination)

      const now = audioContext.currentTime
      oscillator.start(now)
      oscillator.stop(now + 0.1) // 100ms beep
    } catch {
      // Ignore audio errors (permissions/autoplay restrictions)
    }
  }

  const onExportChainToClipboard = async () => {
    if (config.healers.length === 0) {
      setStatus('No healers to export')
      return
    }

    const chainParts = config.healers
      .map((healer, index) => `${PositionHelper.positionToString(index + 1)} ${healer}`)
      .join(', ')

    const exportText = `/rs Rotatonator set_chain: ${chainParts}, set_delay: ${config.chainIntervalSeconds}`
    const ok = await copyToClipboard(exportText)
    if (ok) {
      playBeep()
      setStatus(`Chain configuration copied: ${exportText}`)
    } else {
      setStatus('Clipboard write failed (browser permission)')
    }
  }

  const onExportCHStringToClipboard = async () => {
    const playerName = config.playerName.trim()
    if (!playerName) {
      setStatus('Enter your character name first')
      return
    }

    if (config.healers.length === 0) {
      setStatus('No healers in chain')
      return
    }

    const playerIndex = config.healers.findIndex(
      healer => healer.toLowerCase() === playerName.toLowerCase()
    )

    if (playerIndex < 0) {
      setStatus(`Your character '${playerName}' is not in the healer list`)
      return
    }

    const prefix = config.chainPrefix.trim()
    if (!prefix) {
      setStatus('Enter a chain prefix first')
      return
    }

    const positionString = PositionHelper.positionToString(playerIndex + 1)
    const chString = `/rs ${prefix} ${positionString} CH - %t - %n`
    const ok = await copyToClipboard(chString)
    if (ok) {
      playBeep()
      setStatus(`CH macro copied: ${chString}`)
    } else {
      setStatus('Clipboard write failed (browser permission)')
    }
  }

  const onExportAppendMacroToClipboard = async () => {
    const playerName = config.playerName.trim()
    if (!playerName) {
      setStatus('Enter your character name first')
      return
    }

    if (config.healers.length === 0) {
      setStatus('No healers in chain')
      return
    }

    const playerIndex = config.healers.findIndex(
      healer => healer.toLowerCase() === playerName.toLowerCase()
    )

    if (playerIndex < 0) {
      setStatus(`Your character '${playerName}' is not in the healer list`)
      return
    }

    const appendMacro = `rotat:${playerIndex + 1}, %t`
    const ok = await copyToClipboard(appendMacro)
    if (ok) {
      playBeep()
      setStatus(`Append macro copied: ${appendMacro}`)
    } else {
      setStatus('Clipboard write failed (browser permission)')
    }
  }

  const openOverlayWindow = () => {
    // Note: Modern browsers don't support "always on top" for security reasons
    // Users can manually keep the window on top using OS features (Windows: Ctrl+Space with PowerToys, etc.)
    window.open('overlay.html', 'rotatonator-overlay', 'width=420,height=460,resizable=yes')
  }

  // Auto-poll the log file when a file is selected
  useEffect(() => {
    if (!fileHandle) {
      return
    }

    const interval = setInterval(() => {
      readLogUpdates(fileHandle)
    }, 200) // Poll every 200ms for faster response

    return () => clearInterval(interval)
  }, [fileHandle, config])

  return (
    <main className="app">
      <header>
        <h1>Rotatonator Web</h1>
      </header>

      <section className="panel">
        <h2>Rotatonator Options</h2>
        <p className="subtitle">
          Uses browser File System Access API. Best support is Chromium-based browsers.
        </p>
        <div className="buttonRow">
          <button type="button" className="btn-file-select" onClick={onPickLogFile} disabled={!fsSupported}>
            📁 Select EQ Log File
          </button>
          <button type="button" className="btn-overlay" onClick={openOverlayWindow}>
            👁️ Open Overlay Window
          </button>
        </div>
        <div className="buttonRow export-buttons">
          <button type="button" className="btn-export" onClick={onExportChainToClipboard}>
            📋 Export chain
          </button>
          <button type="button" className="btn-export" onClick={onExportCHStringToClipboard}>
            📋 Export CH string
          </button>
          <button type="button" className="btn-export" onClick={onExportAppendMacroToClipboard}>
            📋 Export macro
          </button>
        </div>
        <p className="status">{status}</p>
        {!fsSupported && (
          <p className="warning">
            File System Access API is not supported in this browser. Use Chromium or fallback companion service.
          </p>
        )}
      </section>

      <details className="panel" open>
        <summary className="summary-header">
          <h2>Configuration</h2>
        </summary>
        <ConfigPanel config={config} onConfigChange={setConfig} />
      </details>

      <details className="panel" open>
        <summary className="summary-header">
          <h2>Audio Options</h2>
        </summary>
        <AudioConfigPanel config={config} onConfigChange={setConfig} />
      </details>

      <RotationFeedback rotationManager={rotationManager} config={config} />
    </main>
  )
}

export default App
