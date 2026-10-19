import { mergeConfig } from 'vitest/config';
import base from './vite.config';
import path from 'node:path';
export default mergeConfig(base, { resolve: { dedupe: ['react', 'react-dom', 'react/jsx-runtime', '@cratis/arc.react', '@cratis/arc', '@cratis/components', '@cratis/fundamentals'], alias: { react: path.resolve(__dirname, 'node_modules/react'), 'react-dom': path.resolve(__dirname, 'node_modules/react-dom') } } });
