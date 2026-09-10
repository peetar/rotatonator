export type LocalLogReadResult = {
  text: string;
  newOffset: number;
};

export function isFileSystemAccessSupported(): boolean {
  return typeof window !== 'undefined' && 'showOpenFilePicker' in window;
}

export async function pickLocalLogFile(): Promise<FileSystemFileHandle> {
  const picker = window.showOpenFilePicker;
  const handles = await picker({
    multiple: false,
    types: [
      {
        description: 'EverQuest Log Files',
        accept: {
          'text/plain': ['.txt', '.log'],
        },
      },
    ],
  });

  return handles[0];
}

export async function readNewLogText(
  fileHandle: FileSystemFileHandle,
  previousOffset: number,
): Promise<LocalLogReadResult> {
  const file = await fileHandle.getFile();
  
  // For large files, only read the last 256KB to avoid reading entire file
  // This significantly speeds up polling on large logs
  const chunkSize = 256 * 1024; // 256KB
  const fileSize = file.size;
  const readStart = Math.max(0, fileSize - chunkSize);
  
  // If previousOffset is within our read chunk, use it; otherwise start from chunk start
  const safeOffset = previousOffset >= readStart ? previousOffset : readStart;
  
  const blob = file.slice(safeOffset);
  const text = await blob.text();
  
  // Return the new offset as previousOffset + what we just read
  const newOffset = safeOffset + text.length;

  return {
    text: text,
    newOffset: newOffset,
  };
}

export function splitRecentLines(input: string, maxLines = 8): string[] {
  return input
    .split(/\r?\n/)
    .map((line) => line.trim())
    .filter((line) => line.length > 0)
    .slice(-maxLines);
}
