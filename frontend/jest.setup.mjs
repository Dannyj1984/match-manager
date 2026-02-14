import { config } from '@vue/test-utils';

// Mock Nuxt globals if necessary
global.defineNuxtConfig = (config) => config;
global.defineNuxtPlugin = (plugin) => plugin;
global.defineNuxtMiddleware = (middleware) => middleware;

// Example of mocking a global component or utility
// config.global.components = {
//   'NuxtLink': { template: '<a><slot /></a>' }
// };
