export const desktopReferenceMap = {
  parser: '../Rotatonator/Services/LogMonitor.cs',
  rotation: '../Rotatonator/Services/RotationManager.cs',
  positions: '../Rotatonator/Services/PositionHelper.cs',
  exportUx: '../Rotatonator/MainWindow.xaml.cs',
} as const;

export const parityChecklist = [
  'Prefix CH detection with tolerant separators',
  'Append macro detection: rotat:<position>, <target>',
  'Unified handling path for append and prefix messages',
  'Out-of-sync cast timing adjustment using cast-start lookback',
  'NPC target warning behavior parity',
  'CH export format parity (always 3-char position strings)',
] as const;
