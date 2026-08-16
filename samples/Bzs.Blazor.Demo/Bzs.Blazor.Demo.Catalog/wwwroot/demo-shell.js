import { startStaticController } from './Components/CatalogShell.razor.js';

const start = () => startStaticController('demo-app-shell');

if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', start, { once: true });
} else {
    start();
}
