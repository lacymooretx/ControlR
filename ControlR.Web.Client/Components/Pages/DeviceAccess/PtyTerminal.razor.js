// noinspection JSUnusedGlobalSymbols

const terminals = new Map();

export function initTerminal(containerId, dotnetRef, cols, rows) {
  const container = document.getElementById(containerId);
  if (!container) {
    console.error('PtyTerminal: container not found:', containerId);
    return null;
  }

  const Terminal = globalThis.Terminal;
  const FitAddon = globalThis.FitAddon?.FitAddon;
  const WebLinksAddon = globalThis.WebLinksAddon?.WebLinksAddon;

  if (!Terminal) {
    console.error('PtyTerminal: xterm.js Terminal not found on globalThis');
    return null;
  }

  const term = new Terminal({
    cols: cols || 80,
    rows: rows || 24,
    cursorBlink: true,
    fontFamily: "'Cascadia Code', 'Fira Code', 'Consolas', 'Courier New', monospace",
    fontSize: 14,
    theme: {
      background: '#1e1e1e',
      foreground: '#d4d4d4',
      cursor: '#d4d4d4',
      selectionBackground: '#264f78',
    },
    allowProposedApi: true,
  });

  let fitAddon = null;
  if (FitAddon) {
    fitAddon = new FitAddon();
    term.loadAddon(fitAddon);
  }

  if (WebLinksAddon) {
    term.loadAddon(new WebLinksAddon());
  }

  term.open(container);

  if (fitAddon) {
    // Delay first fit to ensure container is sized
    requestAnimationFrame(() => {
      try {
        fitAddon.fit();
      } catch (e) {
        console.warn('PtyTerminal: initial fit failed:', e);
      }
    });
  }

  // Helper: convert bytes to base64 for Blazor WASM interop
  function toBase64(uint8Array) {
    let binary = '';
    for (let i = 0; i < uint8Array.length; i++) {
      binary += String.fromCharCode(uint8Array[i]);
    }
    return btoa(binary);
  }

  // User keyboard input → send to .NET as base64
  term.onData(data => {
    const bytes = new TextEncoder().encode(data);
    dotnetRef.invokeMethodAsync('OnTerminalInput', toBase64(bytes));
  });

  // Binary data (e.g., from paste) → send to .NET as base64
  term.onBinary(data => {
    const bytes = Uint8Array.from(data, c => c.charCodeAt(0));
    dotnetRef.invokeMethodAsync('OnTerminalInput', toBase64(bytes));
  });

  // Terminal resize → notify .NET
  term.onResize(({ cols, rows }) => {
    dotnetRef.invokeMethodAsync('OnTerminalResize', cols, rows);
  });

  // Container resize → refit terminal
  let resizeObserver = null;
  if (fitAddon) {
    let resizeTimeout = null;
    resizeObserver = new ResizeObserver(() => {
      // Debounce resize events
      if (resizeTimeout) {
        clearTimeout(resizeTimeout);
      }
      resizeTimeout = setTimeout(() => {
        try {
          fitAddon.fit();
        } catch (e) {
          // Container might not be visible
        }
      }, 100);
    });
    resizeObserver.observe(container);
  }

  terminals.set(containerId, { term, fitAddon, resizeObserver, dotnetRef });

  return { cols: term.cols, rows: term.rows };
}

export function writeOutput(containerId, bytes) {
  const state = terminals.get(containerId);
  if (state) {
    state.term.write(new Uint8Array(bytes));
  }
}

export function focus(containerId) {
  const state = terminals.get(containerId);
  if (state) {
    state.term.focus();
  }
}

export function dispose(containerId) {
  const state = terminals.get(containerId);
  if (state) {
    if (state.resizeObserver) {
      state.resizeObserver.disconnect();
    }
    state.term.dispose();
    terminals.delete(containerId);
  }
}
