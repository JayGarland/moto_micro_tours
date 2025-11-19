import { bootstrapApplication } from '@angular/platform-browser';
import { appConfig } from './app/app.config';
import { App } from './app/app';

function removeGlobalLoader() {
  try {
    const loader = document.getElementById('global-loader');
    if (loader && loader.parentNode) {
      loader.parentNode.removeChild(loader);
    }
  } catch (e) {
    // ignore DOM removal errors
  }
}

bootstrapApplication(App, appConfig)
  .then(() => removeGlobalLoader())
  .catch((err) => {
    console.error(err);
    // Ensure loader is removed even on bootstrap error so user can see the console/UI
    removeGlobalLoader();
  });
