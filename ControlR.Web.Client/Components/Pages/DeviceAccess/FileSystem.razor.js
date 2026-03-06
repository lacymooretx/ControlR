export function triggerFileInput(fileInputElement) {
  if (fileInputElement) {
    fileInputElement.click();
  }
}

export function downloadFile(url, filename) {
  const link = document.createElement('a');
  link.href = url;
  link.download = filename || '';
  link.style.display = 'none';
  document.body.appendChild(link);
  link.click();
  document.body.removeChild(link);
}

let _dropZoneElement = null;
let _dotnetRef = null;
let _dragCounter = 0;

export function initDropZone(dropZoneElement, dotnetRef) {
  _dropZoneElement = dropZoneElement;
  _dotnetRef = dotnetRef;
  _dragCounter = 0;

  dropZoneElement.addEventListener('dragenter', onDragEnter);
  dropZoneElement.addEventListener('dragleave', onDragLeave);
  dropZoneElement.addEventListener('dragover', onDragOver);
  dropZoneElement.addEventListener('drop', onDrop);
}

export function disposeDropZone() {
  if (_dropZoneElement) {
    _dropZoneElement.removeEventListener('dragenter', onDragEnter);
    _dropZoneElement.removeEventListener('dragleave', onDragLeave);
    _dropZoneElement.removeEventListener('dragover', onDragOver);
    _dropZoneElement.removeEventListener('drop', onDrop);
    _dropZoneElement = null;
    _dotnetRef = null;
    _dragCounter = 0;
  }
}

function onDragEnter(e) {
  e.preventDefault();
  e.stopPropagation();
  _dragCounter++;
  if (_dragCounter === 1 && hasFiles(e)) {
    _dotnetRef.invokeMethodAsync('OnDragEnterFromJs');
  }
}

function onDragLeave(e) {
  e.preventDefault();
  e.stopPropagation();
  _dragCounter--;
  if (_dragCounter <= 0) {
    _dragCounter = 0;
    _dotnetRef.invokeMethodAsync('OnDragLeaveFromJs');
  }
}

function onDragOver(e) {
  e.preventDefault();
  e.stopPropagation();
}

function onDrop(e) {
  e.preventDefault();
  e.stopPropagation();
  _dragCounter = 0;
  _dotnetRef.invokeMethodAsync('OnDragLeaveFromJs');

  if (!e.dataTransfer || !e.dataTransfer.files || e.dataTransfer.files.length === 0) {
    return;
  }

  // Feed files into the hidden InputFile element via a new DataTransfer
  const inputElement = _dropZoneElement.querySelector('input[type="file"]');
  if (inputElement) {
    inputElement.files = e.dataTransfer.files;
    // Trigger the change event so Blazor's InputFile picks it up
    inputElement.dispatchEvent(new Event('change', { bubbles: true }));
  }
}

function hasFiles(e) {
  if (e.dataTransfer && e.dataTransfer.types) {
    for (let i = 0; i < e.dataTransfer.types.length; i++) {
      if (e.dataTransfer.types[i] === 'Files') {
        return true;
      }
    }
  }
  return false;
}
