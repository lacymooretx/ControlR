// noinspection JSUnusedGlobalSymbols

class AnnotationState {
  /** @type {CanvasRenderingContext2D} */
  context;
  /** @type {HTMLCanvasElement} */
  canvasElement;
  /** @type {string} */
  canvasId;
  /** @type {any} */
  componentRef;
  /** @type {boolean} */
  isDrawing;
  /** @type {number[]} */
  currentPointsX;
  /** @type {number[]} */
  currentPointsY;
  /** @type {string} */
  color;
  /** @type {number} */
  thickness;

  constructor() {
    this.isDrawing = false;
    this.currentPointsX = [];
    this.currentPointsY = [];
    this.color = "#ff0000";
    this.thickness = 3;
  }

  /**
   * @param {string} methodName
   * @param {...any} args
   * @returns {Promise<any>}
   */
  invokeDotNet(methodName, ...args) {
    return this.componentRef.invokeMethodAsync(methodName, ...args);
  }
}

/**
 * @param {string} canvasId
 * @returns {AnnotationState}
 */
function getAnnotationState(canvasId) {
  const key = `controlr-annotation-${canvasId}`;
  if (!window[key]) {
    window[key] = new AnnotationState();
  }
  return window[key];
}

/**
 * Initialize the annotation canvas with drawing event handlers.
 * @param {any} componentRef
 * @param {string} canvasId
 */
export function initialize(componentRef, canvasId) {
  const state = getAnnotationState(canvasId);

  /** @type {HTMLCanvasElement} */
  const canvas = document.getElementById(canvasId);
  if (!canvas) {
    console.error("Annotation canvas not found:", canvasId);
    return;
  }

  state.componentRef = componentRef;
  state.canvasId = canvasId;
  state.canvasElement = canvas;
  state.context = canvas.getContext("2d");

  canvas.addEventListener("pointerdown", (ev) => {
    if (!canvas.classList.contains("active")) {
      return;
    }

    ev.preventDefault();
    ev.stopPropagation();

    state.isDrawing = true;
    state.currentPointsX = [];
    state.currentPointsY = [];

    const rect = canvas.getBoundingClientRect();
    const scaleX = canvas.width / rect.width;
    const scaleY = canvas.height / rect.height;
    const x = (ev.clientX - rect.left) * scaleX;
    const y = (ev.clientY - rect.top) * scaleY;

    state.currentPointsX.push(x / canvas.width);
    state.currentPointsY.push(y / canvas.height);

    state.context.beginPath();
    state.context.strokeStyle = state.color;
    state.context.lineWidth = state.thickness;
    state.context.lineCap = "round";
    state.context.lineJoin = "round";
    state.context.moveTo(x, y);
  }, { passive: false });

  canvas.addEventListener("pointermove", (ev) => {
    if (!state.isDrawing) {
      return;
    }

    ev.preventDefault();
    ev.stopPropagation();

    const rect = canvas.getBoundingClientRect();
    const scaleX = canvas.width / rect.width;
    const scaleY = canvas.height / rect.height;
    const x = (ev.clientX - rect.left) * scaleX;
    const y = (ev.clientY - rect.top) * scaleY;

    state.currentPointsX.push(x / canvas.width);
    state.currentPointsY.push(y / canvas.height);

    state.context.lineTo(x, y);
    state.context.stroke();
  }, { passive: false });

  const finishStroke = async (ev) => {
    if (!state.isDrawing) {
      return;
    }

    ev.preventDefault();
    ev.stopPropagation();

    state.isDrawing = false;

    if (state.currentPointsX.length > 1) {
      try {
        await state.invokeDotNet(
          "OnStrokeCompleted",
          state.currentPointsX,
          state.currentPointsY,
          state.color,
          state.thickness);
      } catch (e) {
        console.error("Error sending annotation stroke:", e);
      }
    }

    state.currentPointsX = [];
    state.currentPointsY = [];
  };

  canvas.addEventListener("pointerup", finishStroke, { passive: false });
  canvas.addEventListener("pointercancel", finishStroke, { passive: false });
  canvas.addEventListener("pointerleave", finishStroke, { passive: false });

  // Prevent context menu on annotation canvas
  canvas.addEventListener("contextmenu", (ev) => {
    if (canvas.classList.contains("active")) {
      ev.preventDefault();
      ev.stopPropagation();
    }
  });
}

/**
 * Set the drawing color.
 * @param {string} canvasId
 * @param {string} color
 */
export function setColor(canvasId, color) {
  const state = getAnnotationState(canvasId);
  state.color = color;
}

/**
 * Set the drawing thickness.
 * @param {string} canvasId
 * @param {number} thickness
 */
export function setThickness(canvasId, thickness) {
  const state = getAnnotationState(canvasId);
  state.thickness = thickness;
}

/**
 * Clear all annotations from the canvas.
 * @param {string} canvasId
 */
export function clearCanvas(canvasId) {
  const state = getAnnotationState(canvasId);
  if (state.context && state.canvasElement) {
    state.context.clearRect(0, 0, state.canvasElement.width, state.canvasElement.height);
  }
}
