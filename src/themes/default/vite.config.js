import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

export default defineConfig({
  plugins: [react()],
  define: {
    'process.env.NODE_ENV': JSON.stringify('production'),
  },
  build: {
    outDir: 'dist',
    lib: {
      entry: 'src/index.jsx',
      formats: ['iife'],
      name: 'VitrineTheme',
      fileName: () => 'theme.js',
    },
    cssCodeSplit: false,
  },
});
