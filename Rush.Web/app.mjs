import { open } from "./browser/Rush.Web.mjs";

const app = await open();
const picker = document.querySelector("#rom");
const title = document.querySelector("#title");

picker.addEventListener("change", async () => {
   const file = picker.files[0];
   if (!file) 
       return;
   
   const error = app.start(new Uint8Array(await file.arrayBuffer()));
   if (error)
       title.textContent = `Error: ${error}`;
   picker.blur();
});


addEventListener("pagehide", async () => {
    app.stop();
    await app.close();
})