import { defineConfig } from "vite";
import { resolve } from "path";

export default defineConfig({
  root: "./src",
  publicDir: "../public",
  build: {
    outDir: "../_dist",
    rollupOptions: {
      input: {
        main: resolve(__dirname, "./src/index.html"),
        timer: resolve(__dirname, "./src/timer.html"),
      },
    },
  },
  server: {
    open: true,
    port: 8001,
    strictPort: true,
    proxy: {
      "/ws": {
        target: "ws://localhost:8002/",
        ws: true,
      },
    },
  },
  preview: { port: 8000 },
});
