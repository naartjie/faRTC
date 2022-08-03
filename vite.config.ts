import { defineConfig } from "vite";
import { resolve } from "path";

export default defineConfig({
  root: "./src",
  server: {
    port: 8001,
    strictPort: true,
  },
  build: {
    rollupOptions: {
      input: {
        main: resolve(__dirname, "./src/index.html"),
        timer: resolve(__dirname, "./src/timer.html"),
      },
    },
  },
});
