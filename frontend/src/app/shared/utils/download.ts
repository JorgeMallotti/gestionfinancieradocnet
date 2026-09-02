/**
 * Saves a Blob as a file download in the browser (PDF/Excel exports).
 * The server generates the bytes; the client never assembles documents.
 */
export function saveBlob(blob: Blob, filename: string): void {
  const url = URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.href = url;
  link.download = filename;
  link.click();
  URL.revokeObjectURL(url);
}
