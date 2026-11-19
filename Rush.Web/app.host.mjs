let canvas;
let context;
let image;

export function setText(selector, text) {
  document.querySelector(selector).textContent = text;
}

export function putPixels(rgba, width, height) {
  canvas ??= document.querySelector("#screen");
  context ??= canvas.getContext("2d");
  if (canvas.width !== width || canvas.height !== height) {
    canvas.width = width;
    canvas.height = height;
    image = undefined;
    fit();
  }
  
  image ??= context.createImageData(width, height);
  image.data.set(rgba);
  context.putImageData(image, 0, 0);
}

export function onKey(key) {
  const listener = event => {
    if (event.key.startsWith("Arrow")) 
      event.preventDefault();
    key(event.key);
  };
    
  addEventListener("keydown", listener);
  return { dispose() { removeEventListener("keydown", listener); } };
}

export function onFrame(frame){
  let active = true;
  let id = requestAnimationFrame(function tick(time) {
    frame(time);
    if (active) 
      id = requestAnimationFrame(tick);
  });
  
  return { dispose() { active = false; cancelAnimationFrame(id); }};
}

function fit() {
  if (!canvas?.width) return;
  const stage = canvas.parentElement;
  const ratio = devicePixelRatio;
  const scale = Math.max(1, Math.floor(Math.min(
      stage.clientWidth * ratio / canvas.width,
      stage.clientHeight * ratio / canvas.height )));
  canvas.style.width = `${canvas.width * scale / ratio}px`;
  canvas.style.height = `${canvas.height * scale / ratio}px`;
}

addEventListener("resize", fit);